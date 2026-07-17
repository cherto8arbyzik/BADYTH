"""Build a safe static collider and navigation outline for Starting Map 3.0.

The Meshy render FBX contains roughly 1.63 million triangles and must never be
used directly as a MeshCollider.  This script detects the largest connected,
upward-facing top component and ray-samples it into a closed radial shell.  The
result is deliberately conservative at the island edge and contains no loose or
non-manifold geometry.

Run with Blender 5.2 LTS or newer::

    blender --background --python Tools/Art/build_starting_map3_collision.py

The source FBX and its textures are read-only inputs and are never modified.
"""

from __future__ import annotations

import json
import math
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree


ROOT = Path(__file__).resolve().parents[2]
SOURCE_DIR = (
    ROOT
    / "Assets/_Project/Resources/Models/Custom/Environment/StartingIsland/Map3/Source"
)
SOURCE_STEM = "Meshy_AI_Floating_Island_Grass_0717131253_texture"
SOURCE_FBX = SOURCE_DIR / f"{SOURCE_STEM}.fbx"

OUTPUT_DIR = (
    ROOT
    / "Assets/_Project/Resources/Models/Custom/Environment/StartingIsland/Map3/Collision"
)
OUTPUT_FBX = OUTPUT_DIR / "StartingMap3_Collision.fbx"
OUTPUT_JSON = OUTPUT_DIR / "StartingMap3_NavigationFootprint.json"
OUTPUT_BLEND = (
    ROOT
    / "ArtSource/Blender/Environment/StartingIsland/StartingMap3_Collision.blend"
)

# The render factory scales the longest source horizontal side to 280 Unity
# metres.  Expressing the safety inset in metres keeps this build contract
# independent from Meshy's normalised source units.
TARGET_LONGEST_HORIZONTAL_METRES = 280.0
EDGE_INSET_METRES = 3.0
COLLIDER_THICKNESS_METRES = 2.0

# Audited Map 3.0 top classification.  A largest-component pass rejects the
# isolated upward-facing triangles on the rocky underside.
TOP_MIN_CENTROID_Z = 0.30
TOP_MIN_NORMAL_Z = math.cos(math.radians(35.0))

# 128 angular samples are useful directly as a navigation contour.  Forty-eight
# radial rings produce 12,544 closed-shell triangles: comfortably inside the
# 8k-20k collision budget while retaining the visible macro relief.
ANGULAR_SEGMENTS = 128
RADIAL_RINGS = 48
EXPECTED_MIN_TRIANGLES = 8_000
EXPECTED_MAX_TRIANGLES = 20_000

# The source factory aligns this fractional source-bounds height with Unity y=0.
# Including the same value in JSON makes the footprint immediately usable from
# C# without duplicating an undocumented measurement.
SOURCE_GROUND_HEIGHT_FROM_BOTTOM = 0.926231


def require_source() -> None:
    if not SOURCE_FBX.is_file():
        raise FileNotFoundError(f"Missing Starting Map 3.0 FBX: {SOURCE_FBX}")


def clear_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_source() -> bpy.types.Object:
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"No mesh imported from {SOURCE_FBX}")

    source = max(meshes, key=lambda obj: len(obj.data.polygons))
    source.name = "StartingMap3_Source_SamplingOnly"
    if len(source.data.polygons) < 1_000_000:
        raise RuntimeError(
            "Unexpected Map 3.0 topology; refusing to build from the wrong FBX "
            f"({len(source.data.polygons):,} polygons)"
        )

    bpy.ops.object.select_all(action="DESELECT")
    source.select_set(True)
    bpy.context.view_layer.objects.active = source
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    if any(len(polygon.vertices) != 3 for polygon in source.data.polygons):
        triangulate = source.modifiers.new("Sampling Triangulation", "TRIANGULATE")
        bpy.ops.object.modifier_apply(modifier=triangulate.name)

    source.data.update(calc_edges=True)
    return source


