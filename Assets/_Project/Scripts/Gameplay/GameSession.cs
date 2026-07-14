using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class GameSession : MonoBehaviour
{
    [SerializeField] private float dayDuration = 45f;

    private WaveDirector _waveDirector;
    private CampCore _campCore;
    private bool _nightStarted;

    public GamePhase Phase { get; private set; } = GamePhase.Day;
    public float TimeRemaining { get; private set; }
    public int NightNumber { get; private set; } = 1;
    public CampCore CampCore => _campCore;

    public void Initialize(WaveDirector waveDirector, CampCore campCore)
    {
        _waveDirector = waveDirector;
        _campCore = campCore;
        Phase = GamePhase.Day;
        TimeRemaining = dayDuration;
    }

    private void Update()
    {
        if (Phase == GamePhase.Day)
        {
            TickDay();
            return;
        }

        if (Phase == GamePhase.Night)
        {
            TickNight();
        }
    }

    private void TickDay()
    {
        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        if (TimeRemaining <= 0f)
        {
            Phase = GamePhase.Night;
            _nightStarted = false;
            TimeRemaining = 0f;
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
