# HOLLOWWEST

Technical prototype for a small-scale single-player roguelite RTS / real-time tactics game set in a weird-west occult frontier.

The current milestone proves the riskiest interaction: select a squad, issue a formation move, and route every unit around static obstacles on a grid.

## Open the prototype

1. Install Unity `6000.0.73f1` through Unity Hub.
2. Add this repository as a project and open it.
3. Open `Assets/_Project/Scenes/Prototype.unity`.
4. Press **Play**.

The first import can take several minutes while Unity resolves the Test Framework package.

## Controls

- Left click: select one unit.
- Left drag: box-select units.
- Shift + selection: add to the current selection.
- Right click ground: move selected units in formation.
- Right click a wood cache: selected units gather wood.
- Survive the first night wave after the day timer expires.
- WASD / arrow keys: pan the camera.
- Mouse wheel: zoom.
- Escape: clear selection.

## Tests

In Unity, open **Window > General > Test Runner**, choose **EditMode**, and run all tests. Tests cover route finding, blocked routes, formation layout, and unique slot assignment.

## Project status

This is a deliberately narrow technical slice, not a content-complete game. It contains:

- a one-page product definition in `docs/GDD.md`;
- architecture and replacement boundaries in `docs/ARCHITECTURE.md`;
- a code-generated greybox scene with a ruined starting base;
- a visually distinct main hero and subordinate pawns;
- box selection, formation commands, and wood gathering;
- a lightweight grid A* implementation with diagonal corner protection;
- basic local separation between moving units;
- a first resource counter for the day-raid loop;
- a timed day phase, camp core, one night wave, and simple auto-combat;
- Edit Mode tests for the deterministic planning code.

The navigation code implements `INavigationService`. When A* Pathfinding Project Pro is licensed and imported, it can be added behind that interface without rewriting selection, commands, or unit presentation.
