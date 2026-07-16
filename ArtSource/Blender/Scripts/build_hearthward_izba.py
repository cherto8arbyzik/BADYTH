"""Procedurally build the game-ready Hearthward izba for BADYTH.

Run inside Blender 5.x. The script creates a presentation scene, three authored
levels of detail, saves the .blend source, renders a preview, and exports three
FBX files ready for the Unity prefab builder.
"""

from __future__ import annotations

import math
import os
import random

import bpy
from mathutils import Vector


ROOT = r"D:\badyth"
BLEND_PATH = os.path.join(
    ROOT, "ArtSource", "Blender", "Buildings", "Izba", "Izba_Hearthward.blend"
)
PREVIEW_PATH = os.path.join(
    ROOT, "ArtSource", "Renders", "Izba_Hearthward_final.png"
)
EXPORT_DIR = os.path.join(
    ROOT,
    "Assets",
    "_Project",
    "Resources",
    "Models",
    "Custom",
    "Buildings",
    "Izba",
    "Geometry",
)
TEXTURE_DIR = os.path.join(
    ROOT,
    "Assets",
    "_Project",
    "Resources",
    "Models",
    "Custom",
    "Buildings",
    "Izba",
    "Textures",
)
PALETTE_PATH = os.path.join(TEXTURE_DIR, "TEX_Izba_Hearthward_Palette.png")

random.seed(9421)


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for collection in list(bpy.data.collections):
        bpy.data.collections.remove(collection)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    for image in list(bpy.data.images):
        if image.name != "Render Result":
            bpy.data.images.remove(image)

    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1100
    scene.render.resolution_y = 850
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.color = (0.025, 0.03, 0.025)


def collection(name):
    value = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(value)
    return value


def move_to_collection(obj, target):
    for source in list(obj.users_collection):
        source.objects.unlink(obj)
    target.objects.link(obj)


