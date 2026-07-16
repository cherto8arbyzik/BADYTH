import bpy
import math
import os
import random
from mathutils import Vector


PROJECT_DIR = r"D:\badyth\ArtSource\Blender\Characters\BirchWarden"
PROJECT_FILE = os.path.join(PROJECT_DIR, "BirchWarden.blend")
RENDER_DIR = r"D:\badyth\ArtSource\Renders"
RENDER_FILE = os.path.join(RENDER_DIR, "BirchWarden_preview.png")


def _collection(name, parent=None):
    existing = bpy.data.collections.get(name)
    if existing is not None:
        return existing
    result = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(result)
    return result


def _move_to_collection(obj, name):
    target = bpy.data.collections.get(name)
    if target is None:
        target = _collection(name)
    for source in list(obj.users_collection):
        source.objects.unlink(obj)
    target.objects.link(obj)


def _parent(obj, root="CHR_BirchWarden"):
    owner = bpy.data.objects.get(root)
    if owner is not None:
        obj.parent = owner
    return obj


def _smooth(obj):
    if obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    return obj


def _principled_input(shader, *names):
    for name in names:
        socket = shader.inputs.get(name)
        if socket is not None:
            return socket
    return None


def _simple_material(name, color, roughness=0.75, metallic=0.0, emission=None, emission_strength=0.0):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    _principled_input(shader, "Base Color").default_value = (*color, 1.0)
    _principled_input(shader, "Roughness").default_value = roughness
    _principled_input(shader, "Metallic").default_value = metallic
    if emission is not None:
        emission_socket = _principled_input(shader, "Emission Color", "Emission")
        if emission_socket is not None:
            emission_socket.default_value = (*emission, 1.0)
        strength_socket = _principled_input(shader, "Emission Strength")
        if strength_socket is not None:
            strength_socket.default_value = emission_strength
    material.node_tree.links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def _procedural_material(name, dark, light, scale=4.0, roughness=0.8, bump_strength=0.25):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    texcoord = nodes.new("ShaderNodeTexCoord")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = scale
    noise.inputs["Detail"].default_value = 5.0
    noise.inputs["Roughness"].default_value = 0.72
    noise.inputs["Distortion"].default_value = 0.3
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.24
    ramp.color_ramp.elements[0].color = (*dark, 1.0)
    ramp.color_ramp.elements[1].position = 0.68
    ramp.color_ramp.elements[1].color = (*light, 1.0)
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.18
    _principled_input(shader, "Roughness").default_value = roughness
    links.new(texcoord.outputs["Generated"], noise.inputs["Vector"])
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], _principled_input(shader, "Base Color"))
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], _principled_input(shader, "Normal"))
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def _assign(obj, material):
    if obj.type == "MESH" or obj.type == "CURVE":
        obj.data.materials.append(material)
    return obj


def _segment(name, start, end, radius_a, radius_b, material, vertices=12, collection="BW_Body"):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    length = direction.length
    if length < 0.0001:
        return None
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius_a,
        radius2=radius_b,
        depth=length,
        location=(start + end) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    obj.rotation_mode = "XYZ"
    _assign(obj, material)
    _smooth(obj)
    _parent(obj)
    _move_to_collection(obj, collection)
    bevel = obj.modifiers.new("Soft bark edges", "BEVEL")
    bevel.width = min(radius_a, radius_b) * 0.12
    bevel.segments = 2
    return obj


def _joint(name, location, radius, material, scale=(1.0, 1.0, 1.0), collection="BW_Body"):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=radius, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    _assign(obj, material)
    _smooth(obj)
    _parent(obj)
    _move_to_collection(obj, collection)
    return obj


def _chain(name, points, radii, material, vertices=12, collection="BW_Body", joints=True):
    objects = []
    for index in range(len(points) - 1):
        obj = _segment(
            f"{name}_{index + 1:02d}",
            points[index],
            points[index + 1],
            radii[index],
            radii[index + 1],
            material,
            vertices,
            collection,
        )
        if obj is not None:
            objects.append(obj)
    if joints:
        for index in range(1, len(points) - 1):
            objects.append(_joint(f"{name}_Knot_{index:02d}", points[index], radii[index] * 1.06, material, collection=collection))
    return objects