def mesh_arrays(mesh: bpy.types.Mesh) -> tuple[np.ndarray, ...]:
    vertices = np.empty((len(mesh.vertices), 3), dtype=np.float64)
    mesh.vertices.foreach_get("co", vertices.ravel())

    normals = np.empty((len(mesh.polygons), 3), dtype=np.float64)
    centers = np.empty((len(mesh.polygons), 3), dtype=np.float64)
    loop_starts = np.empty(len(mesh.polygons), dtype=np.int64)
    loop_totals = np.empty(len(mesh.polygons), dtype=np.int32)
    loop_vertices = np.empty(len(mesh.loops), dtype=np.int64)
    mesh.polygons.foreach_get("normal", normals.ravel())
    mesh.polygons.foreach_get("center", centers.ravel())
    mesh.polygons.foreach_get("loop_start", loop_starts)
    mesh.polygons.foreach_get("loop_total", loop_totals)
    mesh.loops.foreach_get("vertex_index", loop_vertices)
    return vertices, normals, centers, loop_starts, loop_totals, loop_vertices


def triangle_vertices(
    face_indices: np.ndarray,
    loop_starts: np.ndarray,
    loop_totals: np.ndarray,
    loop_vertices: np.ndarray,
) -> np.ndarray:
    if np.any(loop_totals[face_indices] != 3):
        raise RuntimeError("The sampling mesh is not fully triangulated")
    starts = loop_starts[face_indices]
    return np.column_stack(
        (loop_vertices[starts], loop_vertices[starts + 1], loop_vertices[starts + 2])
    ).astype(np.int64, copy=False)


def encoded_edges(triangles: np.ndarray, vertex_count: int) -> tuple[np.ndarray, np.ndarray]:
    edge_vertices = np.concatenate(
        (
            triangles[:, (0, 1)],
            triangles[:, (1, 2)],
            triangles[:, (2, 0)],
        ),
        axis=0,
    )
    edge_vertices.sort(axis=1)
    keys = (
        edge_vertices[:, 0].astype(np.int64) * np.int64(vertex_count + 1)
        + edge_vertices[:, 1].astype(np.int64)
    )
    return keys, edge_vertices


def largest_connected_component(
    candidate_faces: np.ndarray,
    candidate_triangles: np.ndarray,
    vertex_count: int,
) -> tuple[np.ndarray, np.ndarray]:
    """Return source polygon indices and triangles for the largest edge component."""

    keys, _ = encoded_edges(candidate_triangles, vertex_count)
    owners = np.tile(np.arange(len(candidate_faces), dtype=np.int32), 3)
    order = np.argsort(keys, kind="mergesort")
    sorted_keys = keys[order]
    sorted_owners = owners[order]

    parent = np.arange(len(candidate_faces), dtype=np.int32)
    sizes = np.ones(len(candidate_faces), dtype=np.int32)

    def find(value: int) -> int:
        root = value
        while int(parent[root]) != root:
            root = int(parent[root])
        while value != root:
            next_value = int(parent[value])
            parent[value] = root
            value = next_value
        return root

    duplicate_positions = np.flatnonzero(sorted_keys[1:] == sorted_keys[:-1])
    for position in duplicate_positions:
        left = find(int(sorted_owners[position]))
        right = find(int(sorted_owners[position + 1]))
        if left == right:
            continue
        if int(sizes[left]) < int(sizes[right]):
            left, right = right, left
        parent[right] = left
        sizes[left] += sizes[right]

    roots = np.empty(len(candidate_faces), dtype=np.int32)
    for index in range(len(candidate_faces)):
        roots[index] = find(index)
    counts = np.bincount(roots, minlength=len(candidate_faces))
    largest_root = int(np.argmax(counts))
    selection = roots == largest_root
    return candidate_faces[selection], candidate_triangles[selection]


def find_top_component(
    mesh: bpy.types.Mesh,
    normals: np.ndarray,
    centers: np.ndarray,
    loop_starts: np.ndarray,
    loop_totals: np.ndarray,
    loop_vertices: np.ndarray,
) -> tuple[np.ndarray, np.ndarray]:
    candidates = np.flatnonzero(
        (centers[:, 2] >= TOP_MIN_CENTROID_Z)
        & (normals[:, 2] >= TOP_MIN_NORMAL_Z)
    ).astype(np.int64)
    if len(candidates) < 10_000:
        raise RuntimeError(f"Unexpectedly small Map 3.0 top mask: {len(candidates):,}")

    candidate_triangles = triangle_vertices(
        candidates, loop_starts, loop_totals, loop_vertices
    )
    top_faces, top_triangles = largest_connected_component(
        candidates, candidate_triangles, len(mesh.vertices)
    )
    if len(top_faces) < 10_000:
        raise RuntimeError(
            f"Unexpectedly small largest Map 3.0 top component: {len(top_faces):,}"
        )
    return top_faces, top_triangles


