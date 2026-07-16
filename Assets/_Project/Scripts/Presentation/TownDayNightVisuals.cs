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
    private float _nightBlend;

    public void Initialize(GameSession session, Camera worldCamera, Light sun, Light fill)
    {
        _session = session;
        _camera = worldCamera;
        _sun = sun;
        _fill = fill;
        ApplyLighting(0f);
    }

    private void Update()
    {
        bool isNight = _session != null && _session.Phase == GamePhase.Night;
        float target = isNight ? 1f : 0f;
        _nightBlend = Mathf.MoveTowards(_nightBlend, target, Time.deltaTime * 0.7f);
        ApplyLighting(_nightBlend);
    }

    private void ApplyLighting(float night)
    {
        if (_sun != null)
        {
            _sun.color = Color.Lerp(new Color(1f, 0.79f, 0.58f), new Color(0.32f, 0.40f, 0.62f), night);
            _sun.intensity = Mathf.Lerp(1.35f, 0.34f, night);
        }

        if (_fill != null)
        {
            _fill.color = Color.Lerp(new Color(0.36f, 0.46f, 0.62f), new Color(0.18f, 0.24f, 0.48f), night);
            _fill.intensity = Mathf.Lerp(0.34f, 0.62f, night);
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
}
}