def _curve(name, points, radius, material, collection="BW_Vines", cyclic=False, resolution=2):
    data = bpy.data.curves.new(name + "_Curve", "CURVE")
    data.dimensions = "3D"
    data.resolution_u = resolution
    data.bevel_depth = radius
    data.bevel_resolution = 2
    data.resolution_u = 2
    spline = data.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for control, point in zip(spline.bezier_points, points):
        control.co = point
        control.handle_left_type = "AUTO"
        control.handle_right_type = "AUTO"
    spline.use_cyclic_u = cyclic
    obj = bpy.data.objects.new(name, data)
    bpy.data.collections[collection].objects.link(obj)
    _assign(obj, material)
    _parent(obj)
    return obj


def _box(name, location, scale, rotation, material, collection="BW_Details"):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    _assign(obj, material)
    bevel = obj.modifiers.new("Worn edges", "BEVEL")
    bevel.width = min(scale) * 0.22
    bevel.segments = 2
    _parent(obj)
    _move_to_collection(obj, collection)
    return obj


def _leaf_cluster(name, location, scale, material):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    _assign(obj, material)
    _smooth(obj)
    _parent(obj)
    _move_to_collection(obj, "BW_Moss")
    return obj


def _look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def phase_setup():
    # Manual cleanup keeps user preferences and the Blender MCP add-on alive.
    if bpy.context.object is not None:
        bpy.ops.object.mode_set(mode="OBJECT") if bpy.context.object.mode != "OBJECT" else None
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for collection in list(bpy.data.collections):
        bpy.data.collections.remove(collection)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights, bpy.data.materials):
        for datablock in list(datablocks):
            datablocks.remove(datablock)
    os.makedirs(PROJECT_DIR, exist_ok=True)
    os.makedirs(RENDER_DIR, exist_ok=True)
    scene = bpy.context.scene
    scene.name = "BirchWarden_Showcase"
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 720
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.resolution_percentage = 100
    scene.render.filepath = RENDER_FILE
    scene.render.image_settings.color_depth = "8"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.world = bpy.data.worlds.new("WORLD_BirchWarden")
    scene.world.use_nodes = True
    background = next(node for node in scene.world.node_tree.nodes if node.bl_idname == "ShaderNodeBackground")
    background.inputs["Color"].default_value = (0.006, 0.009, 0.008, 1.0)
    background.inputs["Strength"].default_value = 0.18

    root_collection = _collection("BirchWarden_Character")
    for name in ("BW_Body", "BW_Roots", "BW_Crown", "BW_Vines", "BW_Moss", "BW_Details", "BW_Talismans"):
        _collection(name, root_collection)
    _collection("BW_Presentation")

    root = bpy.data.objects.new("CHR_BirchWarden", None)
    root_collection.objects.link(root)
    root["character_name"] = "Берёзовый Страж"
    root["unity_scale_meters"] = 1.0
    root["design_note"] = "Original root-and-birch forest guardian inspired by the supplied mood reference"

    _procedural_material("MAT_BW_DarkWood", (0.025, 0.014, 0.008), (0.19, 0.105, 0.045), 3.8, 0.9, 0.42)
    _procedural_material("MAT_BW_Birch", (0.055, 0.042, 0.028), (0.72, 0.68, 0.55), 5.5, 0.82, 0.28)
    _procedural_material("MAT_BW_Moss", (0.025, 0.06, 0.018), (0.19, 0.31, 0.075), 7.0, 1.0, 0.55)
    _simple_material("MAT_BW_BarkScar", (0.018, 0.012, 0.008), 0.96)
    _simple_material("MAT_BW_Vine", (0.055, 0.075, 0.025), 0.94)
    _simple_material("MAT_BW_Brass", (0.34, 0.19, 0.055), 0.42, 0.68)
    _simple_material("MAT_BW_RedCharm", (0.29, 0.025, 0.018), 0.92)
    _simple_material("MAT_BW_Bone", (0.57, 0.50, 0.34), 0.84)
    _simple_material("MAT_BW_Eye", (0.22, 0.055, 0.004), 0.28, 0.0, (1.0, 0.18, 0.015), 8.0)
    _simple_material("MAT_BW_Ground", (0.008, 0.010, 0.009), 1.0)
    print("Birch Warden: setup complete")