def material(name, color, roughness=0.72, metallic=0.0, emission=None, emission_strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    shader = next(node for node in mat.node_tree.nodes if node.type == "BSDF_PRINCIPLED")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metallic
    if emission is not None:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = emission_strength
    return mat


def apply_bevel(obj, width, segments=1):
    if width <= 0.0:
        return
    modifier = obj.modifiers.new("Crafted edge", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def add_box(target, name, location, dimensions, mat, rotation=(0.0, 0.0, 0.0), bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    apply_bevel(obj, bevel)
    move_to_collection(obj, target)
    return obj


def add_cylinder_between(
    target,
    name,
    start,
    end,
    radius,
    side_mat,
    end_mat=None,
    vertices=8,
):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=direction.length,
        end_fill_type="NGON",
        location=(start + end) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    obj.data.materials.append(side_mat)
    if end_mat is not None:
        obj.data.materials.append(end_mat)
        for polygon in obj.data.polygons:
            if len(polygon.vertices) == vertices:
                polygon.material_index = 1
    move_to_collection(obj, target)
    return obj


def add_torus(target, name, location, major_radius, minor_radius, mat, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=16,
        minor_segments=5,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    move_to_collection(obj, target)
    return obj


def prism_yz(target, name, points, x_center, thickness, mat):
    count = len(points)
    vertices = [(x_center - thickness * 0.5, y, z) for y, z in points]
    vertices += [(x_center + thickness * 0.5, y, z) for y, z in points]
    faces = [tuple(range(count - 1, -1, -1)), tuple(range(count, count * 2))]
    for index in range(count):
        following = (index + 1) % count
        faces.append((index, following, count + following, count + index))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    target.objects.link(obj)
    apply_bevel(obj, 0.018)
    return obj


def subtract_intervals(low, high, intervals):
    result = [(low, high)]
    for cut_low, cut_high in intervals:
        updated = []
        for start, end in result:
            if cut_high <= start or cut_low >= end:
                updated.append((start, end))
                continue
            if cut_low > start:
                updated.append((start, cut_low))
            if cut_high < end:
                updated.append((cut_high, end))
        result = updated
    return [(start, end) for start, end in result if end - start > 0.12]


def active_cuts(openings, z):
    cuts = []
    for center, width, bottom, top in openings:
        if bottom <= z <= top:
            cuts.append((center - width * 0.5 - 0.035, center + width * 0.5 + 0.035))
    return cuts


def add_log_walls(target, detail, mats):
    width = 5.0
    depth = 3.8
    half_w = width * 0.5
    half_d = depth * 0.5
    radius = 0.17 if detail == 0 else 0.18
    rows = 7
    front_openings = [(-1.25, 0.92, 0.38, 2.30), (0.92, 1.02, 1.00, 2.06)]
    back_openings = [(-0.45, 1.02, 1.00, 2.06)]
    right_openings = [(0.30, 0.95, 1.02, 2.05)]
    left_openings = [(0.45, 0.78, 1.16, 1.94)]

    if detail == 2:
        add_box(target, "LOD2 wall mass", (0, 0, 1.58), (4.94, 3.74, 2.38), mats["log"], bevel=0.11)
        for z in (0.72, 1.08, 1.44, 1.80, 2.16, 2.52):
            add_box(target, "LOD2 log shadow front", (0, -1.885, z), (4.8, 0.035, 0.045), mats["log_dark"], bevel=0.0)
            add_box(target, "LOD2 log shadow side", (2.485, 0, z), (0.035, 3.58, 0.045), mats["log_dark"], bevel=0.0)
        return

    for row in range(rows):
        z = 0.55 + row * 0.34
        x_extension = 0.20 if row % 2 == 0 else 0.04
        y_extension = 0.20 if row % 2 == 1 else 0.04
        row_mat = mats["log"] if row % 3 else mats["log_dark"]
        for start, end in subtract_intervals(-half_w - x_extension, half_w + x_extension, active_cuts(front_openings, z)):
            add_cylinder_between(target, "Front wall log", (start, -half_d, z), (end, -half_d, z), radius, row_mat, mats["end"], 8 if detail == 0 else 6)
        for start, end in subtract_intervals(-half_w - x_extension, half_w + x_extension, active_cuts(back_openings, z)):
            add_cylinder_between(target, "Back wall log", (start, half_d, z), (end, half_d, z), radius, row_mat, mats["end"], 8 if detail == 0 else 6)
        for start, end in subtract_intervals(-half_d - y_extension, half_d + y_extension, active_cuts(right_openings, z)):
            add_cylinder_between(target, "Right wall log", (half_w, start, z), (half_w, end, z), radius, row_mat, mats["end"], 8 if detail == 0 else 6)
        for start, end in subtract_intervals(-half_d - y_extension, half_d + y_extension, active_cuts(left_openings, z)):
            add_cylinder_between(target, "Left wall log", (-half_w, start, z), (-half_w, end, z), radius, row_mat, mats["end"], 8 if detail == 0 else 6)

    gable_rows = 6 if detail == 0 else 5
    for row in range(gable_rows):
        z = 2.86 + row * 0.31
        half = max(0.28, half_w * (1.0 - (z - 2.70) / 2.34))
        add_cylinder_between(target, "Front gable log", (-half, -half_d, z), (half, -half_d, z), radius * 0.92, mats["log"], mats["end"], 8 if detail == 0 else 6)
        add_cylinder_between(target, "Back gable log", (-half, half_d, z), (half, half_d, z), radius * 0.92, mats["log"], mats["end"], 8 if detail == 0 else 6)


def add_foundation(target, detail, mats):
    if detail > 0:
        add_box(target, "Stone foundation", (0, 0, 0.31), (5.10, 3.88, 0.54), mats["stone"], bevel=0.10)
        return

    add_box(target, "Foundation shadow", (0, 0, 0.31), (4.88, 3.68, 0.48), mats["stone_dark"], bevel=0.08)
    for side in (-1, 1):
        for index in range(10):
            x = -2.32 + index * 0.515 + random.uniform(-0.05, 0.05)
            size = (0.48 + random.uniform(-0.06, 0.06), 0.34, 0.34 + random.uniform(-0.05, 0.08))
            add_box(target, "Hand-laid foundation stone", (x, side * 1.90, 0.34), size, mats["stone"] if index % 3 else mats["stone_dark"], rotation=(0, random.uniform(-0.08, 0.08), random.uniform(-0.10, 0.10)), bevel=0.07)
    for side in (-1, 1):
        for index in range(7):
            y = -1.57 + index * 0.52 + random.uniform(-0.04, 0.04)
            size = (0.34, 0.47 + random.uniform(-0.06, 0.05), 0.35 + random.uniform(-0.04, 0.07))
            add_box(target, "Hand-laid side stone", (side * 2.51, y, 0.34), size, mats["stone"] if index % 2 else mats["stone_dark"], rotation=(random.uniform(-0.05, 0.05), 0, random.uniform(-0.10, 0.10)), bevel=0.07)


def add_roof(target, detail, mats):
    half = 2.85
    eave_z = 2.72
    ridge_z = 5.02
    rise = ridge_z - eave_z
    angle = math.atan2(rise, half)
    slope = math.sqrt(half * half + rise * rise)
    for side in (-1, 1):
        rotation = (0, -angle if side < 0 else angle, 0)
        add_box(target, "Roof underlay", (side * half * 0.5, 0, (eave_z + ridge_z) * 0.5), (slope + 0.08, 4.48, 0.13), mats["roof_dark"], rotation=rotation, bevel=0.018)

        if detail == 0:
            courses = 12
            tile_count = 11
            for course in range(courses):
                t = 0.055 + course * 0.078
                x = side * (half - half * t)
                z = eave_z + rise * t + 0.085
                offset = 0.19 if course % 2 else 0.0
                for tile_index in range(tile_count):
                    y = -2.08 + tile_index * 0.415 + offset
                    if y > 2.14:
                        continue
                    tile_mat = mats["roof"] if (tile_index + course) % 4 else mats["roof_mid"]
                    add_box(
                        target,
                        "Split aspen roof shingle",
                        (x, y, z),
                        (0.48, 0.44, 0.045),
                        tile_mat,
                        rotation=rotation,
                        bevel=0.012,
                    )
        elif detail == 1:
            for course in range(10):
                t = 0.075 + course * 0.092
                x = side * (half - half * t)
                z = eave_z + rise * t + 0.078
                add_box(target, "LOD1 shingle course", (x, 0, z), (0.48, 4.40, 0.05), mats["roof"] if course % 3 else mats["roof_mid"], rotation=rotation, bevel=0.012)

    add_cylinder_between(target, "Carved ridge pole", (0, -2.43, 5.05), (0, 2.43, 5.05), 0.09 if detail < 2 else 0.075, mats["trim"], mats["end"], 8 if detail == 0 else 6)

    if detail == 0:
        moss_specs = [
            (-1, 0.32, -0.72, 0.62, 0.48),
            (-1, 0.56, 1.02, 0.45, 0.68),
            (1, 0.30, 0.65, 0.70, 0.42),
            (1, 0.68, -1.16, 0.42, 0.55),
        ]
        for side, t, y, sx, sy in moss_specs:
            x = side * (half - half * t)
            z = eave_z + rise * t + 0.135
            add_box(target, "Moss patch", (x, y, z), (sx, sy, 0.025), mats["moss"], rotation=(0, -angle if side < 0 else angle, random.uniform(-0.12, 0.12)), bevel=0.035)


def add_window_front_back(target, x, y, outward, detail, mats, width=0.94, height=0.92):
    z = 1.55
    face = y + outward * 0.055
    add_box(target, "Warm window glass", (x, face, z), (width, 0.055, height), mats["window"], bevel=0.018)
    trim = 0.105 if detail == 0 else 0.12
    for dx in (-width * 0.5 - trim * 0.35, width * 0.5 + trim * 0.35):
        add_box(target, "Carved vertical casing", (x + dx, face + outward * 0.035, z), (trim, 0.09, height + 0.24), mats["accent"], bevel=0.018)
    add_box(target, "Carved window sill", (x, face + outward * 0.04, z - height * 0.5 - 0.11), (width + 0.30, 0.11, 0.12), mats["trim"], bevel=0.018)
    add_box(target, "Carved window crown", (x, face + outward * 0.045, z + height * 0.5 + 0.12), (width + 0.34, 0.11, 0.14), mats["accent"], bevel=0.02)
    add_box(target, "Window mullion", (x, face + outward * 0.055, z), (0.055, 0.075, height - 0.05), mats["trim"], bevel=0.008)
    add_box(target, "Window crossbar", (x, face + outward * 0.057, z), (width - 0.04, 0.075, 0.055), mats["trim"], bevel=0.008)
    if detail == 0:
        for side in (-1, 1):
            shutter_x = x + side * (width * 0.5 + 0.24)
            add_box(target, "Open protective shutter", (shutter_x, face + outward * 0.11, z), (0.36, 0.075, height + 0.05), mats["accent_dark"] if side < 0 else mats["accent"], rotation=(0, 0, side * outward * 0.26), bevel=0.025)
            add_box(target, "Shutter diamond", (shutter_x, face + outward * 0.155, z), (0.19, 0.04, 0.19), mats["trim"], rotation=(0, math.radians(45), side * outward * 0.26), bevel=0.015)
        for dx in (-0.28, 0, 0.28):
            add_box(target, "Casing tooth", (x + dx, face + outward * 0.07, z + height * 0.5 + 0.23), (0.13, 0.08, 0.13), mats["trim"], rotation=(0, math.radians(45), 0), bevel=0.012)


def add_window_side(target, x, y, outward, detail, mats, width=0.86, height=0.86):
    z = 1.56
    face = x + outward * 0.055
    add_box(target, "Warm side window", (face, y, z), (0.055, width, height), mats["window"], bevel=0.018)
    trim = 0.11
    for dy in (-width * 0.5 - trim * 0.35, width * 0.5 + trim * 0.35):
        add_box(target, "Side casing upright", (face + outward * 0.035, y + dy, z), (0.09, trim, height + 0.22), mats["accent"], bevel=0.018)
    add_box(target, "Side window sill", (face + outward * 0.04, y, z - height * 0.5 - 0.10), (0.11, width + 0.28, 0.12), mats["trim"], bevel=0.018)
    add_box(target, "Side window crown", (face + outward * 0.045, y, z + height * 0.5 + 0.11), (0.11, width + 0.30, 0.14), mats["accent"], bevel=0.02)
    add_box(target, "Side window mullion", (face + outward * 0.055, y, z), (0.075, 0.055, height - 0.04), mats["trim"], bevel=0.008)
    add_box(target, "Side window crossbar", (face + outward * 0.055, y, z), (0.075, width - 0.04, 0.055), mats["trim"], bevel=0.008)
    if detail == 0:
        for side in (-1, 1):
            shutter_y = y + side * (width * 0.5 + 0.22)
            add_box(target, "Side open shutter", (face + outward * 0.11, shutter_y, z), (0.075, 0.34, height + 0.02), mats["accent_dark"] if side < 0 else mats["accent"], rotation=(0, 0, side * outward * 0.24), bevel=0.025)


def add_door(target, detail, mats):
    x = -1.25
    y = -1.955
    add_box(target, "Oak plank door", (x, y, 1.34), (0.90, 0.10, 1.88), mats["trim_dark"], bevel=0.035)
    plank_count = 5 if detail == 0 else 3
    for index in range(plank_count):
        px = x - 0.34 + index * (0.68 / max(1, plank_count - 1))
        add_box(target, "Door plank seam", (px, y - 0.06, 1.34), (0.025, 0.025, 1.76), mats["log_dark"], bevel=0.004)
    add_box(target, "Door lintel", (x, y - 0.08, 2.35), (1.18, 0.14, 0.18), mats["accent"], bevel=0.025)
    add_box(target, "Door left casing", (x - 0.54, y - 0.07, 1.36), (0.16, 0.14, 2.05), mats["accent"], bevel=0.025)
    add_box(target, "Door right casing", (x + 0.54, y - 0.07, 1.36), (0.16, 0.14, 2.05), mats["accent"], bevel=0.025)
    add_box(target, "Door diagonal brace", (x, y - 0.075, 1.30), (0.12, 0.09, 1.02), mats["trim"], rotation=(0, math.radians(-39), 0), bevel=0.018)
    if detail == 0:
        add_torus(target, "Iron door ring", (x + 0.27, y - 0.135, 1.38), 0.09, 0.018, mats["iron"], rotation=(math.radians(90), 0, 0))
        add_box(target, "Protective door diamond", (x, y - 0.13, 1.87), (0.26, 0.055, 0.26), mats["accent"], rotation=(0, math.radians(45), 0), bevel=0.018)


def add_porch(target, detail, mats):
    center_x = -1.25
    add_box(target, "Porch floor", (center_x, -2.48, 0.57), (2.28, 1.12, 0.20), mats["trim_dark"], bevel=0.035)
    for index, (y, z, depth, width) in enumerate(((-3.12, 0.16, 0.42, 1.78), (-2.91, 0.33, 0.38, 1.98))):
        add_box(target, "Porch step", (center_x, y, z), (width, depth, 0.20), mats["trim"], bevel=0.035)
    for x in (-2.17, -0.33):
        add_box(target, "Porch carved post", (x, -2.88, 1.57), (0.15, 0.15, 2.04), mats["trim"], bevel=0.022)
        add_box(target, "Red post collar", (x, -2.88, 2.05), (0.22, 0.22, 0.13), mats["accent"], bevel=0.018)
        if detail == 0:
            add_cylinder_between(target, "Porch knee brace", (x, -2.86, 2.05), (x + (0.30 if x < center_x else -0.30), -2.54, 2.35), 0.055, mats["accent"], mats["end"], 6)

    porch_half = 1.28
    ridge_z = 3.08
    edge_z = 2.53
    angle = math.atan2(ridge_z - edge_z, porch_half)
    slope = math.sqrt(porch_half * porch_half + (ridge_z - edge_z) ** 2)
    for side in (-1, 1):
        x = center_x + side * porch_half * 0.5
        rotation = (0, -angle if side < 0 else angle, 0)
        add_box(target, "Asymmetric porch roof", (x, -2.48, (ridge_z + edge_z) * 0.5), (slope + 0.05, 1.72, 0.11), mats["roof"], rotation=rotation, bevel=0.018)
        if detail == 0:
            for course in range(4):
                t = 0.18 + course * 0.22
                px = center_x + side * (porch_half - porch_half * t)
                pz = edge_z + (ridge_z - edge_z) * t + 0.07
                add_box(target, "Porch shingle course", (px, -2.48, pz), (0.42, 1.70, 0.045), mats["roof_mid"] if course % 2 else mats["roof"], rotation=rotation, bevel=0.010)
    add_cylinder_between(target, "Porch ridge", (center_x, -3.35, ridge_z + 0.02), (center_x, -1.62, ridge_z + 0.02), 0.065, mats["accent"], mats["end"], 8 if detail == 0 else 6)


def add_chimney(target, detail, mats):
    add_box(target, "Fieldstone chimney", (1.43, 0.58, 4.04), (0.55, 0.50, 1.72), mats["stone_dark"], bevel=0.055)
    add_box(target, "Chimney cap", (1.43, 0.58, 4.94), (0.72, 0.66, 0.17), mats["iron"], bevel=0.035)
    if detail == 0:
        for z in (3.52, 3.90, 4.28, 4.66):
            add_box(target, "Chimney masonry band", (1.43, 0.325, z), (0.52, 0.045, 0.075), mats["stone"], bevel=0.012)
        add_box(target, "Chimney spark crown", (1.43, 0.58, 5.10), (0.42, 0.36, 0.17), mats["iron"], bevel=0.025)


def add_firewood_rack(target, detail, mats):
    if detail == 2:
        return
    x = -2.68
    for y in (-0.58, 0.95):
        add_box(target, "Firewood rack post", (x, y, 1.03), (0.13, 0.13, 1.32), mats["trim_dark"], bevel=0.018)
    add_box(target, "Firewood rack canopy", (-2.72, 0.19, 1.78), (0.50, 1.78, 0.10), mats["roof_mid"], rotation=(0, math.radians(-10), 0), bevel=0.020)
    stacks = 3 if detail == 0 else 2
    rows = 4 if detail == 0 else 3
    for row in range(rows):
        for index in range(stacks):
            y = -0.36 + index * 0.52 + (0.12 if row % 2 else 0.0)
            z = 0.61 + row * 0.25
            add_cylinder_between(target, "Stacked split firewood", (-2.52, y, z), (-2.86, y, z), 0.105, mats["trim"], mats["end"], 7 if detail == 0 else 6)


def add_folk_accents(target, detail, mats):
    if detail < 2:
        add_torus(target, "Protective sun wheel", (0.0, -2.025, 3.63), 0.27, 0.043, mats["accent"], rotation=(math.radians(90), 0, 0))
        ray_count = 8 if detail == 0 else 4
        for index in range(ray_count):
            angle = index * math.pi / ray_count
            add_box(target, "Sun wheel ray", (0.0, -2.08, 3.63), (0.72, 0.065, 0.055), mats["trim"], rotation=(0, angle, 0), bevel=0.010)

    front_profile = [
        (-2.16, 4.96),
        (-2.34, 5.32),
        (-2.48, 5.60),
        (-2.57, 5.82),
        (-2.65, 5.57),
        (-2.84, 5.69),
        (-3.04, 5.53),
        (-2.82, 5.35),
        (-2.57, 5.34),
        (-2.40, 5.06),
    ]
    if detail == 2:
        front_profile = [(-2.16, 4.98), (-2.44, 5.54), (-2.74, 5.60), (-2.92, 5.43), (-2.57, 5.32), (-2.38, 5.02)]
    prism_yz(target, "Hearthward horse-head finial", front_profile, 0.0, 0.14 if detail == 0 else 0.12, mats["accent_dark"])
    back_profile = [(-y, z) for y, z in front_profile]
    prism_yz(target, "Rear horse-head finial", back_profile, 0.0, 0.14 if detail == 0 else 0.12, mats["accent_dark"])

    if detail == 0:
        for y in (-2.79, 2.79):
            add_torus(target, "Finial eye", (0.075, y, 5.52), 0.035, 0.013, mats["iron"], rotation=(0, math.radians(90), 0))
        # Three large geometric teeth create a readable, non-literal protection mark.
        for x in (-0.36, 0.0, 0.36):
            add_box(target, "Gable protection tooth", (x, -2.04, 4.27 - abs(x) * 0.18), (0.18, 0.07, 0.18), mats["trim"], rotation=(0, math.radians(45), 0), bevel=0.015)


def add_windows_and_door(target, detail, mats):
    add_door(target, detail, mats)
    add_window_front_back(target, 0.92, -1.92, -1, detail, mats)
    add_window_front_back(target, -0.45, 1.92, 1, detail, mats)
    add_window_side(target, 2.52, 0.30, 1, detail, mats)
    if detail == 0:
        add_window_side(target, -2.52, 0.45, -1, detail, mats, width=0.72, height=0.72)


def remove_duplicate_material_slots(obj):
    unique = []
    remap = {}
    for index, mat in enumerate(obj.data.materials):
        if mat not in unique:
            unique.append(mat)
        remap[index] = unique.index(mat)
    preserved_indices = [remap.get(polygon.material_index, 0) for polygon in obj.data.polygons]
    obj.data.materials.clear()
    for mat in unique:
        obj.data.materials.append(mat)
    for polygon, material_index in zip(obj.data.polygons, preserved_indices):
        polygon.material_index = material_index


def create_palette_material(mats):
    def linear_to_srgb(value):
        return 12.92 * value if value <= 0.0031308 else 1.055 * (value ** (1.0 / 2.4)) - 0.055

    palette = [
        mats["stone_dark"],
        mats["stone"],
        mats["log_dark"],
        mats["end"],
        mats["log"],
        mats["trim_dark"],
        mats["accent"],
        mats["trim"],
        mats["accent_dark"],
        mats["roof_dark"],
        mats["roof_mid"],
        mats["roof"],
        mats["moss"],
    ]
    width = 512
    height = 32
    image = bpy.data.images.new("TEX_Izba_Hearthward_Palette", width=width, height=height, alpha=True)
    pixels = [0.0] * (width * height * 4)
    for y in range(height):
        for x in range(width):
            index = min(len(palette) - 1, int(x * len(palette) / width))
            color = palette[index].diffuse_color
            offset = (y * width + x) * 4
            pixels[offset] = linear_to_srgb(float(color[0]))
            pixels[offset + 1] = linear_to_srgb(float(color[1]))
            pixels[offset + 2] = linear_to_srgb(float(color[2]))
            pixels[offset + 3] = float(color[3])
    image.pixels.foreach_set(pixels)
    image.update()
    image.colorspace_settings.name = "sRGB"
    image.filepath_raw = PALETTE_PATH
    image.file_format = "PNG"
    image.save()
    image.reload()

    atlas = bpy.data.materials.new("IZB_Atlas_Opaque")
    atlas.diffuse_color = (1, 1, 1, 1)
    atlas.use_nodes = True
    nodes = atlas.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = image
    texture.interpolation = "Closest"
    texture.extension = "CLIP"
    shader.inputs["Roughness"].default_value = 0.84
    atlas.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    atlas.node_tree.links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return atlas, palette


def collapse_to_runtime_materials(obj, atlas, palette, iron, window):
    palette_lookup = {mat.name: index for index, mat in enumerate(palette)}
    original_materials = list(obj.data.materials)
    for existing_uv in list(obj.data.uv_layers):
        obj.data.uv_layers.remove(existing_uv)
    uv_layer = obj.data.uv_layers.new(name="IZB_PaletteUV")
    obj.data.uv_layers.active = uv_layer
    uv_layer.active_render = True
    material_indices = []
    for polygon in obj.data.polygons:
        source = original_materials[polygon.material_index]
        if source.name == iron.name:
            runtime_index = 1
            palette_u = 0.5
        elif source.name == window.name:
            runtime_index = 2
            palette_u = 0.5
        else:
            runtime_index = 0
            palette_index = palette_lookup.get(source.name, 0)
            palette_u = (palette_index + 0.5) / len(palette)
        material_indices.append(runtime_index)
        for loop_index in polygon.loop_indices:
            uv_layer.data[loop_index].uv = (palette_u, 0.5)

    obj.data.materials.clear()
    obj.data.materials.append(atlas)
    obj.data.materials.append(iron)
    obj.data.materials.append(window)
    for polygon, material_index in zip(obj.data.polygons, material_indices):
        polygon.material_index = material_index
    obj["runtime_material_slots"] = 3
    obj["palette_texture"] = "TEX_Izba_Hearthward_Palette.png"


def join_collection_meshes(target, name):
    meshes = [obj for obj in target.objects if obj.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = name
    joined.data.name = name + "_Mesh"
    remove_duplicate_material_slots(joined)
    for polygon in joined.data.polygons:
        polygon.use_smooth = False
    joined["asset_id"] = "izba_hearthward"
    joined["unit_scale_meters"] = 1.0
    joined["lod"] = name.rsplit("LOD", 1)[-1]
    joined["design_notes"] = "Slavic frontier housing; carved protective horse-head ridge and sun mark"
    return joined


def build_house(detail, mats):
    target = collection(f"IZBA_LOD{detail}")
    add_foundation(target, detail, mats)
    add_log_walls(target, detail, mats)
    add_windows_and_door(target, detail, mats)
    add_roof(target, detail, mats)
    add_porch(target, detail, mats)
    add_chimney(target, detail, mats)
    add_firewood_rack(target, detail, mats)
    add_folk_accents(target, detail, mats)
    joined = join_collection_meshes(target, f"SM_Izba_Hearthward_LOD{detail}")
    joined["recommended_screen_height"] = (0.62, 0.28, 0.0)[detail]
    return joined


def triangle_count(obj):
    return sum(max(0, len(poly.vertices) - 2) for poly in obj.data.polygons)


def export_fbx(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.hide_set(False)
    obj.hide_viewport = False
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )


def look_at(obj, point):
    direction = Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_preview(mats, lod0, lod1, lod2):
    preview = collection("PREVIEW_STUDIO")
    add_box(preview, "Preview ground", (0, 0, -0.07), (16, 16, 0.12), mats["ground"], bevel=0.18)

    bpy.ops.object.camera_add(location=(8.6, -10.2, 8.0))
    camera = bpy.context.object
    camera.name = "CAM_Izba_Preview"
    camera.data.lens = 58
    look_at(camera, (0, -0.15, 2.25))
    move_to_collection(camera, preview)
    bpy.context.scene.camera = camera

    lights = [
        ("Key", (4.8, -5.8, 10.5), 1250, 5.5, (1.0, 0.76, 0.52)),
        ("Fill", (-6.0, -1.5, 6.5), 820, 5.0, (0.48, 0.63, 1.0)),
        ("Rim", (2.0, 6.5, 8.5), 1100, 4.0, (1.0, 0.30, 0.16)),
    ]
    for name, location, energy, size, color in lights:
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.name = f"LIGHT_{name}"
        light.data.energy = energy
        light.data.shape = "DISK"
        light.data.size = size
        light.data.color = color
        look_at(light, (0, 0, 2.0))
        move_to_collection(light, preview)

    lod0.hide_viewport = False
    lod0.hide_render = False
    lod1.hide_viewport = True
    lod1.hide_render = True
    lod2.hide_viewport = True
    lod2.hide_render = True

    scene = bpy.context.scene
    scene.render.filepath = PREVIEW_PATH
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.look = "AgX - Medium High Contrast"


def main():
    for path in (os.path.dirname(BLEND_PATH), os.path.dirname(PREVIEW_PATH), EXPORT_DIR, TEXTURE_DIR):
        os.makedirs(path, exist_ok=True)
    reset_scene()

    mats = {
        "log": material("IZB_Wood_Log_Oak", (0.25, 0.095, 0.028), 0.82),
        "log_dark": material("IZB_Wood_Log_Weathered", (0.155, 0.048, 0.017), 0.88),
        "end": material("IZB_Wood_Endgrain", (0.44, 0.19, 0.055), 0.86),
        "trim": material("IZB_Wood_Carved_Warm", (0.57, 0.245, 0.055), 0.74),
        "trim_dark": material("IZB_Wood_Smoked", (0.13, 0.045, 0.018), 0.84),
        "accent": material("IZB_Accent_Rowen_Red", (0.42, 0.055, 0.024), 0.78),
        "accent_dark": material("IZB_Accent_Oxblood", (0.205, 0.018, 0.012), 0.82),
        "roof": material("IZB_Roof_Aspen_Dark", (0.115, 0.032, 0.018), 0.92),
        "roof_mid": material("IZB_Roof_Aspen_Worn", (0.22, 0.067, 0.030), 0.90),
        "roof_dark": material("IZB_Roof_Underlay", (0.065, 0.018, 0.012), 0.94),
        "stone": material("IZB_Stone_Fieldstone", (0.26, 0.285, 0.27), 0.95),
        "stone_dark": material("IZB_Stone_Damp", (0.125, 0.15, 0.14), 0.98),
        "iron": material("IZB_Iron_Black", (0.025, 0.030, 0.032), 0.42, 0.72),
        "window": material("IZB_Window_Hearth_Glow", (0.08, 0.035, 0.015), 0.30, emission=(1.0, 0.26, 0.045), emission_strength=2.6),
        "moss": material("IZB_Moss_Roof", (0.16, 0.225, 0.065), 0.98),
        "ground": material("PREVIEW_Ground", (0.065, 0.085, 0.055), 0.98),
    }

    lod0 = build_house(0, mats)
    lod1 = build_house(1, mats)
    lod2 = build_house(2, mats)

    atlas, palette = create_palette_material(mats)
    for obj in (lod0, lod1, lod2):
        collapse_to_runtime_materials(obj, atlas, palette, mats["iron"], mats["window"])

    for obj in (lod0, lod1, lod2):
        obj["triangle_count"] = triangle_count(obj)

    export_fbx(lod0, os.path.join(EXPORT_DIR, "SM_Izba_Hearthward_LOD0.fbx"))
    export_fbx(lod1, os.path.join(EXPORT_DIR, "SM_Izba_Hearthward_LOD1.fbx"))
    export_fbx(lod2, os.path.join(EXPORT_DIR, "SM_Izba_Hearthward_LOD2.fbx"))

    setup_preview(mats, lod0, lod1, lod2)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    print(
        "HEARTHWARD_IZBA_COMPLETE",
        {
            "blend": BLEND_PATH,
            "preview": PREVIEW_PATH,
            "lod0_tris": triangle_count(lod0),
            "lod1_tris": triangle_count(lod1),
            "lod2_tris": triangle_count(lod2),
        },
    )


if __name__ == "__main__":
    main()