def boundary_vertices(top_triangles: np.ndarray, vertex_count: int) -> np.ndarray:
    keys, edges = encoded_edges(top_triangles, vertex_count)
    order = np.argsort(keys, kind="mergesort")
    sorted_keys = keys[order]
    sorted_edges = edges[order]
    _, first, counts = np.unique(sorted_keys, return_index=True, return_counts=True)
    boundary = sorted_edges[first[counts == 1]]
    if len(boundary) < ANGULAR_SEGMENTS:
        raise RuntimeError(f"Top boundary is unexpectedly sparse: {len(boundary):,} edges")
    return np.unique(boundary.ravel())


def circular_fill(values: np.ndarray) -> np.ndarray:
    valid = np.flatnonzero(np.isfinite(values))
    if len(valid) < ANGULAR_SEGMENTS // 2:
        raise RuntimeError("Too many missing angular boundary samples")
    extended_x = np.concatenate((valid - len(values), valid, valid + len(values)))
    extended_y = np.concatenate((values[valid], values[valid], values[valid]))
    return np.interp(np.arange(len(values)), extended_x, extended_y)


def radial_profile(
    vertices: np.ndarray,
    centers: np.ndarray,
    top_faces: np.ndarray,
    boundary_indices: np.ndarray,
    source_scale: float,
) -> tuple[Vector, np.ndarray]:
    top_centers = centers[top_faces, :2]
    minimum = top_centers.min(axis=0)
    maximum = top_centers.max(axis=0)
    center_xy = (minimum + maximum) * 0.5

    boundary_xy = vertices[boundary_indices, :2]
    offsets = boundary_xy - center_xy
    angles = np.mod(np.arctan2(offsets[:, 1], offsets[:, 0]), math.tau)
    radii = np.linalg.norm(offsets, axis=1)
    bins = np.floor(angles / math.tau * ANGULAR_SEGMENTS).astype(np.int32)
    bins = np.clip(bins, 0, ANGULAR_SEGMENTS - 1)

    raw = np.full(ANGULAR_SEGMENTS, -np.inf, dtype=np.float64)
    np.maximum.at(raw, bins, radii)
    raw[raw == -np.inf] = np.nan
    raw = circular_fill(raw)

    # Keep the measured outer radius here.  The exact boundary at each segment
    # angle is resolved against the source BVH below; smoothing a concave outline
    # before that test can accidentally move the guessed edge outside the top.
    profile = raw
    minimum_radius = (8.0 + EDGE_INSET_METRES) / source_scale
    if np.any(profile <= minimum_radius):
        raise RuntimeError("The requested edge inset collapses the Map 3.0 footprint")
    return Vector((float(center_xy[0]), float(center_xy[1]))), profile


def create_source_bvh(source: bpy.types.Object) -> BVHTree:
    tree = BVHTree.FromObject(source, bpy.context.evaluated_depsgraph_get())
    if tree is None:
        raise RuntimeError("Could not create a BVH from Starting Map 3.0")
    return tree


def raycast_top(
    tree: BVHTree,
    x: float,
    y: float,
    ray_origin_z: float,
    ray_distance: float,
) -> tuple[Vector | None, Vector | None, int | None]:
    location, normal, polygon_index, _ = tree.ray_cast(
        Vector((x, y, ray_origin_z)), Vector((0.0, 0.0, -1.0)), ray_distance
    )
    return location, normal, polygon_index