def phase_body():
    dark = bpy.data.materials["MAT_BW_DarkWood"]
    birch = bpy.data.materials["MAT_BW_Birch"]
    scar = bpy.data.materials["MAT_BW_BarkScar"]
    eye = bpy.data.materials["MAT_BW_Eye"]

    _chain("TorsoCore", [(0.0, 0.08, 0.45), (-0.05, 0.04, 1.35), (0.08, 0.0, 2.35), (-0.03, 0.0, 3.25), (0.0, -0.02, 3.85)], [0.62, 0.86, 1.02, 0.88, 0.58], dark, 18)
    _chain("Leg_L", [(-0.40, 0.08, 2.05), (-0.52, 0.02, 1.22), (-0.68, -0.02, 0.30)], [0.46, 0.38, 0.23], birch, 14)
    _chain("Leg_R", [(0.43, 0.10, 2.00), (0.58, 0.04, 1.08), (0.48, -0.03, 0.27)], [0.43, 0.34, 0.21], birch, 14)

    _chain("Arm_L", [(-0.67, 0.0, 3.55), (-1.08, -0.02, 3.00), (-1.33, -0.08, 2.15), (-1.46, -0.12, 1.05)], [0.45, 0.39, 0.29, 0.19], birch, 14)
    _chain("Arm_R", [(0.68, 0.01, 3.55), (1.08, -0.06, 2.98), (1.33, -0.12, 2.25), (1.55, -0.14, 1.55)], [0.39, 0.34, 0.25, 0.17], dark, 14)

    # Long root-like fingers give the silhouette the same solemn, ancient weight as the reference.
    for side, x, z in (("L", -1.46, 1.03), ("R", 1.55, 1.53)):
        direction = -1.0 if side == "L" else 1.0
        for index in range(5):
            offset = (index - 2) * 0.08
            start = (x + offset, -0.13, z)
            middle = (x + direction * (0.10 + index * 0.025), -0.18 - index * 0.025, z - 0.34 - abs(index - 2) * 0.04)
            end = (middle[0] + direction * 0.05, middle[1] - 0.06, middle[2] - 0.31)
            _chain(f"Finger_{side}_{index + 1}", [start, middle, end], [0.07, 0.045, 0.016], dark, 7, joints=False)

    _joint("HeadCore", (0.0, -0.05, 4.15), 0.60, dark, (0.82, 0.72, 1.12))
    _box("MaskBrow_L", (-0.21, -0.50, 4.42), (0.26, 0.075, 0.10), (math.radians(-8), math.radians(5), math.radians(-12)), birch)
    _box("MaskBrow_R", (0.21, -0.50, 4.42), (0.26, 0.075, 0.10), (math.radians(-8), math.radians(-5), math.radians(12)), birch)
    _box("MaskNose", (0.0, -0.59, 4.17), (0.10, 0.08, 0.28), (math.radians(8), 0.0, 0.0), birch)
    _box("MaskJaw", (0.0, -0.48, 3.91), (0.31, 0.08, 0.09), (0.0, 0.0, math.radians(2)), dark)

    for name, x in (("Eye_L", -0.18), ("Eye_R", 0.18)):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, radius=0.075, location=(x, -0.585, 4.29))
        obj = bpy.context.object
        obj.name = name
        obj.scale = (1.0, 0.5, 0.72)
        _assign(obj, eye)
        _smooth(obj)
        _parent(obj)
        _move_to_collection(obj, "BW_Details")
        light_data = bpy.data.lights.new(name + "_Glow", "POINT")
        light_data.color = (1.0, 0.12, 0.01)
        light_data.energy = 35.0
        light_data.shadow_soft_size = 0.22
        light = bpy.data.objects.new(name + "_Glow", light_data)
        bpy.data.collections["BW_Details"].objects.link(light)
        light.location = (x, -0.68, 4.29)
        _parent(light)

    # Broken bark plates add a readable humanoid ribcage without making the guardian anatomical.
    random.seed(1741)
    for index in range(22):
        side = -1 if index % 2 == 0 else 1
        z = 1.15 + (index % 11) * 0.23
        x = side * (0.52 + random.uniform(-0.10, 0.12))
        y = -0.66 + random.uniform(-0.05, 0.03)
        _box(
            f"TorsoBarkPlate_{index + 1:02d}",
            (x, y, z),
            (random.uniform(0.17, 0.31), 0.055, random.uniform(0.06, 0.12)),
            (math.radians(random.uniform(-6, 6)), math.radians(random.uniform(-8, 8)), math.radians(random.uniform(-18, 18))),
            birch if index % 5 == 0 else scar,
        )

    # Birch scars are separate geometry so they remain legible after Unity texture compression.
    limbs = [(-0.55, -0.28, 1.42), (0.55, -0.26, 1.25), (-1.22, -0.26, 2.45), (-1.39, -0.24, 1.55)]
    for base_index, base in enumerate(limbs):
        for stripe in range(4):
            _box(
                f"BirchScar_{base_index}_{stripe}",
                (base[0], base[1] - stripe * 0.004, base[2] + (stripe - 1.5) * 0.17),
                (0.18, 0.025, 0.025),
                (0.0, 0.0, math.radians((stripe - 1.5) * 8)),
                scar,
            )
    print("Birch Warden: body complete")


