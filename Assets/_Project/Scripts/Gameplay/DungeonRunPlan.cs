namespace Hollowwest.Gameplay
{

/// <summary>
/// Deterministic run length: the same expedition seed reproduces the same
/// number of floors while every new run stays inside the intended 3–6 range.
/// </summary>
public sealed class DungeonRunPlan
{
    public DungeonRunPlan(int seed)
    {
        FloorCount = 3 + (seed & int.MaxValue) % 4;
    }

    public int FloorCount { get; }
}

}
