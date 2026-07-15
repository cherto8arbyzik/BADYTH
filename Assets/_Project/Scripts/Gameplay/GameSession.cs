using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class GameSession : MonoBehaviour
{
    private WaveDirector _waveDirector;
    private CampCore _campCore;
    private bool _nightStarted;

    public GamePhase Phase { get; private set; } = GamePhase.Day;
    public int NightNumber { get; private set; } = 1;
    public CampCore CampCore => _campCore;

    public void Initialize(WaveDirector waveDirector, CampCore campCore)
    {
        _waveDirector = waveDirector;
        _campCore = campCore;
        Phase = GamePhase.Day;
        _nightStarted = false;
    }

    public void SetDayForDevelopment()
    {
        if (Phase == GamePhase.Victory)
        {
            NightNumber++;
        }

        if (_waveDirector != null)
        {
            _waveDirector.StopNight(true);
        }

        Phase = GamePhase.Day;
        _nightStarted = false;
    }

    public void SetNightForDevelopment()
    {
        if (Phase == GamePhase.Night)
        {
            return;
        }

        if (Phase == GamePhase.Victory)
        {
            NightNumber++;
        }

        if (_waveDirector != null)
        {
            _waveDirector.StopNight(true);
        }

        Phase = GamePhase.Night;
        _nightStarted = false;
    }

    private void Update()
    {
        if (Phase == GamePhase.Night)
        {
            TickNight();
        }
    }

    private void TickNight()
    {
        if (_campCore != null && _campCore.IsDestroyed)
        {
            Phase = GamePhase.Defeat;
            return;
        }

        if (!_nightStarted)
        {
            _nightStarted = true;
            if (_waveDirector != null)
            {
                _waveDirector.StartNight(NightNumber);
            }
        }

        if (_waveDirector != null && _waveDirector.IsNightComplete)
        {
            Phase = GamePhase.Victory;
        }
    }
}
}