def validate_boundary_profile(
    tree: BVHTree,
    center: Vector,
    profile: np.ndarray,
    top_face_lookup: np.ndarray,
    ray_origin_z: float,
    ray_distance: float,
    source_scale: float,
) -> np.ndarray:
    validated = profile.copy()
    shrink_step = 0.25 / source_scale
    inset_source = EDGE_INSET_METRES / source_scale
    search_lead = 1.0 / source_scale
    maximum_edge_search = 40.0 / source_scale
    maximum_post_inset_adjustment = 1.0 / source_scale
    for index in range(ANGULAR_SEGMENTS):
        angle = math.tau * index / ANGULAR_SEGMENTS
        search_inset = 0.0
        outer_radius = None
        while search_inset <= maximum_edge_search + 1e-9:
            radius = validated[index] + search_lead - search_inset
            x = center.x + math.cos(angle) * radius
            y = center.y + math.sin(angle) * radius
            location, normal, polygon_index = raycast_top(
                tree, x, y, ray_origin_z, ray_distance
            )
            if (
                location is not None
                and normal is not None
                and polygon_index is not None
                and 0 <= polygon_index < len(top_face_lookup)
                and bool(top_face_lookup[polygon_index])
                and location.z >= TOP_MIN_CENTROID_Z - 0.03
                and normal.z >= TOP_MIN_NORMAL_Z - 0.05
            ):
                outer_radius = radius
                break
            search_inset += shrink_step
        if outer_radius is None:
            raise RuntimeError(
                f"Could not find a safe top boundary sample at angular index {index}"
            )

        # The final contour is 3m inside the detected top edge.  Permit at most
        # one additional metre if a tiny steep source triangle sits exactly on
        # the requested point; this keeps the promised 2-4m Unity safety band.
        target_radius = outer_radius - inset_source
        post_adjustment = 0.0
        while post_adjustment <= maximum_post_inset_adjustment + 1e-9:
            radius = target_radius - post_adjustment
            x = center.x + math.cos(angle) * radius
            y = center.y + math.sin(angle) * radius
            location, normal, _ = raycast_top(tree, x, y, ray_origin_z, ray_distance)
            if (
                location is not None
                and normal is not None
                and location.z >= TOP_MIN_CENTROID_Z - 0.05
                and normal.z >= 0.20
            ):
                validated[index] = radius
                break
            post_adjustment += shrink_step
        else:
            raise RuntimeError(
                f"The 3-4m inset does not land on the top at angular index {index}"
            )
    return validated


def sample_collision_vertices(
    tree: BVHTree,
    center: Vector,
    profile: np.ndarray,
    ray_origin_z: float,
    ray_distance: float,
    source_scale: float,
) -> tuple[list[tuple[float, float, float]], int]:
    vertices: list[tuple[float, float, float]] = []
    fallback_count = 0

    center_hit, _, _ = raycast_top(
        tree, center.x, center.y, ray_origin_z, ray_distance
    )
    if center_hit is None or center_hit.z < TOP_MIN_CENTROID_Z - 0.05:
        raise RuntimeError("The radial footprint centre is not on the Map 3.0 top")
    vertices.append((center.x, center.y, center_hit.z))

    previous_ring_heights = np.full(ANGULAR_SEGMENTS, center_hit.z, dtype=np.float64)
    for ring in range(1, RADIAL_RINGS + 1):
        fraction = ring / RADIAL_RINGS
        ring_heights = np.empty(ANGULAR_SEGMENTS, dtype=np.float64)
        for segment in range(ANGULAR_SEGMENTS):
            angle = math.tau * segment / ANGULAR_SEGMENTS
            radius = profile[segment] * fraction
            x = center.x + math.cos(angle) * radius
            y = center.y + math.sin(angle) * radius
            location, _, _ = raycast_top(tree, x, y, ray_origin_z, ray_distance)
            if location is None or location.z < TOP_MIN_CENTROID_Z - 0.05:
                # The footprint is star-shaped and boundary-validated, so this
                # can only be a tiny source hole.  Continuing the previous ring
                # height avoids creating a physics pinhole or a vertical spike.
                z = float(previous_ring_heights[segment])
                fallback_count += 1
            else:
                z = float(location.z)
            ring_heights[segment] = z
            vertices.append((x, y, z))
        previous_ring_heights = ring_heights

    # More than one percent fallbacks indicates a non-star-shaped source or an
    # orientation mismatch and should fail loudly instead of hiding bad physics.
    if fallback_count > ANGULAR_SEGMENTS * RADIAL_RINGS // 100:
        raise RuntimeError(f"Too many collision raycast fallbacks: {fallback_count}")
    return vertices, fallback_count


def ring_vertex(ring: int, segment: int) -> int:
    if ring == 0:
        return 0
    return 1 + (ring - 1) * ANGULAR_SEGMENTS + (segment % ANGULAR_SEGMENTS)


