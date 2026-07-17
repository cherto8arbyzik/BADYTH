using Hollowwest.Gameplay;
using UnityEngine;

namespace Hollowwest.Presentation
{

public sealed class TownDayNightVisuals : MonoBehaviour
{
    private GameSession _session;
    private Camera _camera;
    private Light _sun;
    private Light _fill;

    public void Initialize(GameSession session, Camera worldCamera, Light sun, Light fill)
    {
        _session = session;
        _camera = worldCamera;
        _sun = sun;
        _fill = fill;
        ApplyLighting(GamePhase.Day, 0f);
    }

    private void Update()
    {
        GamePhase phase = _session?.Phase ?? GamePhase.Day;
        ApplyLighting(phase, _session?.PhaseProgress ?? 0f);
    }

    private void ApplyLighting(GamePhase phase, float progress)
    {
        float daylight = GetDaylight(phase, progress);
        float night = 1f - daylight;
        Color sunrise = new(1f, 0.48f, 0.22f);
        Color day = new(1f, 0.86f, 0.68f);
        Color moon = new(0.32f, 0.40f, 0.62f);
        Color sunColor = phase switch
        {
            GamePhase.Dawn => Color.Lerp(moon, sunrise, Mathf.SmoothStep(0f, 1f, progress)),
            GamePhase.Dusk => Color.Lerp(day, sunrise, Mathf.SmoothStep(0f, 1f, progress)),
            GamePhase.Night => moon,
            _ => day
        };

        float elevation = GetSunElevation(phase, progress);
        float azimuth = Mathf.Lerp(-58f, 122f, _session?.CycleProgress ?? 0.25f);
        if (_sun != null)
        {
            _sun.transform.rotation = Quaternion.Euler(90f - elevation, azimuth, 0f);
            _sun.color = sunColor;
            _sun.intensity = Mathf.Lerp(0.10f, 1.35f, daylight);
        }

        if (_fill != null)
        {
            _fill.color = Color.Lerp(new Color(0.36f, 0.46f, 0.62f), new Color(0.18f, 0.24f, 0.48f), night);
            _fill.intensity = Mathf.Lerp(0.34f, 0.56f, night);
        }

        RenderSettings.ambientLight = Color.Lerp(
            new Color(0.25f, 0.27f, 0.31f),
            new Color(0.055f, 0.075f, 0.13f),
            night);
        RenderSettings.fogColor = Color.Lerp(
            new Color(0.39f, 0.56f, 0.72f),
            new Color(0.055f, 0.075f, 0.14f),
            night);
        RenderSettings.fogDensity = Mathf.Lerp(0.0025f, 0.0065f, night);

        Material skybox = RenderSettings.skybox;
        if (skybox != null)
        {
            skybox.SetColor(
                "_SkyTint",
                Color.Lerp(new Color(0.28f, 0.52f, 0.82f), new Color(0.055f, 0.085f, 0.20f), night));
            skybox.SetColor(
                "_GroundColor",
                Color.Lerp(new Color(0.24f, 0.44f, 0.66f), new Color(0.025f, 0.035f, 0.075f), night));
            skybox.SetFloat("_Exposure", Mathf.Lerp(0.82f, 0.58f, night));
        }

        if (_camera != null)
        {
            _camera.backgroundColor = Color.Lerp(
                new Color(0.44f, 0.66f, 0.86f),
                new Color(0.025f, 0.045f, 0.12f),
                night);
        }
    }

    private static float GetDaylight(GamePhase phase, float progress)
    {
        return phase switch
        {
            GamePhase.Dawn => Mathf.SmoothStep(0.06f, 1f, progress),
            GamePhase.Dusk => Mathf.SmoothStep(1f, 0.06f, progress),
            GamePhase.Night => 0.06f,
            GamePhase.Defeat => 0.06f,
            _ => 1f
        };
    }

    private static float GetSunElevation(GamePhase phase, float progress)
    {
        return phase switch
        {
            GamePhase.Dawn => Mathf.Lerp(-7f, 12f, progress),
            GamePhase.Day => Mathf.Lerp(12f, 68f, Mathf.Sin(progress * Mathf.PI)),
            GamePhase.Dusk => Mathf.Lerp(12f, -8f, progress),
            _ => Mathf.Lerp(-8f, -24f, Mathf.Sin(progress * Mathf.PI))
        };
    }
}
}