def _bell(name, anchor, drop=0.75, scale=1.0):
    brass = bpy.data.materials["MAT_BW_Brass"]
    dark = bpy.data.materials["MAT_BW_DarkWood"]
    end = Vector(anchor) + Vector((0.0, 0.0, -drop))
    _curve(name + "_Chain", [anchor, Vector(anchor) + Vector((0.03, 0.0, -drop * 0.48)), end], 0.018 * scale, brass, "BW_Talismans")
    bpy.ops.mesh.primitive_cone_add(vertices=18, radius1=0.16 * scale, radius2=0.075 * scale, depth=0.28 * scale, location=end + Vector((0.0, 0.0, -0.14 * scale)))
    body = bpy.context.object
    body.name = name + "_Bell"
    _assign(body, brass)
    _smooth(body)
    _parent(body)
    _move_to_collection(body, "BW_Talismans")
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.045 * scale, location=end + Vector((0.0, 0.0, -0.31 * scale)))
    clapper = bpy.context.object
    clapper.name = name + "_Clapper"
    _assign(clapper, dark)
    _parent(clapper)
    _move_to_collection(clapper, "BW_Talismans")


def _cloth_strip(name, anchor, length, side):
    material = bpy.data.materials["MAT_BW_RedCharm"]
    x, y, z = anchor
    width = 0.07
    vertices = [
        (x - width, y, z),
        (x + width, y, z),
        (x + width * 0.75 + side * 0.05, y - 0.025, z - length * 0.48),
        (x - width * 0.75 + side * 0.03, y + 0.015, z - length * 0.52),
        (x + width * 0.45 - side * 0.04, y, z - length),
        (x - width * 0.45 - side * 0.02, y, z - length * 0.96),
    ]
    faces = [(0, 1, 2, 3), (3, 2, 4, 5)]
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material)
    obj = bpy.data.objects.new(name, mesh)
    bpy.data.collections["BW_Talismans"].objects.link(obj)
    _parent(obj)