def build_closed_collision_mesh(
    top_vertices: list[tuple[float, float, float]],
    source_scale: float,
) -> bpy.types.Object:
    vertices = list(top_vertices)
    faces: list[tuple[int, int, int]] = []

    for segment in range(ANGULAR_SEGMENTS):
        faces.append(
            (0, ring_vertex(1, segment), ring_vertex(1, segment + 1))
        )

    for ring in range(1, RADIAL_RINGS):
        for segment in range(ANGULAR_SEGMENTS):
            inner_a = ring_vertex(ring, segment)
            inner_b = ring_vertex(ring, segment + 1)
            outer_a = ring_vertex(ring + 1, segment)
            outer_b = ring_vertex(ring + 1, segment + 1)
            faces.append((inner_a, outer_a, outer_b))
            faces.append((inner_a, outer_b, inner_b))

    boundary_top = [ring_vertex(RADIAL_RINGS, index) for index in range(ANGULAR_SEGMENTS)]
    bottom_z = min(vertex[2] for vertex in top_vertices) - (
        COLLIDER_THICKNESS_METRES / source_scale
    )
    boundary_bottom: list[int] = []
    for top_index in boundary_top:
        x, y, _ = vertices[top_index]
        boundary_bottom.append(len(vertices))
        vertices.append((x, y, bottom_z))
    bottom_center = len(vertices)
    center_x = sum(vertices[index][0] for index in boundary_bottom) / ANGULAR_SEGMENTS
    center_y = sum(vertices[index][1] for index in boundary_bottom) / ANGULAR_SEGMENTS
    vertices.append((center_x, center_y, bottom_z))

    for segment in range(ANGULAR_SEGMENTS):
        next_segment = (segment + 1) % ANGULAR_SEGMENTS
        top_a = boundary_top[segment]
        top_b = boundary_top[next_segment]
        bottom_a = boundary_bottom[segment]
        bottom_b = boundary_bottom[next_segment]
        faces.append((top_a, bottom_a, top_b))
        faces.append((top_b, bottom_a, bottom_b))
        faces.append((bottom_center, bottom_b, bottom_a))

    mesh = bpy.data.meshes.new("StartingMap3_Collision_Mesh")
    mesh.from_pydata(vertices, (), faces)
    mesh.validate(clean_customdata=True)
    mesh.update(calc_edges=True)

    collision = bpy.data.objects.new("StartingMap3_Collision", mesh)
    bpy.context.collection.objects.link(collision)
    collision["collision_kind"] = "static_non_convex"
    collision["edge_inset_metres"] = EDGE_INSET_METRES
    collision["runtime_scale_metres_per_source_unit"] = source_scale
    collision["source_asset"] = SOURCE_FBX.name

    bmesh_data = bmesh.new()
    bmesh_data.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bmesh_data, faces=list(bmesh_data.faces))
    bmesh_data.to_mesh(mesh)
    bmesh_data.free()
    mesh.update(calc_edges=True)
    return collision


def world_bounds(obj: bpy.types.Object) -> tuple[np.ndarray, np.ndarray]:
    points = np.array(
        [tuple(obj.matrix_world @ vertex.co) for vertex in obj.data.vertices],
        dtype=np.float64,
    )
    return points.min(axis=0), points.max(axis=0)


def topology_report(obj: bpy.types.Object) -> dict[str, int]:
    mesh = obj.data
    mesh.calc_loop_triangles()
    bmesh_data = bmesh.new()
    bmesh_data.from_mesh(mesh)
    non_manifold_edges = sum(
        1 for edge in bmesh_data.edges if len(edge.link_faces) != 2
    )
    loose_vertices = sum(1 for vertex in bmesh_data.verts if not vertex.link_edges)
    degenerate_faces = sum(1 for face in bmesh_data.faces if face.calc_area() <= 1e-12)
    report = {
        "vertices": len(mesh.vertices),
        "triangles": len(mesh.loop_triangles),
        "nonManifoldEdges": non_manifold_edges,
        "looseVertices": loose_vertices,
        "degenerateFaces": degenerate_faces,
    }
    bmesh_data.free()
    return report


def export_collision(collision: bpy.types.Object) -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    collision.select_set(True)
    bpy.context.view_layer.objects.active = collision
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_FBX),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_custom_props=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )
    bpy.ops.object.select_all(action="DESELECT")


