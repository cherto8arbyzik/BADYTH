using System;

namespace Hollowwest.Gameplay
{

[Flags]
public enum DiscoveryType
{
    None = 0,
    AncientBlueprints = 1 << 0,
    GrainSeeds = 1 << 1,
    SacredRelic = 1 << 2,
    WardStone = 1 << 3,
    SkyGlass = 1 << 4
}
}
