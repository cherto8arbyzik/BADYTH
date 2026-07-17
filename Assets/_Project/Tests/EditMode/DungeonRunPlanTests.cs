using Hollowwest.Gameplay;
using NUnit.Framework;

namespace Hollowwest.Tests
{

public sealed class DungeonRunPlanTests
{
    [Test]
    public void FloorCount_AlwaysStaysBetweenThreeAndSix()
    {
        for (int seed = -25; seed <= 25; seed++)
        {
            Assert.That(new DungeonRunPlan(seed).FloorCount, Is.InRange(3, 6));
        }
    }

    [Test]
    public void FloorCount_IsDeterministicForSeed()
    {
        Assert.That(new DungeonRunPlan(9137).FloorCount, Is.EqualTo(new DungeonRunPlan(9137).FloorCount));
    }
}

}