def write_footprint_json(
    collision: bpy.types.Object,
    profile: np.ndarray,
    source_center: Vector,
    source_bounds_min: np.ndarray,
    source_bounds_max: np.ndarray,
    source_scale: float,
    top_face_count: int,
    fallback_count: int,
    report: dict[str, int],
) -> None:
    source_bounds_center = (source_bounds_min + source_bounds_max) * 0.5
    source_ground_z = source_bounds_min[2] + (
        source_bounds_max[2] - source_bounds_min[2]
    ) * SOURCE_GROUND_HEIGHT_FROM_BOTTOM

    source_points = []
    unity_points = []
    for segment in range(ANGULAR_SEGMENTS):
        vertex_index = ring_vertex(RADIAL_RINGS, segment)
        point = collision.data.vertices[vertex_index].co
        source_points.append(
            {"x": round(point.x, 8), "y": round(point.y, 8), "z": round(point.z, 8)}
        )
        unity_points.append(
            {
                "x": round((point.x - source_bounds_center[0]) * source_scale, 4),
                "y": round((point.z - source_ground_z) * source_scale, 4),
                "z": round((point.y - source_bounds_center[1]) * source_scale, 4),
            }
        )

    collision_min, collision_max = world_bounds(collision)
    payload = {
        "schemaVersion": 1,
        "sourceAsset": str(SOURCE_FBX.relative_to(ROOT)).replace("\\", "/"),
        "coordinateContract": {
            "fbx": "Blender Z-up exported as FBX -Z forward, Y up",
            "sourcePoints": "Blender source-local x/y/z",
            "unityLocalPointsMetres": (
                "x=(source.x-sourceBounds.center.x)*scale; "
                "y=(source.z-sourceGroundZ)*scale; "
                "z=(source.y-sourceBounds.center.y)*scale"
            ),
            "winding": "counter-clockwise viewed from above",
        },
        "runtimeContract": {
            "targetLongestHorizontalSizeMetres": TARGET_LONGEST_HORIZONTAL_METRES,
            "metresPerSourceUnit": round(source_scale, 8),
            "edgeInsetMetres": EDGE_INSET_METRES,
            "colliderThicknessMetres": COLLIDER_THICKNESS_METRES,
            "sourceGroundHeightFromBottom": SOURCE_GROUND_HEIGHT_FROM_BOTTOM,
            "sourceGroundZ": round(float(source_ground_z), 8),
        },
        "audit": {
            "topMinimumCentroidZ": TOP_MIN_CENTROID_Z,
            "topMinimumNormalZ": round(TOP_MIN_NORMAL_Z, 8),
            "largestTopComponentTriangles": top_face_count,
            "raycastFallbackSamples": fallback_count,
            **report,
            "closedManifold": report["nonManifoldEdges"] == 0,
        },
        "sourceBounds": {
            "min": [round(float(value), 8) for value in source_bounds_min],
            "max": [round(float(value), 8) for value in source_bounds_max],
        },
        "collisionBoundsSource": {
            "min": [round(float(value), 8) for value in collision_min],
            "max": [round(float(value), 8) for value in collision_max],
        },
        "footprint": {
            "pointCount": ANGULAR_SEGMENTS,
            "radialOriginSource": {
                "x": round(source_center.x, 8),
                "y": round(source_center.y, 8),
            },
            "sourcePoints": source_points,
            "unityLocalPointsMetres": unity_points,
        },
    }
    OUTPUT_JSON.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )


def save_master(collision: bpy.types.Object, source: bpy.types.Object) -> None:
    # Keeping the 135 MB render source inside the .blend would duplicate user
    # content.  The generator is the reproducible link; the master contains only
    # the compact derived collider plus explicit source metadata.
    bpy.data.objects.remove(source, do_unlink=True)
    for obj in list(bpy.context.scene.objects):
        if obj.type == "MESH" and obj != collision:
            bpy.data.objects.remove(obj, do_unlink=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene["generated_by"] = "Tools/Art/build_starting_map3_collision.py"
    scene["source_fbx"] = str(SOURCE_FBX.relative_to(ROOT)).replace("\\", "/")
    OUTPUT_BLEND.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND), check_existing=False)