def phase_roots_crown_details():
    random.seed(8128)
    dark = bpy.data.materials["MAT_BW_DarkWood"]
    birch = bpy.data.materials["MAT_BW_Birch"]
    moss = bpy.data.materials["MAT_BW_Moss"]
    vine = bpy.data.materials["MAT_BW_Vine"]
    bone = bpy.data.materials["MAT_BW_Bone"]

    # Wide radial root system creates a strong readable base from both game and portrait cameras.
    for index in range(26):
        angle = index * math.tau / 26.0 + random.uniform(-0.10, 0.10)
        length = random.uniform(2.0, 3.8)
        start = Vector((math.cos(angle) * 0.42, math.sin(angle) * 0.42, 0.42))
        mid = Vector((math.cos(angle) * length * 0.46, math.sin(angle) * length * 0.46, random.uniform(0.06, 0.22)))
        end = Vector((math.cos(angle) * length, math.sin(angle) * length, random.uniform(0.015, 0.07)))
        material = birch if index % 7 == 0 else dark
        _chain(f"Root_{index + 1:02d}", [start, mid, end], [random.uniform(0.19, 0.30), random.uniform(0.09, 0.15), 0.025], material, 9, "BW_Roots")
        if index % 3 == 0:
            split_angle = angle + random.choice((-1, 1)) * random.uniform(0.22, 0.48)
            split = end * 0.55
            fork_end = Vector((math.cos(split_angle) * length * 0.9, math.sin(split_angle) * length * 0.9, 0.025))
            _chain(f"RootFork_{index + 1:02d}", [split, fork_end], [0.075, 0.012], dark, 7, "BW_Roots", joints=False)

    # Tangled hanging fibers bridge torso and ground, matching the reference's robe-like mass.
    for index in range(28):
        angle = random.uniform(0.0, math.tau)
        radius = random.uniform(0.35, 0.78)
        start = Vector((math.cos(angle) * radius, math.sin(angle) * radius, random.uniform(2.2, 3.45)))
        middle = Vector((start.x + random.uniform(-0.25, 0.25), start.y + random.uniform(-0.18, 0.18), random.uniform(0.9, 1.7)))
        end = Vector((start.x + random.uniform(-0.55, 0.55), start.y + random.uniform(-0.45, 0.45), random.uniform(0.06, 0.25)))
        _curve(f"HangingRoot_{index + 1:02d}", [start, middle, end], random.uniform(0.018, 0.045), dark if index % 4 else vine, "BW_Vines")

    # Three climbing vines wind around the central trunk.
    for vine_index in range(3):
        points = []
        for step in range(16):
            t = step / 15.0
            angle = t * math.tau * (1.35 + vine_index * 0.18) + vine_index * 2.0
            radius = 0.72 + math.sin(t * math.pi) * 0.18
            points.append((math.cos(angle) * radius, math.sin(angle) * radius, 0.42 + t * 3.65))
        _curve(f"ClimbingVine_{vine_index + 1}", points, 0.035 - vine_index * 0.005, vine, "BW_Vines")

    # Crown and antlers: deliberately asymmetrical, with a taller right side.
    crown_starts = [
        (-0.42, 0.02, 4.48), (-0.12, 0.05, 4.60), (0.28, 0.04, 4.52),
        (-0.70, 0.03, 3.76), (0.72, 0.02, 3.78), (0.05, 0.18, 4.62),
    ]
    crown_ends = [
        (-1.25, 0.02, 5.52), (-0.52, 0.08, 6.05), (0.70, 0.02, 5.85),
        (-1.72, 0.06, 4.82), (1.72, 0.04, 5.18), (0.16, 0.25, 6.22),
    ]
    crown_tips = []
    for index, (start, end) in enumerate(zip(crown_starts, crown_ends)):
        mid = (Vector(start) + Vector(end)) * 0.5 + Vector((random.uniform(-0.18, 0.18), random.uniform(-0.12, 0.18), 0.10))
        _chain(f"CrownBranch_{index + 1}", [start, mid, end], [0.16, 0.10, 0.035], dark, 9, "BW_Crown")
        end_vec = Vector(end)
        crown_tips.append(end_vec)
        for fork in (-1, 1):
            fork_end = end_vec + Vector((fork * random.uniform(0.25, 0.48), random.uniform(-0.18, 0.22), random.uniform(0.28, 0.55)))
            _chain(f"CrownFork_{index + 1}_{fork}", [mid.lerp(end_vec, 0.62), fork_end], [0.06, 0.018], dark, 7, "BW_Crown", joints=False)
            crown_tips.append(fork_end)

    # Crooked staff is grown through the right hand rather than carried conventionally.
    staff_points = [(1.64, 0.04, 0.18), (1.79, 0.02, 1.75), (1.62, 0.06, 3.20), (1.88, 0.02, 4.65), (1.72, 0.04, 5.70)]
    _chain("LivingStaff", staff_points, [0.20, 0.17, 0.14, 0.10, 0.035], dark, 11, "BW_Crown")
    for fork, end in enumerate(((2.28, 0.02, 5.22), (2.18, 0.16, 5.82), (1.30, 0.06, 5.45))):
        _chain(f"StaffFork_{fork + 1}", [staff_points[-2], end], [0.075, 0.018], dark, 7, "BW_Crown", joints=False)

    # Moss masses are clustered rather than evenly scattered, keeping the silhouette readable.
    moss_points = [
        (-0.58, -0.42, 3.25), (0.48, -0.47, 2.88), (-0.12, -0.65, 2.20),
        (-0.78, 0.02, 1.55), (0.35, -0.52, 1.10), (-1.02, 0.12, 0.30),
        (0.90, 0.16, 0.22), (-1.72, 0.04, 0.15), (1.55, -0.05, 0.12),
    ]
    for index, point in enumerate(moss_points):
        _leaf_cluster(f"MossClump_{index + 1:02d}", point, (random.uniform(0.22, 0.42), random.uniform(0.12, 0.25), random.uniform(0.08, 0.18)), moss)
    for index, tip in enumerate(crown_tips[::3]):
        _leaf_cluster(f"CrownMoss_{index + 1:02d}", tip, (0.18, 0.14, 0.11), moss)

    # Bells, strips and bone charms make the guardian culturally specific to Hollowwest.
    _bell("Bell_Crown_L", (-1.45, -0.04, 4.95), 0.85, 0.86)
    _bell("Bell_Crown_R", (1.66, -0.04, 4.82), 0.72, 0.78)
    _bell("Bell_Staff_High", (2.18, 0.10, 5.42), 0.95, 0.92)
    _bell("Bell_Staff_Low", (1.88, -0.02, 4.25), 0.62, 0.68)
    _cloth_strip("CharmCloth_L", (-1.68, -0.02, 4.63), 0.82, -1)
    _cloth_strip("CharmCloth_R", (1.90, -0.02, 4.65), 0.72, 1)

    for index, anchor in enumerate(((-0.85, -0.72, 2.58), (0.72, -0.73, 2.34), (1.72, -0.02, 3.55))):
        end = Vector(anchor) + Vector((0.0, -0.02, -0.42))
        _curve(f"BoneCharmCord_{index + 1}", [anchor, end], 0.012, vine, "BW_Talismans")
        bpy.ops.mesh.primitive_cone_add(vertices=7, radius1=0.055, radius2=0.025, depth=0.26, location=end + Vector((0.0, 0.0, -0.13)))
        charm = bpy.context.object
        charm.name = f"BoneCharm_{index + 1}"
        _assign(charm, bone)
        _parent(charm)
        _move_to_collection(charm, "BW_Talismans")
    print("Birch Warden: roots, crown and talismans complete")


