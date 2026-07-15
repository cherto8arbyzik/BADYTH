# Art pipeline

## Repository policy

- Small redistributable runtime assets may live directly in Git.
- Large binary sources are tracked through Git LFS.
- The public repository may contain only assets whose licence permits redistribution.
- Every external pack needs a local licence notice and source URL.
- Paid marketplace packs stay out of the public repository unless their licence explicitly allows source redistribution.

## Why the current picture still looks rough

The current houses are coherent low-poly models, and the first licensed nature
subset now replaces the old sphere trees. Model detail is still only one part of
final image quality. The weakest layers remain the procedural ground, placeholder
hero, missing animation, flat materials, no terrain blending/decals, and no
post-processing stack.

Higher-detail buildings alone would make those differences more visible rather
than solve the whole frame. The recommended visual order is:

1. terrain, nature kit, paths, decals, and ground variation;
2. proper hero and resident character set with animations;
3. upgraded building kit and staged ruin/restoration variants;
4. URP lighting, colour grading, ambient occlusion, and fog tuning;
5. VFX, weather, props, and final polish.