def verify_export(expected_min: np.ndarray, expected_max: np.ndarray) -> dict[str, int]:
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(OUTPUT_FBX))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if len(meshes) != 1:
        raise RuntimeError(f"Expected one collision mesh after re-import, found {len(meshes)}")
    collision = meshes[0]
    bpy.ops.object.select_all(action="DESELECT")
    collision.select_set(True)
    bpy.context.view_layer.objects.active = collision
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    report = topology_report(collision)
    if not EXPECTED_MIN_TRIANGLES <= report["triangles"] <= EXPECTED_MAX_TRIANGLES:
        raise RuntimeError(f"Collision triangle budget failed: {report}")
    if report["nonManifoldEdges"] or report["looseVertices"] or report["degenerateFaces"]:
        raise RuntimeError(f"Collision topology verification failed: {report}")

    imported_min, imported_max = world_bounds(collision)
    maximum_bound_error = float(
        max(np.max(np.abs(imported_min - expected_min)), np.max(np.abs(imported_max - expected_max)))
    )
    if maximum_bound_error > 1e-4:
        raise RuntimeError(
            f"FBX orientation/bounds changed after re-import ({maximum_bound_error:.8f})"
        )

    top_normals = [
        polygon.normal.z
        for polygon in collision.data.polygons
        if polygon.center.z > imported_max[2] - (imported_max[2] - imported_min[2]) * 0.75
    ]
    if not top_normals or sum(top_normals) / len(top_normals) <= 0.5:
        raise RuntimeError("Collision top faces are not oriented upward after FBX re-import")
    return report


def build() -> None:
    require_source()
    clear_scene()
    source = import_source()
    mesh = source.data
    source_polygon_count = len(mesh.polygons)
    vertices, normals, centers, loop_starts, loop_totals, loop_vertices = mesh_arrays(mesh)
    source_bounds_min = vertices.min(axis=0)
    source_bounds_max = vertices.max(axis=0)
    source_longest_horizontal = max(
        source_bounds_max[0] - source_bounds_min[0],
        source_bounds_max[1] - source_bounds_min[1],
    )
    source_scale = TARGET_LONGEST_HORIZONTAL_METRES / source_longest_horizontal

    top_faces, top_triangles = find_top_component(
        mesh, normals, centers, loop_starts, loop_totals, loop_vertices
    )
    boundary = boundary_vertices(top_triangles, len(mesh.vertices))
    center, profile = radial_profile(
        vertices, centers, top_faces, boundary, source_scale
    )

    tree = create_source_bvh(source)
    source_height = source_bounds_max[2] - source_bounds_min[2]
    ray_origin_z = float(source_bounds_max[2] + max(1.0, source_height))
    ray_distance = max(4.0, float(source_height * 4.0))
    top_face_lookup = np.zeros(len(mesh.polygons), dtype=np.bool_)
    top_face_lookup[top_faces] = True
    profile = validate_boundary_profile(
        tree,
        center,
        profile,
        top_face_lookup,
        ray_origin_z,
        ray_distance,
        source_scale,
    )
    top_vertices, fallback_count = sample_collision_vertices(
        tree,
        center,
        profile,
        ray_origin_z,
        ray_distance,
        source_scale,
    )
    collision = build_closed_collision_mesh(top_vertices, source_scale)
    report = topology_report(collision)
    if report["nonManifoldEdges"] or report["looseVertices"] or report["degenerateFaces"]:
        raise RuntimeError(f"Generated collision topology failed: {report}")
    if not EXPECTED_MIN_TRIANGLES <= report["triangles"] <= EXPECTED_MAX_TRIANGLES:
        raise RuntimeError(f"Generated collision triangle budget failed: {report}")

    collision_min, collision_max = world_bounds(collision)
    export_collision(collision)
    write_footprint_json(
        collision,
        profile,
        center,
        source_bounds_min,
        source_bounds_max,
        source_scale,
        len(top_faces),
        fallback_count,
        report,
    )
    save_master(collision, source)
    verified = verify_export(collision_min, collision_max)

    print(
        "STARTING_MAP3_COLLISION_OK",
        json.dumps(
            {
                "sourcePolygons": source_polygon_count,
                "largestTopComponent": len(top_faces),
                "metresPerSourceUnit": round(float(source_scale), 6),
                "edgeInsetMetres": EDGE_INSET_METRES,
                "fallbackSamples": fallback_count,
                **verified,
            },
            sort_keys=True,
        ),
    )


if __name__ == "__main__":
    build()