def phase_quality_pass():
    random.seed(4107)
    dark = bpy.data.materials["MAT_BW_DarkWood"]
    birch = bpy.data.materials["MAT_BW_Birch"]
    scar = bpy.data.materials["MAT_BW_BarkScar"]
    moss = bpy.data.materials["MAT_BW_Moss"]
    vine = bpy.data.materials["MAT_BW_Vine"]

    # The first blockout is intentionally generous; this pass makes it tall,
    # hollow and robe-like instead of reading as a round fantasy golem.
    for obj in bpy.data.objects:
        if obj.name.startswith("TorsoCore"):
            obj.scale.x *= 0.84
            obj.scale.y *= 0.70
    head = bpy.data.objects.get("HeadCore")
    if head is not None:
        head.scale = (0.68, 0.57, 1.05)

    # Pull the wood palette toward damp near-black bark.
    dark_ramp = next((node.color_ramp for node in dark.node_tree.nodes if node.bl_idname == "ShaderNodeValToRGB"), None)
    if dark_ramp is not None:
        dark_ramp.elements[0].color = (0.0025, 0.0015, 0.0010, 1.0)
        dark_ramp.elements[1].color = (0.070, 0.034, 0.012, 1.0)
    birch_ramp = next((node.color_ramp for node in birch.node_tree.nodes if node.bl_idname == "ShaderNodeValToRGB"), None)
    if birch_ramp is not None:
        birch_ramp.elements[0].color = (0.022, 0.017, 0.011, 1.0)
        birch_ramp.elements[1].color = (0.54, 0.51, 0.42, 1.0)

    # A black inset carries the face. The mask is only a broken birch frame.
    _joint("FaceHollow", (0.0, -0.555, 4.18), 0.46, scar, (0.78, 0.18, 1.04), "BW_Details")
    for name, x in (("Eye_L", -0.18), ("Eye_R", 0.18)):
        eye = bpy.data.objects.get(name)
        if eye is not None:
            eye.location = (x, -0.675, 4.27)
            eye.scale = (0.62, 0.28, 0.38)
    for name in ("MaskBrow_L", "MaskBrow_R"):
        obj = bpy.data.objects.get(name)
        if obj is not None:
            obj.location.y = -0.665
            obj.location.z += 0.015
            obj.scale *= 0.72
    nose = bpy.data.objects.get("MaskNose")
    if nose is not None:
        nose.location.y = -0.692
        nose.scale = (0.70, 0.70, 0.74)
    jaw = bpy.data.objects.get("MaskJaw")
    if jaw is not None:
        jaw.location.y = -0.635
        jaw.scale = (0.74, 0.72, 0.72)
    _box("MaskCheek_L", (-0.31, -0.635, 4.08), (0.085, 0.038, 0.25), (math.radians(-4), math.radians(-5), math.radians(-10)), birch)
    _box("MaskCheek_R", (0.31, -0.635, 4.08), (0.085, 0.038, 0.25), (math.radians(-4), math.radians(5), math.radians(10)), birch)

    # Root beard conceals the neck transition and makes the face feel nested
    # inside an old, inhabited tree rather than attached to its surface.
    for index in range(18):
        x = random.uniform(-0.32, 0.32)
        start = (x, -0.66 + random.uniform(-0.025, 0.015), random.uniform(3.88, 4.06))
        mid = (x + random.uniform(-0.16, 0.16), -0.77, random.uniform(3.30, 3.62))
        end = (x + random.uniform(-0.30, 0.30), -0.76, random.uniform(2.58, 3.18))
        _curve(f"RootBeard_{index + 1:02d}", [start, mid, end], random.uniform(0.015, 0.035), dark if index % 3 else vine, "BW_Vines")

    # Layered vertical bark and fibers form the tattered cloak seen in the mood
    # reference. Separate geometry keeps it legible at typical game distances.
    for index in range(34):
        x = random.uniform(-0.78, 0.78)
        start_z = random.uniform(2.65, 3.65)
        end_z = random.uniform(0.22, 1.30)
        y = random.uniform(-0.83, -0.70)
        middle_z = (start_z + end_z) * 0.5 + random.uniform(-0.12, 0.12)
        _curve(
            f"FrontCloakFiber_{index + 1:02d}",
            [(x, y, start_z), (x + random.uniform(-0.24, 0.24), y - random.uniform(0.02, 0.12), middle_z), (x + random.uniform(-0.38, 0.38), y, end_z)],
            random.uniform(0.018, 0.050),
            dark if index % 5 else vine,
            "BW_Vines",
        )
    for index in range(22):
        x = random.uniform(-0.70, 0.70)
        z = random.uniform(1.0, 3.25)
        _box(
            f"CloakBarkShard_{index + 1:02d}",
            (x, random.uniform(-0.84, -0.76), z),
            (random.uniform(0.055, 0.13), random.uniform(0.026, 0.045), random.uniform(0.18, 0.42)),
            (math.radians(random.uniform(-7, 7)), math.radians(random.uniform(-8, 8)), math.radians(random.uniform(-18, 18))),
            birch if index % 8 == 0 else dark,
        )

    # A few moss shelves break the cloak into depth layers.
    for index, point in enumerate(((-0.58, -0.84, 2.95), (0.50, -0.86, 2.42), (-0.24, -0.88, 1.76), (0.60, -0.82, 1.05))):
        _leaf_cluster(f"FrontMossShelf_{index + 1}", point, (0.23, 0.10, 0.07), moss)
    print("Birch Warden: quality pass complete")


