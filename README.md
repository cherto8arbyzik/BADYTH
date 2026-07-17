# ZASTAVA

Unity 6 project reset to a clean environment baseline.

## Current state

The only active Unity scene is `Assets/_Project/Scenes/Prototype.unity`. It contains:

- Starting Map 3.0 with its original PBR textures;
- a separate optimized static collision mesh;
- one camera and one directional light.

There are no gameplay systems, characters, buildings, UI, procedural props, or runtime bootstrap scripts in the active project. The removed prototype is documented in [`docs/PROTOTYPE_ARCHIVE.md`](docs/PROTOTYPE_ARCHIVE.md) and remains recoverable from Git history.

## Open the scene

1. Install Unity `6000.0.73f1` through Unity Hub.
2. Add this repository as a project and open it.
3. Open `Assets/_Project/Scenes/Prototype.unity`.
4. Use Scene view or press **Play** to inspect the island.

The render mesh is intentionally not used for physics. Collision is provided by the dedicated 12,544-triangle static non-convex mesh under the same transform as the visual model.

## Source and licensing

Starting Map 3.0 provenance and file hashes are recorded in [`docs/STARTING_MAP_3_PROVENANCE.md`](docs/STARTING_MAP_3_PROVENANCE.md). Large binary assets are tracked through Git LFS.
