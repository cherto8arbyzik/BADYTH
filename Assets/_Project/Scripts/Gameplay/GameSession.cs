using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class GameSession : MonoBehaviour
{
    private WaveDirector _waveDirector;
    private CampCore _campCore;
    private DayNightCycle _cycle;
    private bool _nightStarted;

    public GamePhase Phase { get; private set; } = GamePhase.Day;
    public int NightNumber { get; private set; } = 1;
    public CampCore CampCore => _campCore;
    public float PhaseProgress => _cycle?.PhaseProgress ?? 0f;
    public float PhaseRemaining => _cycle?.PhaseRemaining ?? 0f;
    public float CycleProgress => _cycle?.CycleProgress ?? 0f;

    public void Initialize(WaveDirector waveDirector, CampCore campCore)
    {
        _waveDirector = waveDirector;
        _campCore = campCore;
        _cycle = new DayNightCycle(720f, 300f, 60f);
        Phase = _cycle.Phase;
        _nightStarted = false;
    }

    public void SetDayForDevelopment()
    {
        if (_waveDirector != null)
        {
            _waveDirector.StopNight(true);
        }

        _cycle?.SetPhase(GamePhase.Day);
        Phase = GamePhase.Day;
        _nightStarted = false;
    }

    public void SetNightForDevelopment()
    {
        if (Phase == GamePhase.Night)
        {
            return;
        }

        if (_waveDirector != null)
        {
            _waveDirector.StopNight(true);
        }

        _cycle?.SetPhase(GamePhase.Night);
        Phase = GamePhase.Night;
        _nightStarted = false;
    }

    private void Update()
    {
        if (_campCore != null && _campCore.IsDestroyed)
        {
            Phase = GamePhase.Defeat;
            return;
        }

        if (_cycle == null)
        {
            return;
        }

        GamePhase previous = Phase;
        _cycle.Tick(Time.deltaTime);
        Phase = _cycle.Phase;
        HandlePhaseTransition(previous, Phase);

        if (Phase == GamePhase.Night)
        {
            TickNight();
        }
    }

    private void TickNight()
    {
        if (!_nightStarted)
        {
            _nightStarted = true;
            if (_waveDirector != null)
            {
                _waveDirector.StartNight(NightNumber);
            }
        }

    }

    private void HandlePhaseTransition(GamePhase previous, GamePhase current)
    {
        if (previous == current)
        {
            return;
        }

        if (previous == GamePhase.Night)
        {
            _waveDirector?.StopNight(true);
            _nightStarted = false;
            NightNumber++;
        }

        if (current == GamePhase.Night)
        {
            _nightStarted = false;
        }
    }
}
}