def phase_presentation_and_save():
    scene = bpy.context.scene
    ground_material = bpy.data.materials["MAT_BW_Ground"]
    presentation = bpy.data.collections.get("BW_Presentation")
    if presentation is not None:
        for obj in list(presentation.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
    bpy.ops.mesh.primitive_plane_add(size=24.0, location=(0.0, 0.0, -0.01))
    ground = bpy.context.object
    ground.name = "PREVIEW_Ground"
    _assign(ground, ground_material)
    _move_to_collection(ground, "BW_Presentation")

    # A subtle raised disk catches root shadows without reading as a game base.
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=3.75, depth=0.08, location=(0.0, 0.0, 0.0))
    disk = bpy.context.object
    disk.name = "PREVIEW_RootShadowDisk"
    _assign(disk, ground_material)
    bevel = disk.modifiers.new("Ground bevel", "BEVEL")
    bevel.width = 0.18
    bevel.segments = 3
    _move_to_collection(disk, "BW_Presentation")

    camera_data = bpy.data.cameras.new("CAM_BirchWarden_Portrait")
    camera = bpy.data.objects.new("CAM_BirchWarden_Portrait", camera_data)
    bpy.data.collections["BW_Presentation"].objects.link(camera)
    camera.location = (4.4, -14.8, 5.25)
    camera_data.lens = 62.0
    camera_data.sensor_width = 36.0
    _look_at(camera, (0.0, 0.0, 2.85))
    scene.camera = camera

    def area(name, location, color, energy, size):
        data = bpy.data.lights.new(name, "AREA")
        data.color = color
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        obj = bpy.data.objects.new(name, data)
        bpy.data.collections["BW_Presentation"].objects.link(obj)
        obj.location = location
        _look_at(obj, (0.0, 0.0, 2.6))
        return obj

    area("LIGHT_Key_Warm", (-5.2, -7.2, 8.6), (1.0, 0.56, 0.28), 820.0, 5.0)
    area("LIGHT_Fill_Cool", (5.5, -3.5, 5.4), (0.24, 0.46, 0.62), 470.0, 4.0)
    area("LIGHT_Rim_Moon", (1.0, 5.8, 8.2), (0.38, 0.58, 0.76), 1180.0, 3.0)

    scene.render.filepath = RENDER_FILE
    scene.render.image_settings.file_format = "PNG"
    scene.render.resolution_x = 720
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.resolution_percentage = 100

    # Save before and after rendering so a cancelled render never costs model work.
    bpy.ops.wm.save_as_mainfile(filepath=PROJECT_FILE)
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=PROJECT_FILE)
    print({"project": PROJECT_FILE, "render": RENDER_FILE, "objects": len(bpy.data.objects), "materials": len(bpy.data.materials)})


def build_all():
    phase_setup()
    phase_body()
    phase_roots_crown_details()
    phase_quality_pass()
    phase_presentation_and_save()


if __name__ == "__main__":
    build_all()
