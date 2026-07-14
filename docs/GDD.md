# HOLLOWWEST — one-page GDD

## Promise

Lead a small frontier posse through a cursed expedition: scout and prepare by day, then survive a supernatural siege by night. Decisions and positioning matter more than actions per minute.

## Product

- Genre: single-player roguelite real-time tactics with light camp defence.
- Platform: Windows PC / Steam first.
- Business model: premium, target price USD 15–18; no F2P and no competitive PvP.
- Run length: target 25–40 minutes.
- Combat scale: 6–20 controllable units and dozens, not thousands, of enemies.
- Tone: weird-west frontier, restrained occult horror, readable stylised visuals.

## Core loop

1. Choose the next location on the expedition route.
2. During daylight, explore, gather supplies, recruit or reposition the posse, and prepare the camp.
3. Draft one meaningful relic or tactical upgrade.
4. At night, defend the camp against escalating enemy waves.
5. Survive to move deeper into the frontier; fail to end the run and retain limited unlock progress.

## Design pillars

1. **Small squad, meaningful space.** Facing, cover lanes, formation, and timing outweigh rapid clicking.
2. **Day creates the night problem.** Exploration choices determine the shape and difficulty of the defence.
3. **Short runs, strong variation.** Route, map obstacles, encounters, recruits, and relic combinations change each expedition.
4. **Clarity before quantity.** Distinct silhouettes, immediate feedback, and a polished small roster beat a large noisy roster.

## Vertical-slice content ceiling

- One greybox biome.
- Six controllable units using one shared prototype role.
- One camp objective.
- One enemy archetype and three short waves.
- Six draftable relics.
- One complete day/night cycle with victory, defeat, and restart.

Anything beyond this list waits until the movement-and-command prototype is fun and reliable.

## Current milestone: movement proof

The player can select one or more units and right-click a destination. Units receive unique formation slots, find routes around static obstacles, avoid cutting blocked diagonal corners, and apply light local separation while moving.

Acceptance criteria:

- Six units can cross the greybox without entering obstacles.
- Every selected unit receives a unique destination.
- A wall with a valid opening is navigated; a sealed wall returns no route.
- Selection and commands are understandable without a tutorial.
- The planning logic is covered by deterministic Edit Mode tests.

## Explicit non-goals for this milestone

Combat, enemies, procedural map generation, economy, save data, meta progression, final art, audio, multiplayer, and Asset Store navigation integration.
