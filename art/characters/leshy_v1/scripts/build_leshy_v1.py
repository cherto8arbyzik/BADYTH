"""Procedural production build for the reference-matched Leshy v1.

Run inside Blender through the Blender MCP in stages.  The module deliberately
keeps geometry categories separate for Unity materials, inspection, and LOD
work while using deterministic construction for repeatable QA renders.
"""

from __future__ import annotations

import json
import math
import os
from pathlib import Path
from typing import Iterable, Sequence

import bpy
from mathutils import Matrix, Vector


ROOT = Path(r"D:\badyth\art\characters\leshy_v1")
MASTER = ROOT / "blend" / "Leshy_v1_MASTER.blend"
CHECKPOINTS = ROOT / "blend" / "checkpoints"
RENDERS = ROOT / "renders"

MODEL_COLLECTIONS = (
    "LESHY_GEO",
    "LESHY_CROWN",
    "LESHY_MOSS",
    "LESHY_VINES",
    "LESHY_ACCESSORIES",
)


def _collection(name: str) -> bpy.types.Collection:
    coll = bpy.data.collections.get(name)
    if coll is None:
        coll = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(coll)
    return coll


def _move_to_collection(obj: bpy.types.Object, collection_name: str) -> None:
    coll = _collection(collection_name)
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    coll.objects.link(obj)


