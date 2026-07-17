# Leshy v1 — reference manifest

All working references are lossless copies. The attachment sources and the files in this directory are unchanged at the pixel level.

| Original source | Assigned view | Working copy | Resolution | SHA-256 |
|---|---|---|---:|---|
| `642d0633-b83d-4573-8ed7-d8e0562b9fd2/image-1.png` (identical to current attachment `afbace36-46cc-446f-a300-e80e6c7eb66f/image-1.png`) | Canonical front | `leshy_front.png` | 1024×1536 | `DD6FE358A66D25BEB578EB8A15E848C8C84EE34DA9239945C14003BD67E90959` |
| `642d0633-b83d-4573-8ed7-d8e0562b9fd2/image-2.png` (identical to current attachment `afbace36-46cc-446f-a300-e80e6c7eb66f/image-2.png`) | Strict left | `leshy_left.png` | 1024×1536 | `37A4FBD13FD6F9D4E70497AD196637587A03E977F9FBA360B1ECE2D25E2F66B0` |
| `642d0633-b83d-4573-8ed7-d8e0562b9fd2/image-3.png` (identical to current attachment `afbace36-46cc-446f-a300-e80e6c7eb66f/image-3.png`) | Strict back | `leshy_back.png` | 1024×1536 | `EF6FFC58F3FFDAD5EA4DE2C1FBF368609118A085CE65703BDA938BC384A7A781` |
| `642d0633-b83d-4573-8ed7-d8e0562b9fd2/image-4.png` (identical to current attachment `afbace36-46cc-446f-a300-e80e6c7eb66f/image-4.png`) | Front three-quarter | `leshy_front_3q.png` | 1024×1536 | `A0771394DB2FC76186EC174786D3C6D8D96500CC56975C1B31FB15A22A1ECFD3` |
| `642d0633-b83d-4573-8ed7-d8e0562b9fd2/image-5.png` | Back three-quarter | `leshy_back_3q.png` | 1024×1536 | `E93BCCDE347240FEFE3AA831715942D474F8EF025EA1B5835B7FD7E623C4D347` |

## View authority

- `leshy_front.png` is authoritative for identity, total silhouette, crown width, mask proportions, arm length, hand/root spread, accessory count, and accessory side.
- `leshy_back.png` and `leshy_left.png` define hidden geometry and body depth.
- The two three-quarter views validate volume and layering only; they do not override the canonical front silhouette.

## Visible details and conflicts

- The top branch is the 3.2 m height point; the root soles sit at Z=0.
- The crown is wide and asymmetric, with a dominant horizontal fork and numerous thin bare tips. Thin tips must remain visible in silhouette renders.
- The mask is narrow, vertically layered birch bark over a physically recessed black face cavity. The eyes are tiny and dim amber.
- Arms are unusually long; hands end in separated root fingers. Feet are broad, branched root structures rather than shoes or a fused stump.
- Moss is dark olive and volumetric. Hanging roots/vines form an irregular mantle without hiding the mask.
- Exactly three copper bells and exactly two cloth ribbons hang from the character's left crown branch (viewer-right in the front reference). The ribbons are dirty burgundy and bone-white.
- Some dangling strands merge into the grey background and differ slightly between views. Their count is not treated as fixed, but they may not read as extra jewelry, weapons, or staffs.
- Perspective and subject scale vary slightly across source images. QA cameras therefore use equal orthographic framing and compare normalized silhouettes/keypoints.
- Forbidden additions: weapon, staff, armor, pedestal, extra bells, extra ribbons, or decorative jewelry not supported by the references.
