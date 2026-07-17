# ZASTAVA

Technical prototype for a small-scale single-player RTS / colony-survival game set on a damaged Slavic frontier outpost.

The current milestone presents one directly controlled hero, a damaged starting shrine, a larger ruined village, gathering, building restoration, and a manually triggered day/night defence loop.

## Open the prototype

1. Install Unity `6000.0.73f1` through Unity Hub.
2. Add this repository as a project and open it.
3. Open `Assets/_Project/Scenes/Town.unity`.
4. Press **Play**.

The first import can take several minutes while Unity resolves the Test Framework package.

## Controls

- Left click: select one unit.
- Left drag: box-select units.
- Right click ground: move the hero.
- Right click a wood cache: the hero gathers wood.
- Click a ruined building: open its restoration panel.
- **DAY**: return to the safe development state.
- **NIGHT + WAVE**: switch lighting and launch the current enemy wave.
- WASD / arrow keys: pan the camera.
- Mouse wheel: zoom.
- H / F1: show or hide the compact controls panel.
- Escape: clear selection.

## Tests

In Unity, open **Window > General > Test Runner**, choose **EditMode**, and run all tests. Tests cover route finding, blocked routes, formation layout, and unique slot assignment.

## Project status

This is a deliberately narrow technical slice, not a content-complete game. It contains:

- the canonical world, story, and character bible in [`docs/ЗАСТАВА.md`](docs/ЗАСТАВА.md);
- the production-facing character art and modular profession guide in [`docs/CHARACTER_REFERENCE_BIBLE.md`](docs/CHARACTER_REFERENCE_BIBLE.md);
- a one-page product definition in `docs/GDD.md`;
- architecture and replacement boundaries in `docs/ARCHITECTURE.md`;
- separate `Town` and placeholder `Expedition` scenes;
- a code-generated stylized 3D town roughly ten times the old playable area;
- Quaternius CC0 Medieval Village buildings plus a curated Ultimate Stylized Nature subset;
- one directly controlled hero;
- selectable ruined buildings with wood restoration costs;
- hero selection, movement, combat, and wood gathering;
- a lightweight grid A* implementation with diagonal corner protection;
- basic local separation between moving units;
- a first resource counter for the day-raid loop;
- manual day/night development controls, camp core, one night wave, and simple auto-combat;
- Edit Mode tests for the deterministic planning code.

The navigation code implements `INavigationService`. When A* Pathfinding Project Pro is licensed and imported, it can be added behind that interface without rewriting selection, commands, or unit presentation.

## Third-party assets

Building models come from the [Quaternius Medieval Village Pack](https://quaternius.com/packs/medievalvillage.html). Trees and bushes come from the [Quaternius Ultimate Stylized Nature Pack](https://quaternius.com/packs/ultimatestylizednature.html). Both are licensed under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/), and local source notices are stored beside the imported files.

Large art sources (`FBX`, `BLEND`, `GLB`, `PSD`, `TGA`, `EXR`) and the selected nature textures are tracked through Git LFS. Every imported third-party pack must include its source URL and redistribution licence; paid assets that cannot be redistributed must not be pushed to this public repository.
