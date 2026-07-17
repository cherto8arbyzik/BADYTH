using Hollowwest.Gameplay;
using NUnit.Framework;

namespace Hollowwest.Tests
{

public sealed class DayNightCycleTests
{
    [Test]
    public void Cycle_UsesTwelveMinuteDaylightAndFiveMinuteNight()
    {
        DayNightCycle cycle = new(720f, 300f, 60f);

        Assert.That(cycle.DaylightDuration, Is.EqualTo(720f));
        Assert.That(cycle.NightDuration, Is.EqualTo(300f));
        Assert.That(cycle.TotalDuration, Is.EqualTo(1020f));
    }

    [Test]
    public void Cycle_VisitsDuskNightAndDawnInOrder()
    {
        DayNightCycle cycle = new(720f, 300f, 60f);
        cycle.SetPhase(GamePhase.Day);

        cycle.Tick(601f);
        Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Dusk));
        cycle.Tick(60f);
        Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Night));
        cycle.Tick(300f);
        Assert.That(cycle.Phase, Is.EqualTo(GamePhase.Dawn));
    }
}

}
