# Prototype architecture

## Boundaries

```text
SelectionController
        |
        v
FormationPlanner ----> NavigationAgent (one per unit)
                              |
                              v
                    INavigationService
                              |
                    GridNavigationService
                    [future AstarPathAdapter]
```

- `SelectionController` owns player intent: selection and move commands.
- `FormationPlanner` is stateless deterministic math. It creates slots and assigns each selected unit once.
- `NavigationAgent` owns only route following and lightweight neighbour separation.
- `INavigationService` prevents the rest of the game from depending on a particular pathfinding vendor.
- `GridNavigationService` is the prototype implementation. It uses an eight-neighbour A* grid and disallows diagonal corner cutting.
- `PrototypeBootstrap` creates the greybox at runtime so the scene contains no fragile object references or paid assets.

## Why the prototype does not import A* Pathfinding Project

The recommended Asset Store package is paid and must be acquired through the project owner's Unity account. Committing or redistributing it without a licence is not acceptable. The prototype instead validates controls, formation assignment, and navigation semantics through the interface that a licensed adapter will later implement.

## Replacement contract

A future adapter must satisfy `INavigationService.TryFindPath(start, destination, output)`:

- clear and refill the provided list;
- return `false` when no route can be produced;
- omit the starting point;
- return world-space waypoints in travel order;
- keep callers independent of vendor-specific graph or agent types.

## Performance budgets for the next milestone

- Target: 60 FPS on a mid-range Windows PC.
- Player agents in the slice: maximum 20.
- Concurrent simple enemies in the slice: maximum 100.
- No per-frame path recalculation; paths change only on commands or explicit invalidation.
- No runtime allocations in route following after a command is accepted.
- Profile before increasing agent counts.

## Source layout

```text
Assets/_Project/
  Scenes/Prototype.unity
  Scripts/
    Core/           contracts and pure planning code
    Navigation/     route planning and following
    Presentation/   greybox visuals and HUD
    Prototype/      scene composition
    Selection/      player input and commands
  Tests/EditMode/   deterministic unit tests
```

## Next decision gate

After Unity is installed, run the tests and play the prototype. If group motion remains readable through the two obstacle choke points, import the licensed A* package and implement an adapter spike on a separate branch. If motion is already unclear at six units, reduce unit radius and formation density before adding combat.