def _material(name: str, base: tuple[float, float, float, float], roughness: float,
              metallic: float = 0.0, noise_scale: float | None = None,
              second: tuple[float, float, float, float] | None = None,
              emission: tuple[float, float, float, float] | None = None,
              emission_strength: float = 0.0) -> bpy.types.Material:
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = base
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    if emission:
        emission_input = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if emission_input:
            emission_input.default_value = emission
        strength_input = bsdf.inputs.get("Emission Strength")
        if strength_input:
            strength_input.default_value = emission_strength
    if noise_scale and second:
        tex = nodes.new("ShaderNodeTexNoise")
        tex.inputs["Scale"].default_value = noise_scale
        tex.inputs["Detail"].default_value = 5.0
        tex.inputs["Roughness"].default_value = 0.72
        ramp = nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.30
        ramp.color_ramp.elements[0].color = second
        ramp.color_ramp.elements[1].position = 0.72
        ramp.color_ramp.elements[1].color = base
        links.new(tex.outputs["Fac"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
        bump = nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = 0.22
        bump.inputs["Distance"].default_value = 0.055
        links.new(tex.outputs["Fac"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def make_materials() -> dict[str, bpy.types.Material]:
    return {
        "birch": _material("MAT_Leshy_Birch", (0.34, 0.32, 0.275, 1), 0.86, noise_scale=7.5,
                           second=(0.055, 0.047, 0.038, 1)),
        "wood": _material("MAT_Leshy_DarkWood", (0.055, 0.046, 0.036, 1), 0.9, noise_scale=5.0,
                          second=(0.012, 0.010, 0.008, 1)),
        "cavity": _material("MAT_Leshy_FaceCavity", (0.002, 0.0015, 0.001, 1), 1.0),
        "moss": _material("MAT_Leshy_Moss", (0.042, 0.052, 0.014, 1), 0.96, noise_scale=8.0,
                          second=(0.006, 0.009, 0.003, 1)),
        "copper": _material("MAT_Leshy_Copper", (0.24, 0.095, 0.035, 1), 0.54, metallic=0.82,
                            noise_scale=9.0, second=(0.035, 0.105, 0.073, 1)),
        "red": _material("MAT_Leshy_ClothRed", (0.22, 0.025, 0.022, 1), 0.96, noise_scale=12.0,
                         second=(0.055, 0.012, 0.010, 1)),
        "white": _material("MAT_Leshy_ClothWhite", (0.66, 0.59, 0.45, 1), 0.98, noise_scale=11.0,
                           second=(0.20, 0.17, 0.125, 1)),
        "amber": _material("MAT_Leshy_EyesAmber", (0.24, 0.075, 0.006, 1), 0.35,
                           emission=(0.75, 0.18, 0.012, 1), emission_strength=0.45),
        "ornament": _material("MAT_Leshy_Ornament", (0.34, 0.18, 0.07, 1), 0.9),
        "silhouette": _material("MAT_QA_Silhouette", (1.0, 1.0, 1.0, 1), 1.0),
    }


def _mesh_object(name: str, vertices: list[tuple[float, float, float]], faces: list[tuple[int, ...]],
                 material: bpy.types.Material, collection: str) -> bpy.types.Object:
    mesh = bpy.data.meshes.new(f"{name}_MESH")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    _collection(collection).objects.link(obj)
    obj.data.materials.append(material)
    for poly in mesh.polygons:
        poly.use_smooth = True
    return obj


def _frame(tangent: Vector) -> tuple[Vector, Vector]:
    t = tangent.normalized()
    helper = Vector((0.0, 0.0, 1.0))
    if abs(t.dot(helper)) > 0.94:
        helper = Vector((1.0, 0.0, 0.0))
    u = t.cross(helper).normalized()
    v = t.cross(u).normalized()
    # Make near-vertical chains use X then Y for predictable elliptical depth.
    if abs(t.z) > 0.85 and abs(u.x) < abs(v.x):
        u, v = v, -u
    return u, v


def make_chain(name: str, points: Sequence[Sequence[float]], radii: Sequence[float],
               material: bpy.types.Material, collection: str, sides: int = 10,
               depth_scale: float = 1.0, steps_per_segment: int = 3) -> bpy.types.Object:
    control = [Vector(p) for p in points]
    if len(control) != len(radii) or len(control) < 2:
        raise ValueError(f"Bad chain {name}")
    pts: list[Vector] = []
    sampled_radii: list[float] = []
    for i in range(len(control) - 1):
        p0 = control[max(0, i - 1)]
        p1 = control[i]
        p2 = control[i + 1]
        p3 = control[min(len(control) - 1, i + 2)]
        for step in range(steps_per_segment):
            t = step / steps_per_segment
            t2, t3 = t * t, t * t * t
            point = 0.5 * ((2.0 * p1) + (-p0 + p2) * t +
                           (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2 +
                           (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3)
            pts.append(point)
            sampled_radii.append(float(radii[i] * (1.0 - t) + radii[i + 1] * t))
    pts.append(control[-1])
    sampled_radii.append(float(radii[-1]))
    verts: list[tuple[float, float, float]] = []
    for i, (point, radius) in enumerate(zip(pts, sampled_radii)):
        if i == 0:
            tangent = pts[1] - point
        elif i == len(pts) - 1:
            tangent = point - pts[i - 1]
        else:
            tangent = pts[i + 1] - pts[i - 1]
        u, v = _frame(tangent)
        for j in range(sides):
            angle = 2.0 * math.pi * j / sides
            offset = u * (math.cos(angle) * radius) + v * (math.sin(angle) * radius * depth_scale)
            verts.append(tuple(point + offset))
    faces: list[tuple[int, ...]] = []
    for i in range(len(pts) - 1):
        a = i * sides
        b = (i + 1) * sides
        for j in range(sides):
            faces.append((a + j, a + (j + 1) % sides, b + (j + 1) % sides, b + j))
    faces.append(tuple(reversed(tuple(range(sides)))))
    end = (len(pts) - 1) * sides
    faces.append(tuple(end + j for j in range(sides)))
    return _mesh_object(name, verts, faces, material, collection)


def make_ellipsoid(name: str, location: Sequence[float], scale: Sequence[float],
                   material: bpy.types.Material, collection: str, subdivisions: int = 2) -> bpy.types.Object:
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    _move_to_collection(obj, collection)
    obj.data.materials.append(material)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def make_prism(name: str, outline_xz: Sequence[Sequence[float]], y_front: float, y_back: float,
               material: bpy.types.Material, collection: str) -> bpy.types.Object:
    n = len(outline_xz)
    verts = [(x, y_front, z) for x, z in outline_xz] + [(x, y_back, z) for x, z in outline_xz]
    faces: list[tuple[int, ...]] = [tuple(range(n)), tuple(reversed(tuple(range(n, 2 * n))))]
    for i in range(n):
        faces.append((i, (i + 1) % n, n + (i + 1) % n, n + i))
    obj = _mesh_object(name, verts, faces, material, collection)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return obj


def join_objects(objects: Iterable[bpy.types.Object], name: str) -> bpy.types.Object | None:
    objects = [o for o in objects if o and o.name in bpy.data.objects]
    if not objects:
        return None
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    objects[0].name = name
    return objects[0]


def stage_reset_and_materials() -> dict:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for coll in list(bpy.data.collections):
        if coll.users == 0 or coll.name in MODEL_COLLECTIONS or coll.name == "LESHY_QA":
            bpy.data.collections.remove(coll)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    scene = bpy.context.scene
    scene.name = "Leshy_v1_MASTER"
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = "METERS"
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1536
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    for name in MODEL_COLLECTIONS:
        _collection(name)
    _collection("LESHY_QA")
    root = bpy.data.objects.new("LESHY_ROOT", None)
    _collection("LESHY_GEO").objects.link(root)
    root["asset_name"] = "Leshy_v1"
    root["canonical_height_m"] = 3.2
    root["forward_axis"] = "-Z on Unity export"
    root["forbidden_items"] = "weapon, staff, armor, pedestal, extra jewelry"
    materials = make_materials()
    return {"scene": scene.name, "materials": list(materials), "file": str(MASTER)}


def stage_body() -> dict:
    m = make_materials()
    body: list[bpy.types.Object] = []
    dark: list[bpy.types.Object] = []

    # Central layered root torso: compact in depth, broad at the shoulders.
    dark.append(make_chain("body_core", [(0, 0.07, 1.25), (0, 0.06, 1.62), (0, 0.04, 2.02),
                                         (0, 0.02, 2.35), (0, 0.0, 2.52)],
                           [0.23, 0.29, 0.32, 0.34, 0.25], m["wood"], "LESHY_GEO", 12, 1.02))
    for idx, (x, y, lean) in enumerate(((-0.12, 0.015, -0.03), (0.12, 0.025, 0.025),
                                        (-0.055, -0.09, 0.02), (0.06, 0.12, -0.015)), 1):
        dark.append(make_chain(f"torso_root_{idx:02d}", [(x, y, 1.18), (x + lean, y, 1.72),
                                                         (x - lean, y, 2.22), (x, y, 2.53)],
                               [0.075, 0.09, 0.08, 0.045], m["wood"], "LESHY_GEO", 9, 0.8))

    mantle_outline = [(-0.25, 2.47), (-0.34, 2.25), (-0.35, 1.95), (-0.33, 1.55),
                      (-0.35, 1.12), (-0.38, 0.68), (-0.43, 0.27), (-0.29, 0.18),
                      (0.28, 0.18), (0.43, 0.27), (0.38, 0.68), (0.35, 1.12),
                      (0.33, 1.55), (0.35, 1.95), (0.34, 2.25), (0.25, 2.47)]
    dark.append(make_prism("body_root_mantle", mantle_outline, -0.42, 0.42,
                           m["wood"], "LESHY_GEO"))

    # Long birch legs with separated root structures.
    for side, sx in (("L", -1.0), ("R", 1.0)):
        body.append(make_chain(f"leg_{side}", [(0.16 * sx, 0.04, 1.53), (0.205 * sx, 0.025, 1.17),
                                               (0.235 * sx, -0.005, 0.73), (0.27 * sx, -0.02, 0.22)],
                               [0.155, 0.145, 0.125, 0.105], m["birch"], "LESHY_GEO", 12, 0.78))
        # Heel and five readable root toes.  The outer toe establishes the broad silhouette.
        foot_roots = [
            ((0.27 * sx, -0.01, 0.21), (0.31 * sx, -0.22, 0.09), (0.34 * sx, -0.46, 0.025)),
            ((0.27 * sx, -0.02, 0.19), (0.40 * sx, -0.16, 0.080), (0.55 * sx, -0.32, 0.018)),
            ((0.27 * sx, 0.0, 0.18), (0.49 * sx, -0.03, 0.075), (0.73 * sx, -0.12, 0.015)),
            ((0.26 * sx, 0.02, 0.18), (0.45 * sx, 0.15, 0.075), (0.62 * sx, 0.32, 0.018)),
            ((0.25 * sx, 0.04, 0.17), (0.32 * sx, 0.27, 0.070), (0.35 * sx, 0.48, 0.018)),
        ]
        for n, pts in enumerate(foot_roots, 1):
            body.append(make_chain(f"foot_{side}_{n:02d}", pts, [0.09, 0.055, 0.012],
                                   m["birch"], "LESHY_GEO", 9, 0.85))
        extra_roots = [
            ((0.26 * sx, -0.01, 0.18), (0.53 * sx, -0.20, 0.065), (0.82 * sx, -0.24, 0.012)),
            ((0.25 * sx, 0.02, 0.17), (0.48 * sx, 0.21, 0.060), (0.76 * sx, 0.30, 0.012)),
            ((0.25 * sx, 0.03, 0.16), (0.27 * sx, 0.32, 0.055), (0.25 * sx, 0.54, 0.012)),
        ]
        for n, pts in enumerate(extra_roots, 6):
            body.append(make_chain(f"foot_{side}_{n:02d}", pts, [0.065, 0.035, 0.009],
                                   m["wood"], "LESHY_GEO", 8, 0.8))

    # Narrow shoulders and deliberately overlong arms.
    for side, sx in (("L", -1.0), ("R", 1.0)):
        body.append(make_chain(f"arm_{side}", [(0.28 * sx, -0.005, 2.43), (0.43 * sx, -0.025, 2.18),
                                               (0.54 * sx, -0.035, 1.78), (0.635 * sx, -0.045, 1.39),
                                               (0.70 * sx, -0.06, 1.13)],
                               [0.17, 0.16, 0.135, 0.105, 0.075], m["birch"], "LESHY_GEO", 12, 0.76))
        # Root palm and four long fingers plus a splayed thumb.
        body.append(make_chain(f"palm_{side}", [(0.70 * sx, -0.06, 1.14), (0.725 * sx, -0.075, 1.01)],
                               [0.07, 0.047], m["birch"], "LESHY_GEO", 9, 0.7))
        fingers = [
            ((0.71 * sx, -0.08, 1.02), (0.78 * sx, -0.095, 0.91), (0.82 * sx, -0.08, 0.82)),
            ((0.72 * sx, -0.075, 1.01), (0.75 * sx, -0.11, 0.88), (0.77 * sx, -0.12, 0.76)),
            ((0.73 * sx, -0.06, 1.00), (0.70 * sx, -0.12, 0.87), (0.69 * sx, -0.13, 0.77)),
            ((0.73 * sx, -0.045, 1.01), (0.65 * sx, -0.10, 0.90), (0.61 * sx, -0.09, 0.82)),
            ((0.695 * sx, -0.09, 1.07), (0.80 * sx, -0.15, 1.00), (0.87 * sx, -0.15, 0.94)),
        ]
        for n, pts in enumerate(fingers, 1):
            body.append(make_chain(f"finger_{side}_{n:02d}", pts, [0.038, 0.026, 0.009],
                                   m["birch"], "LESHY_GEO", 8, 0.8))

    # Birch shoulder slabs and layered bark ridges increase the reference's lean-but-strong read.
    for side, sx in (("L", -1.0), ("R", 1.0)):
        for n, z in enumerate((2.35, 2.25), 1):
            outline = [(0.27 * sx, z + 0.065), (0.37 * sx, z + 0.045),
                       (0.40 * sx, z - 0.035), (0.29 * sx, z - 0.055)]
            body.append(make_prism(f"shoulder_bark_{side}_{n}", outline, -0.15 - n * 0.003,
                                   -0.105, m["birch"], "LESHY_GEO"))

    body_obj = join_objects(body + dark, "LESHY_BODY")
    body_obj["construction"] = "layered birch limbs over dark root core"
    return {"body_object": body_obj.name, "parts_joined": len(body) + len(dark)}


def stage_face_and_crown() -> dict:
    m = make_materials()
    cavity = make_ellipsoid("LESHY_FACE_CAVITY", (0.0, -0.165, 2.715), (0.205, 0.125, 0.305),
                            m["cavity"], "LESHY_GEO", 2)

    mask_parts = [
        make_prism("mask_forehead", [(-0.11, 2.94), (-0.052, 2.995), (0.0, 3.015),
                                     (0.058, 2.995), (0.115, 2.93), (0.075, 2.845),
                                     (0.028, 2.815), (-0.03, 2.815), (-0.078, 2.85)],
                   -0.286, -0.238, m["birch"], "LESHY_GEO"),
        make_prism("mask_cheek_L", [(-0.135, 2.865), (-0.070, 2.80), (-0.072, 2.70),
                                    (-0.098, 2.545), (-0.14, 2.625), (-0.158, 2.775)],
                   -0.298, -0.245, m["birch"], "LESHY_GEO"),
        make_prism("mask_cheek_R", [(0.135, 2.865), (0.070, 2.80), (0.072, 2.70),
                                    (0.098, 2.545), (0.14, 2.625), (0.158, 2.775)],
                   -0.298, -0.245, m["birch"], "LESHY_GEO"),
        make_prism("mask_nose", [(-0.042, 2.79), (0.0, 2.84), (0.042, 2.79),
                                 (0.025, 2.61), (0.0, 2.545), (-0.025, 2.61)],
                   -0.322, -0.285, m["birch"], "LESHY_GEO"),
        make_prism("mask_brow_L", [(-0.142, 2.825), (-0.04, 2.82), (-0.055, 2.775), (-0.15, 2.76)],
                   -0.326, -0.286, m["birch"], "LESHY_GEO"),
        make_prism("mask_brow_R", [(0.142, 2.825), (0.04, 2.82), (0.055, 2.775), (0.15, 2.76)],
                   -0.326, -0.286, m["birch"], "LESHY_GEO"),
    ]
    mask = join_objects(mask_parts, "LESHY_MASK")

    mask_cracks = [
        make_chain("mask_crack_01", [(-0.035, -0.333, 2.965), (-0.055, -0.334, 2.89),
                                     (-0.035, -0.334, 2.83)], [0.006, 0.004, 0.002],
                   m["wood"], "LESHY_GEO", 6, 0.35),
        make_chain("mask_crack_02", [(0.055, -0.333, 2.94), (0.035, -0.334, 2.88),
                                     (0.06, -0.334, 2.84)], [0.006, 0.004, 0.002],
                   m["wood"], "LESHY_GEO", 6, 0.35),
        make_chain("mask_crack_03", [(-0.14, -0.334, 2.80), (-0.12, -0.335, 2.72),
                                     (-0.14, -0.334, 2.65)], [0.005, 0.0035, 0.002],
                   m["wood"], "LESHY_GEO", 6, 0.35),
    ]
    join_objects(mask_cracks, "LESHY_MASK_CRACKS")

    for side, x in (("L", -0.055), ("R", 0.055)):
        eye = make_ellipsoid(f"LESHY_EYE_{side}", (x, -0.326, 2.765), (0.012, 0.008, 0.010),
                             m["amber"], "LESHY_GEO", 2)
        eye["glow"] = "dim amber only"

    branches = [
        # Two dominant asymmetric shoulder branches.
        [(-0.03, 0.02, 2.84), (-0.28, 0.03, 2.96), (-0.53, 0.035, 2.98), (-0.78, 0.04, 2.94)],
        [(0.03, 0.01, 2.83), (0.27, 0.02, 2.94), (0.49, 0.025, 2.91), (0.76, 0.03, 2.98)],
        # Tall crown forks, with the first ending at the designed 3.2 m top.
        [(-0.08, 0.04, 2.89), (-0.15, 0.05, 3.03), (-0.19, 0.05, 3.12), (-0.205, 0.05, 3.185)],
        [(0.08, 0.055, 2.90), (0.16, 0.06, 3.02), (0.25, 0.055, 3.11), (0.31, 0.05, 3.165)],
        [(-0.01, 0.075, 2.91), (0.00, 0.09, 3.04), (-0.04, 0.10, 3.13)],
        # Secondary upward and downward forks.
        [(-0.31, 0.03, 2.96), (-0.36, 0.035, 3.08), (-0.42, 0.04, 3.145)],
        [(-0.52, 0.035, 2.98), (-0.58, 0.04, 3.07), (-0.64, 0.04, 3.11)],
        [(-0.66, 0.04, 2.96), (-0.72, 0.04, 3.04), (-0.75, 0.04, 3.09)],
        [(0.29, 0.02, 2.94), (0.36, 0.025, 3.06), (0.41, 0.02, 3.14)],
        [(0.49, 0.025, 2.91), (0.55, 0.03, 3.02), (0.58, 0.025, 3.09)],
        [(0.66, 0.03, 2.95), (0.70, 0.035, 3.05), (0.74, 0.03, 3.11)],
        [(-0.46, 0.035, 2.98), (-0.43, 0.04, 2.88), (-0.38, 0.04, 2.82)],
        [(0.58, 0.03, 2.92), (0.62, 0.03, 2.83), (0.67, 0.025, 2.78)],
    ]
    crown_objs = []
    for idx, pts in enumerate(branches, 1):
        depth = (-0.16, 0.15, -0.08, 0.11, 0.18, -0.13, 0.14, -0.18,
                 0.16, -0.12, 0.10, -0.16, 0.15)[idx - 1]
        pts = [(x, y + depth * (i / max(1, len(pts) - 1)), z) for i, (x, y, z) in enumerate(pts)]
        length = len(pts)
        crown_objs.append(make_chain(f"LESHY_CROWN_{idx:02d}", pts,
                                     [0.038 * (1.0 - 0.72 * i / (length - 1)) for i in range(length)],
                                     m["birch"] if idx <= 2 else m["wood"], "LESHY_CROWN", 9, 0.82))
    # Thin branch-tip forks.
    twig_specs = [
        ((-0.28, 0.03, 3.0), (-0.33, 0.035, 3.10)), ((-0.53, 0.035, 2.98), (-0.53, 0.04, 3.13)),
        ((-0.72, 0.04, 2.97), (-0.83, 0.04, 3.03)), ((-0.76, 0.04, 2.95), (-0.86, 0.04, 2.90)),
        ((0.27, 0.02, 2.95), (0.20, 0.03, 3.10)), ((0.49, 0.025, 2.92), (0.47, 0.03, 3.10)),
        ((0.64, 0.03, 2.95), (0.63, 0.03, 3.12)), ((0.74, 0.03, 2.99), (0.84, 0.03, 3.04)),
        ((-0.19, 0.05, 3.12), (-0.27, 0.05, 3.18)), ((0.30, 0.05, 3.16), (0.37, 0.05, 3.19)),
        ((-0.04, 0.10, 3.13), (-0.01, 0.10, 3.19)), ((0.16, 0.06, 3.03), (0.10, 0.06, 3.15)),
        ((-0.34, 0.04, 3.08), (-0.29, 0.04, 3.18)), ((-0.42, 0.04, 3.14), (-0.48, 0.04, 3.19)),
        ((-0.58, 0.04, 3.07), (-0.53, 0.04, 3.17)), ((-0.69, 0.04, 3.02), (-0.65, 0.04, 3.13)),
        ((-0.78, 0.04, 2.94), (-0.88, 0.04, 2.98)), ((-0.61, 0.04, 2.98), (-0.67, 0.04, 2.88)),
        ((0.36, 0.03, 3.06), (0.31, 0.03, 3.17)), ((0.41, 0.02, 3.14), (0.47, 0.02, 3.19)),
        ((0.55, 0.03, 3.02), (0.51, 0.03, 3.14)), ((0.70, 0.03, 3.05), (0.65, 0.03, 3.16)),
        ((0.76, 0.03, 2.98), (0.88, 0.03, 3.02)), ((0.64, 0.03, 2.95), (0.69, 0.03, 2.86)),
    ]
    start_index = len(crown_objs) + 1
    for n, (a, b) in enumerate(twig_specs, start_index):
        depth = -0.15 if n % 2 else 0.16
        b = (b[0], b[1] + depth, b[2])
        crown_objs.append(make_chain(f"LESHY_CROWN_{n:02d}", [a, b], [0.014, 0.0045],
                                     m["wood"], "LESHY_CROWN", 7, 0.8))
    return {"mask": mask.name, "cavity": cavity.name, "crown_objects": len(crown_objs), "eyes": 2}


def stage_moss_vines_accessories() -> dict:
    m = make_materials()
    # Volumetric moss clumps placed asymmetrically, leaving the face readable.
    moss_specs = [
        (-0.29, -0.01, 2.48, 0.16, 0.10, 0.11), (0.31, 0.02, 2.43, 0.17, 0.11, 0.13),
        (-0.43, 0.035, 2.78, 0.14, 0.08, 0.08), (0.44, 0.035, 2.73, 0.12, 0.08, 0.07),
        (-0.55, 0.04, 2.96, 0.12, 0.07, 0.055), (0.50, 0.03, 2.91, 0.10, 0.07, 0.05),
        (-0.19, -0.10, 2.14, 0.11, 0.07, 0.17), (0.18, -0.09, 2.05, 0.13, 0.07, 0.20),
        (-0.23, 0.10, 1.72, 0.11, 0.08, 0.20), (0.24, 0.10, 1.55, 0.12, 0.08, 0.22),
        (-0.27, 0.03, 0.58, 0.12, 0.09, 0.16), (0.28, 0.02, 0.49, 0.13, 0.09, 0.17),
        (-0.43, -0.03, 0.12, 0.14, 0.09, 0.07), (0.45, -0.03, 0.11, 0.16, 0.09, 0.07),
        (-0.31, -0.34, 2.24, 0.10, 0.07, 0.15), (0.29, -0.35, 2.15, 0.11, 0.07, 0.17),
        (-0.33, -0.37, 1.82, 0.09, 0.06, 0.16), (0.32, -0.37, 1.68, 0.10, 0.06, 0.18),
        (-0.35, -0.38, 1.23, 0.09, 0.06, 0.17), (0.34, -0.38, 1.06, 0.10, 0.06, 0.18),
        (-0.37, -0.37, 0.62, 0.10, 0.07, 0.14), (0.37, -0.37, 0.50, 0.11, 0.07, 0.14),
        (-0.56, -0.08, 1.54, 0.09, 0.065, 0.12), (0.57, -0.08, 1.45, 0.09, 0.065, 0.12),
        (-0.13, -0.43, 2.37, 0.19, 0.055, 0.085), (0.14, -0.43, 2.35, 0.19, 0.055, 0.09),
    ]
    moss_objs = []
    for idx, (x, y, z, sx, sy, sz) in enumerate(moss_specs, 1):
        parts = []
        offsets = ((0.0, 0.0, 0.0, 0.72), (-0.48, 0.16, -0.08, 0.48),
                   (0.42, -0.18, 0.10, 0.52), (0.05, 0.22, -0.40, 0.46),
                   (-0.12, -0.18, 0.43, 0.42))
        for part_idx, (ox, oy, oz, size) in enumerate(offsets, 1):
            parts.append(make_ellipsoid(f"moss_{idx:02d}_part_{part_idx}",
                                        (x + ox * sx, y + oy * sy, z + oz * sz),
                                        (sx * size, sy * size * 0.85, sz * size),
                                        m["moss"], "LESHY_MOSS", 2))
        moss_objs.append(join_objects(parts, f"LESHY_MOSS_{idx:02d}"))

    moss_strands = []
    for idx in range(58):
        x, y, z, sx, sy, sz = moss_specs[idx % len(moss_specs)]
        lane = ((idx * 19) % 13 - 6) / 6.0
        start = (x + lane * sx * 0.65, y - sy * 0.25, z - sz * 0.10)
        length = 0.10 + 0.035 * (idx % 7)
        end_z = max(0.02, start[2] - length)
        mid_z = start[2] + (end_z - start[2]) * 0.55
        moss_strands.append(make_chain(f"LESHY_MOSS_STRAND_{idx + 1:02d}",
                                       [start, (start[0] + 0.015 * (-1 if idx % 2 else 1), start[1] - 0.012, mid_z),
                                        (start[0] - 0.010 * (-1 if idx % 2 else 1), start[1], end_z)],
                                       [0.007, 0.0045, 0.0015], m["moss"], "LESHY_MOSS", 5, 0.65))

    # Root/vine mantle. Curves are meshes so FBX export and LOD generation remain deterministic.
    vine_anchors = [
        (-0.30, -0.02, 2.52), (-0.20, -0.10, 2.48), (-0.08, -0.13, 2.43),
        (0.08, -0.13, 2.43), (0.20, -0.10, 2.49), (0.31, -0.02, 2.50),
        (-0.42, 0.04, 2.80), (-0.55, 0.04, 2.96), (0.43, 0.04, 2.77), (0.54, 0.03, 2.91),
    ]
    vine_objs = []
    for idx in range(38):
        ax, ay, az = vine_anchors[idx % len(vine_anchors)]
        side = -1.0 if idx % 2 == 0 else 1.0
        lane = ((idx * 37) % 11 - 5) / 5.0
        end_z = 0.26 + ((idx * 23) % 62) / 100.0
        end_x = max(-0.52, min(0.52, ax + side * (0.07 + 0.025 * (idx % 4)) + lane * 0.035))
        y_bias = -0.30 - 0.035 * (idx % 4) if idx % 2 else 0.27 + 0.035 * (idx % 4)
        pts = [
            (ax, ay, az),
            (ax + side * 0.035, y_bias, az - 0.48),
            (end_x - side * 0.025, y_bias + 0.025 * lane, (az + end_z) * 0.50),
            (end_x, y_bias * 0.7, end_z),
        ]
        base_r = 0.012 + 0.002 * (idx % 4)
        vine_objs.append(make_chain(f"LESHY_VINES_{idx + 1:02d}", pts,
                                    [base_r, base_r * 0.78, base_r * 0.48, 0.0045],
                                    m["moss"] if idx % 5 == 0 else m["wood"],
                                    "LESHY_VINES", 7, 0.85))

    # Thick hidden root bundles establish the depth visible in strict-left and 3/4 references.
    cloak_index = len(vine_objs) + 1
    for layer, y in enumerate((-0.42, -0.33, 0.32, 0.43)):
        for lane, x in enumerate((-0.20, -0.07, 0.07, 0.20)):
            end_x = x + (0.035 if (layer + lane) % 2 else -0.035)
            pts = [(x * 0.75, y * 0.45, 2.45 - 0.04 * lane),
                   (x, y, 1.92), (end_x, y * 0.94, 1.12), (end_x * 1.08, y * 0.88, 0.34 + 0.05 * lane)]
            vine_objs.append(make_chain(f"LESHY_VINES_{cloak_index:02d}", pts,
                                        [0.038, 0.034, 0.026, 0.009],
                                        m["wood"], "LESHY_VINES", 9, 0.88))
            cloak_index += 1

    for side, sx in (("L", -1.0), ("R", 1.0)):
        for idx, z in enumerate((2.08, 1.84, 1.58, 1.34, 1.16), 1):
            t = (2.43 - z) / 1.30
            arm_x = sx * (0.29 + 0.40 * t)
            for fork in (-1.0, 1.0):
                end_x = arm_x + sx * (0.08 + 0.02 * idx)
                end_y = -0.08 + fork * 0.075
                vine_objs.append(make_chain(f"LESHY_VINES_ARM_{side}_{idx}_{'A' if fork < 0 else 'B'}",
                                            [(arm_x, -0.08, z),
                                             (arm_x + sx * 0.045, -0.09 + fork * 0.03, z - 0.06),
                                             (end_x, end_y, z - 0.11 - 0.015 * idx)],
                                            [0.014, 0.008, 0.0025],
                                            m["moss"] if (idx + int(fork)) % 2 else m["wood"],
                                            "LESHY_VINES", 6, 0.7))

    # Exactly two cloth ribbons, both on the character's left branch (+X / viewer-right).
    def ribbon(name: str, x: float, material: bpy.types.Material, phase: float) -> bpy.types.Object:
        rows = 9
        verts: list[tuple[float, float, float]] = []
        half_w = 0.038
        top = 2.76
        bottom = 1.92
        for i in range(rows):
            t = i / (rows - 1)
            z = top * (1 - t) + bottom * t
            cx = x + math.sin(t * math.pi * 2.4 + phase) * 0.014
            cy = -0.17 + math.sin(t * math.pi * 1.7 + phase) * 0.018
            taper = 1.0 - 0.12 * t
            verts.extend(((cx - half_w * taper, cy - 0.006, z), (cx + half_w * taper, cy - 0.006, z),
                          (cx - half_w * taper, cy + 0.006, z), (cx + half_w * taper, cy + 0.006, z)))
        faces: list[tuple[int, ...]] = []
        for i in range(rows - 1):
            a, b = i * 4, (i + 1) * 4
            faces.extend(((a, a + 1, b + 1, b), (a + 2, b + 2, b + 3, a + 3),
                          (a, b, b + 2, a + 2), (a + 1, a + 3, b + 3, b + 1)))
        faces.extend(((0, 2, 3, 1), ((rows - 1) * 4, (rows - 1) * 4 + 1,
                                      (rows - 1) * 4 + 3, (rows - 1) * 4 + 2)))
        obj = _mesh_object(name, verts, faces, material, "LESHY_ACCESSORIES")
        for poly in obj.data.polygons:
            poly.use_smooth = False
        return obj

    red = ribbon("LESHY_CLOTH_RED", 0.47, m["red"], 0.2)
    white = ribbon("LESHY_CLOTH_WHITE", 0.57, m["white"], 1.1)
    # Restrained geometric ornament is separate surface geometry parented to each cloth, not an extra ribbon.
    ornament_parts = []
    for ribbon_obj, x, phase, suffix in ((red, 0.47, 0.2, "RED"), (white, 0.57, 1.1, "WHITE")):
        for i in range(5):
            z = 2.61 - i * 0.13
            outline = [(x - 0.017, z), (x, z + 0.025), (x + 0.017, z), (x, z - 0.025)]
            ornament_parts.append(make_prism(f"ornament_{suffix}_{i}", outline, -0.183, -0.176,
                                             m["white"] if suffix == "RED" else m["ornament"],
                                             "LESHY_ACCESSORIES"))
    ornament = join_objects(ornament_parts, "LESHY_CLOTH_ORNAMENT")

    def bell(name: str, x: float, y: float, z: float, size: float) -> bpy.types.Object:
        profile = [
            (0.018, 0.115), (0.034, 0.090), (0.040, 0.060), (0.046, 0.030),
            (0.062, -0.030), (0.084, -0.085), (0.102, -0.112), (0.108, -0.132),
            (0.087, -0.148),
        ]
        sides = 16
        verts = []
        for radius, dz in profile:
            for j in range(sides):
                a = 2 * math.pi * j / sides
                verts.append((x + radius * size * math.cos(a), y + radius * size * math.sin(a), z + dz * size))
        faces = []
        for i in range(len(profile) - 1):
            for j in range(sides):
                a = i * sides + j
                b = i * sides + (j + 1) % sides
                c = (i + 1) * sides + (j + 1) % sides
                d = (i + 1) * sides + j
                faces.append((a, b, c, d))
        faces.append(tuple(reversed(tuple(range(sides)))))
        return _mesh_object(name, verts, faces, m["copper"], "LESHY_ACCESSORIES")

    bell_specs = [(0.60, -0.18, 2.47, 0.62), (0.68, -0.17, 2.27, 0.70), (0.63, -0.17, 2.02, 0.76)]
    bells = []
    for idx, (x, y, z, size) in enumerate(bell_specs, 1):
        bells.append(bell(f"LESHY_BELL_{idx:02d}", x, y, z, size))
        top_z = z + 0.125 * size
        cord = make_chain(f"LESHY_CORD_BELL_{idx:02d}", [(0.60 + idx * 0.02, -0.02, 2.78),
                                                          (x, -0.12, top_z)],
                          [0.009, 0.005], m["wood"], "LESHY_ACCESSORIES", 7, 0.8)
        cord["supports"] = bells[-1].name

    # Two ribbon ties are cords, not additional cloth pieces.
    for idx, x in enumerate((0.47, 0.57), 1):
        make_chain(f"LESHY_CORD_CLOTH_{idx:02d}", [(0.52, -0.01, 2.79), (x, -0.15, 2.755)],
                   [0.009, 0.005], m["wood"], "LESHY_ACCESSORIES", 7, 0.8)

    # Coal-dark bark fissures and lifted crown plates break the clean procedural surfaces.
    cracks = []
    for side, sx in (("L", -1.0), ("R", 1.0)):
        for idx, offset in enumerate((-0.035, 0.0, 0.038), 1):
            cracks.append(make_chain(f"crack_arm_{side}_{idx}",
                                     [(0.40 * sx + offset, -0.126, 2.18),
                                      (0.52 * sx + offset * 0.6, -0.116, 1.79),
                                      (0.62 * sx + offset * 0.3, -0.096, 1.42)],
                                     [0.009, 0.006, 0.003], m["wood"], "LESHY_GEO", 6, 0.35))
            cracks.append(make_chain(f"crack_leg_{side}_{idx}",
                                     [(0.18 * sx + offset, -0.122, 1.47),
                                      (0.22 * sx + offset * 0.6, -0.109, 0.92),
                                      (0.26 * sx + offset * 0.2, -0.088, 0.36)],
                                     [0.010, 0.006, 0.003], m["wood"], "LESHY_GEO", 6, 0.35))
    cracks_obj = join_objects(cracks, "LESHY_BODY_CRACKS")

    # Fine roots ride over the solid mantle volume and provide the layered, non-columnar read.
    for face_idx, y in enumerate((-0.438, 0.438), 1):
        for lane in range(31):
            x = -0.30 + lane * 0.020
            sway = 0.025 if lane % 2 else -0.022
            top_z = 2.39 - 0.025 * (lane % 4)
            end_z = 0.24 + 0.035 * (lane % 6)
            vine_objs.append(make_chain(f"LESHY_VINES_SURFACE_{face_idx}_{lane + 1:02d}",
                                        [(x * 0.78, y, top_z), (x + sway, y * 1.01, 1.70),
                                         (x - sway * 0.7, y * 0.99, 0.92), (x + sway * 0.4, y, end_z)],
                                        [0.015, 0.013, 0.009, 0.0025],
                                        m["moss"] if lane % 5 == 0 else m["wood"],
                                        "LESHY_VINES", 7, 0.65))

    bark_flakes = []
    for side, sx in (("L", -1.0), ("R", 1.0)):
        for idx, z in enumerate((2.12, 1.91, 1.68, 1.45), 1):
            t = (2.43 - z) / 1.30
            x = sx * (0.30 + 0.40 * t)
            outline = [(x - 0.035, z + 0.055), (x + 0.028, z + 0.045),
                       (x + 0.038, z - 0.048), (x - 0.025, z - 0.062)]
            bark_flakes.append(make_prism(f"arm_flake_{side}_{idx}", outline, -0.168, -0.135,
                                          m["birch"], "LESHY_GEO"))
        for idx, z in enumerate((1.34, 1.06, 0.78, 0.49), 1):
            x = sx * (0.18 + (1.45 - z) * 0.08)
            outline = [(x - 0.040, z + 0.068), (x + 0.032, z + 0.055),
                       (x + 0.043, z - 0.060), (x - 0.030, z - 0.072)]
            bark_flakes.append(make_prism(f"leg_flake_{side}_{idx}", outline, -0.145, -0.112,
                                          m["birch"], "LESHY_GEO"))
    flakes_obj = join_objects(bark_flakes, "LESHY_BODY_BARK_FLAKES")

    bark_plates = []
    plate_specs = [(-0.62, 3.01, 0), (-0.46, 3.02, 0), (-0.31, 3.04, 0),
                   (-0.14, 3.08, 0), (0.16, 3.06, 0), (0.34, 3.03, 0),
                   (0.51, 2.98, 0), (0.66, 3.00, 0)]
    for idx, (x, z, angle) in enumerate(plate_specs, 1):
        w, h = 0.010, 0.018
        outline = [(x - w, z + h), (x + w * 0.7, z + h * 0.8),
                   (x + w, z - h), (x - w * 0.75, z - h * 0.75)]
        plate = make_prism(f"LESHY_CROWN_BARK_{idx:02d}", outline, -0.062, -0.025,
                           m["birch"], "LESHY_CROWN")
        bark_plates.append(plate)

    return {
        "moss_clumps": len(moss_objs), "moss_strands": len(moss_strands), "vines": len(vine_objs),
        "bells": [o.name for o in bells], "cloth": [red.name, white.name],
        "ornament": ornament.name, "cracks": cracks_obj.name, "bark_flakes": flakes_obj.name,
        "crown_bark_plates": len(bark_plates),
    }


def stage_surface_detail() -> dict:
    """Apply real mesh density and restrained bark relief to the hero LOD0."""
    body = bpy.data.objects.get("LESHY_BODY")
    if body is None:
        raise RuntimeError("LESHY_BODY is missing")
    if not body.get("surface_detail_applied"):
        bpy.ops.object.select_all(action="DESELECT")
        body.select_set(True)
        bpy.context.view_layer.objects.active = body
        tri = body.modifiers.new("LOD0_Triangulate", "TRIANGULATE")
        tri.keep_custom_normals = True
        bpy.ops.object.modifier_apply(modifier=tri.name)
        subdiv = body.modifiers.new("LOD0_SurfaceDensity", "SUBSURF")
        subdiv.subdivision_type = "SIMPLE"
        subdiv.levels = 1
        subdiv.render_levels = 1
        bpy.ops.object.modifier_apply(modifier=subdiv.name)
        texture = bpy.data.textures.get("TEX_Leshy_BarkRelief") or bpy.data.textures.new(
            "TEX_Leshy_BarkRelief", type="CLOUDS"
        )
        texture.noise_scale = 0.075
        texture.noise_depth = 2
        displace = body.modifiers.new("LOD0_BarkRelief", "DISPLACE")
        displace.texture = texture
        displace.texture_coords = "GLOBAL"
        displace.strength = 0.009
        displace.mid_level = 0.5
        bpy.ops.object.modifier_apply(modifier=displace.name)
        for poly in body.data.polygons:
            poly.use_smooth = True
        body["surface_detail_applied"] = True

    for name, width in (("LESHY_MASK", 0.004), ("LESHY_CLOTH_RED", 0.0025),
                        ("LESHY_CLOTH_WHITE", 0.0025)):
        obj = bpy.data.objects.get(name)
        if obj is None or obj.get("edge_detail_applied"):
            continue
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bevel = obj.modifiers.new("LOD0_EdgeSoftening", "BEVEL")
        bevel.width = width
        bevel.segments = 2
        bevel.limit_method = "ANGLE"
        bpy.ops.object.modifier_apply(modifier=bevel.name)
        obj["edge_detail_applied"] = True

    moss_texture = bpy.data.textures.get("TEX_Leshy_MossRelief") or bpy.data.textures.new(
        "TEX_Leshy_MossRelief", type="CLOUDS"
    )
    moss_texture.noise_scale = 0.035
    moss_texture.noise_depth = 2
    for obj in _model_meshes():
        if not obj.name.startswith("LESHY_MOSS_") or "STRAND" in obj.name or obj.get("moss_relief_applied"):
            continue
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        displace = obj.modifiers.new("LOD0_MossRelief", "DISPLACE")
        displace.texture = moss_texture
        displace.texture_coords = "GLOBAL"
        displace.strength = 0.022
        displace.mid_level = 0.5
        bpy.ops.object.modifier_apply(modifier=displace.name)
        obj["moss_relief_applied"] = True

    return {"triangles_after_surface_detail": _triangle_count(), "body_vertices": len(body.data.vertices)}


def _look_at(obj: bpy.types.Object, target: Sequence[float]) -> None:
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def _world_background(world: bpy.types.World) -> bpy.types.Node:
    return next(node for node in world.node_tree.nodes if node.type == "BACKGROUND")


def stage_qa() -> dict:
    m = make_materials()
    coll = _collection("LESHY_QA")
    for obj in list(coll.objects):
        bpy.data.objects.remove(obj, do_unlink=True)

    cameras = {
        "QA_FRONT": ((0.0, -8.0, 1.60), (0.0, 0.0, 1.60)),
        "QA_BACK": ((0.0, 8.0, 1.60), (0.0, 0.0, 1.60)),
        "QA_LEFT": ((-8.0, -0.09, 1.60), (0.0, -0.09, 1.60)),
        "QA_FRONT_3Q": ((-5.657, -5.657, 1.60), (0.0, 0.0, 1.60)),
        "QA_BACK_3Q": ((5.657, 5.657, 1.60), (0.0, 0.0, 1.60)),
    }
    for name, (location, target) in cameras.items():
        data = bpy.data.cameras.new(f"{name}_DATA")
        data.type = "ORTHO"
        data.ortho_scale = 3.31
        data.lens = 50
        obj = bpy.data.objects.new(name, data)
        coll.objects.link(obj)
        obj.location = location
        _look_at(obj, target)

    lights = [
        ("QA_KEY", (-3.0, -4.0, 5.2), 1100.0, 4.0),
        ("QA_FILL", (4.0, -2.5, 3.6), 700.0, 4.0),
        ("QA_RIM", (1.0, 4.0, 4.8), 1250.0, 3.0),
    ]
    for name, location, energy, size in lights:
        data = bpy.data.lights.new(f"{name}_DATA", "AREA")
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        obj = bpy.data.objects.new(name, data)
        coll.objects.link(obj)
        obj.location = location
        _look_at(obj, (0.0, 0.0, 1.65))

    scene = bpy.context.scene
    world = bpy.data.worlds.get("Leshy_QA_World") or bpy.data.worlds.new("Leshy_QA_World")
    scene.world = world
    world.use_nodes = True
    bg = _world_background(world)
    bg.inputs["Color"].default_value = (0.115, 0.115, 0.115, 1.0)
    bg.inputs["Strength"].default_value = 0.32
    return {"cameras": list(cameras), "lights": [x[0] for x in lights], "silhouette_material": m["silhouette"].name}


def _model_meshes() -> list[bpy.types.Object]:
    qa_names = {"LESHY_QA"}
    result = []
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        if any(coll.name in qa_names for coll in obj.users_collection):
            continue
        result.append(obj)
    return result


def normalize_height() -> dict:
    objects = _model_meshes()
    min_z = min((obj.matrix_world @ v.co).z for obj in objects for v in obj.data.vertices)
    max_z = max((obj.matrix_world @ v.co).z for obj in objects for v in obj.data.vertices)
    scale = 3.2 / (max_z - min_z)
    for obj in objects:
        inv = obj.matrix_world.inverted()
        for vert in obj.data.vertices:
            world = obj.matrix_world @ vert.co
            world = Vector((world.x * scale, world.y * scale, (world.z - min_z) * scale))
            vert.co = inv @ world
        obj.data.update()
    min_after = min((obj.matrix_world @ v.co).z for obj in objects for v in obj.data.vertices)
    max_after = max((obj.matrix_world @ v.co).z for obj in objects for v in obj.data.vertices)
    return {"before": [min_z, max_z], "scale": scale, "after": [min_after, max_after], "height": max_after - min_after}


def _triangle_count() -> int:
    total = 0
    for obj in _model_meshes():
        obj.data.calc_loop_triangles()
        total += len(obj.data.loop_triangles)
    return total


def _accessory_audit() -> dict:
    names = [obj.name for obj in _model_meshes()]
    return {
        "bells": sorted(n for n in names if n.startswith("LESHY_BELL_")),
        "cloth": sorted(n for n in names if n in {"LESHY_CLOTH_RED", "LESHY_CLOTH_WHITE"}),
        "forbidden_name_hits": sorted(n for n in names if any(word in n.lower() for word in ("staff", "weapon", "armor", "pedestal"))),
    }


def render_iteration(iteration: int = 1) -> dict:
    scene = bpy.context.scene
    folder = RENDERS / f"iter_{iteration:02d}"
    folder.mkdir(parents=True, exist_ok=True)
    view_layer = bpy.context.view_layer
    world_bg = _world_background(scene.world)
    original_bg = tuple(world_bg.inputs["Color"].default_value)
    original_strength = float(world_bg.inputs["Strength"].default_value)
    original_override = view_layer.material_override
    mapping = {
        "front": "QA_FRONT", "back": "QA_BACK", "left": "QA_LEFT",
        "front_3q": "QA_FRONT_3Q", "back_3q": "QA_BACK_3Q",
    }
    outputs = []
    for view, camera_name in mapping.items():
        scene.camera = bpy.data.objects[camera_name]
        view_layer.material_override = None
        world_bg.inputs["Color"].default_value = original_bg
        world_bg.inputs["Strength"].default_value = original_strength
        scene.render.filepath = str(folder / f"{view}_beauty.png")
        bpy.ops.render.render(write_still=True)
        outputs.append(scene.render.filepath)

        view_layer.material_override = bpy.data.materials["MAT_QA_Silhouette"]
        world_bg.inputs["Color"].default_value = (0.0, 0.0, 0.0, 1.0)
        world_bg.inputs["Strength"].default_value = 0.0
        scene.render.filepath = str(folder / f"{view}_silhouette.png")
        bpy.ops.render.render(write_still=True)
        outputs.append(scene.render.filepath)

    view_layer.material_override = original_override
    world_bg.inputs["Color"].default_value = original_bg
    world_bg.inputs["Strength"].default_value = original_strength
    return {"iteration": iteration, "outputs": outputs}


def finalize_iteration(iteration: int = 1, do_render: bool = True) -> dict:
    surface = stage_surface_detail()
    height = normalize_height()
    qa = stage_qa()
    audit = _accessory_audit()
    if len(audit["bells"]) != 3 or len(audit["cloth"]) != 2 or audit["forbidden_name_hits"]:
        raise RuntimeError(f"Accessory acceptance failed: {audit}")
    MASTER.parent.mkdir(parents=True, exist_ok=True)
    CHECKPOINTS.mkdir(parents=True, exist_ok=True)
    bpy.context.scene["qa_accessory_audit"] = json.dumps(audit)
    bpy.context.scene["lod0_triangles"] = _triangle_count()
    bpy.ops.wm.save_as_mainfile(filepath=str(MASTER))
    checkpoint = CHECKPOINTS / f"Leshy_v1_iter_{iteration:02d}.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(checkpoint))
    bpy.ops.wm.save_as_mainfile(filepath=str(MASTER))
    rendered = render_iteration(iteration) if do_render else None
    bpy.ops.wm.save_as_mainfile(filepath=str(MASTER))
    return {
        "master": str(MASTER), "checkpoint": str(checkpoint), "height": height,
        "triangles": _triangle_count(), "surface": surface, "accessories": audit, "qa": qa, "render": rendered,
    }


def scene_audit() -> dict:
    objects = _model_meshes()
    min_z = min((obj.matrix_world @ v.co).z for obj in objects for v in obj.data.vertices)
    max_z = max((obj.matrix_world @ v.co).z for obj in objects for v in obj.data.vertices)
    return {
        "file": bpy.data.filepath,
        "scene": bpy.context.scene.name,
        "mesh_objects": len(objects),
        "materials": sorted(m.name for m in bpy.data.materials if m.name.startswith("MAT_Leshy")),
        "height_m": max_z - min_z,
        "bounds_z": [min_z, max_z],
        "triangles": _triangle_count(),
        "accessories": _accessory_audit(),
        "cameras": sorted(o.name for o in bpy.context.scene.objects if o.type == "CAMERA"),
    }
