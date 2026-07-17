using UnityEngine;

namespace Hollowwest.Gameplay
{

/// <summary>
/// Pure clock logic kept outside MonoBehaviour so the full cycle can be tested
/// without waiting seventeen real minutes in Play Mode.
/// </summary>
public sealed class DayNightCycle
{
    private readonly float _dawnDuration;
    private readonly float _dayDuration;
    private readonly float _duskDuration;
    private readonly float _nightDuration;
    private float _elapsed;

    public DayNightCycle(
        float daylightSeconds = 720f,
        float nightSeconds = 300f,
        float transitionSeconds = 60f)
    {
        float safeDaylight = Mathf.Max(30f, daylightSeconds);
        float safeTransition = Mathf.Min(Mathf.Max(5f, transitionSeconds), safeDaylight * 0.25f);
        _dawnDuration = safeTransition;
        _duskDuration = safeTransition;
        _dayDuration = safeDaylight - _dawnDuration - _duskDuration;
        _nightDuration = Mathf.Max(15f, nightSeconds);
        SetPhase(GamePhase.Dawn);
    }

    public float TotalDuration => _dawnDuration + _dayDuration + _duskDuration + _nightDuration;
    public float DaylightDuration => _dawnDuration + _dayDuration + _duskDuration;
    public float NightDuration => _nightDuration;
    public float CycleProgress => Mathf.Clamp01(_elapsed / TotalDuration);
    public GamePhase Phase => ResolvePhase(_elapsed);
    public float PhaseProgress => GetPhaseProgress(Phase, _elapsed);
    public float PhaseRemaining => GetPhaseDuration(Phase) * (1f - PhaseProgress);

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        _elapsed = Mathf.Repeat(_elapsed + deltaTime, TotalDuration);
    }

    public void SetPhase(GamePhase phase)
    {
        _elapsed = phase switch
        {
            GamePhase.Dawn => 0f,
            GamePhase.Day => _dawnDuration,
            GamePhase.Dusk => _dawnDuration + _dayDuration,
            GamePhase.Night => DaylightDuration,
            _ => _dawnDuration
        };
    }

    private GamePhase ResolvePhase(float elapsed)
    {
        if (elapsed < _dawnDuration)
        {
            return GamePhase.Dawn;
        }

        if (elapsed < _dawnDuration + _dayDuration)
        {
            return GamePhase.Day;
        }

        if (elapsed < DaylightDuration)
        {
            return GamePhase.Dusk;
        }

        return GamePhase.Night;
    }

    private float GetPhaseProgress(GamePhase phase, float elapsed)
    {
        float start = phase switch
        {
            GamePhase.Day => _dawnDuration,
            GamePhase.Dusk => _dawnDuration + _dayDuration,
            GamePhase.Night => DaylightDuration,
            _ => 0f
        };
        return Mathf.Clamp01((elapsed - start) / GetPhaseDuration(phase));
    }

    private float GetPhaseDuration(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Dawn => _dawnDuration,
            GamePhase.Day => _dayDuration,
            GamePhase.Dusk => _duskDuration,
            GamePhase.Night => _nightDuration,
            _ => 1f
        };
    }
}

}
