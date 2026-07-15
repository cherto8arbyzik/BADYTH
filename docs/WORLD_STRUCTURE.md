# World structure

## Town: persistent home scene

`Town.unity` is the long-lived hub. It owns restored buildings, stored resources,
resident assignments, upgrades, story NPCs, and the entrance to expeditions.
The player directly controls only the main hero. Residents will run their own
schedules and will be assigned through building panels instead of RTS selection.

Current slice:

- manual development day/night switch;
- night wave reuses the existing defence logic;
- one directly controlled hero;
- twelve town buildings, including selectable ruins;
- wood collection and immediate building restoration.

Next town systems should be added in this order:

1. persistent `TownState` data independent of scene objects;
2. building definitions, levels, repair stages, and worker slots;
3. autonomous resident schedules and production;
4. town entrance / expedition preparation screen;
5. save/load and story state.

## Expedition: disposable semi-open region

`Expedition.unity` is the boundary for a generated run. One expedition loads one
seeded biome with several points of interest and free movement inside a finite
region. The region is discarded after extraction or defeat.

An expedition may read a snapshot of hero equipment and town bonuses. It may
return only an `ExpeditionResult` containing loot, injuries, rescued residents,
and story flags. It must not keep direct references to town scene objects.

This gives the player an open-region feeling without committing to one seamless,
permanent open world.
