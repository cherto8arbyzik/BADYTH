using Hollowwest.Economy;
using Hollowwest.Gameplay;
using Hollowwest.Selection;
using UnityEngine;

namespace Hollowwest.Presentation
{

public sealed class PrototypeHud : MonoBehaviour
{
    private SelectionController _selection;
    private ResourceStockpile _stockpile;
    private GameSession _session;
    private bool _showControls = true;
    private GUIStyle _panelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _phaseStyle;
    private GUIStyle _bodyStyle;
    private GUIStyle _hintStyle;
    private GUIStyle _barBackgroundStyle;
    private GUIStyle _barFillStyle;

    public static bool BlocksWorldInput(Vector2 screenPosition)
    {
        Vector2 guiPosition = new(screenPosition.x, Screen.height - screenPosition.y);
        Rect phasePanel = new(14f, 12f, 270f, 92f);
        Rect statusPanel = new(Mathf.Max(14f, Screen.width - 306f), 12f, 292f, 62f);
        Rect controlsPanel = new(14f, Screen.height - 70f, Mathf.Min(510f, Screen.width - 28f), 58f);
        Rect buildingPanel = new(Mathf.Max(14f, Screen.width - 334f), Screen.height - 140f, 320f, 126f);
        return phasePanel.Contains(guiPosition) ||
               statusPanel.Contains(guiPosition) ||
               controlsPanel.Contains(guiPosition) ||
               buildingPanel.Contains(guiPosition);
    }

    public void Initialize(SelectionController selection, ResourceStockpile stockpile, GameSession session)
    {
        _selection = selection;
        _stockpile = stockpile;
        _session = session;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.F1))
        {
            _showControls = !_showControls;
        }
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawPhasePanel();
        DrawStatusPanel();
        DrawBuildingPanel();
        DrawControlsPanel();
    }

    private void DrawPhasePanel()
    {
        Rect panel = new(14f, 12f, 270f, 92f);
        GUI.Box(panel, GUIContent.none, _panelStyle);
        GUI.Label(new Rect(28f, 18f, 230f, 24f), "ZASTAVA / TOWN DEV", _titleStyle);

        string phaseText = _session == null
            ? "Preparing the village"
            : BuildPhaseText();

        _phaseStyle.normal.textColor = GetPhaseColor();
        GUI.Label(new Rect(28f, 42f, 230f, 22f), phaseText, _phaseStyle);

        if (GUI.Button(new Rect(28f, 66f, 104f, 24f), "DAY") && _session != null)
        {
            _session.SetDayForDevelopment();
        }

        if (GUI.Button(new Rect(142f, 66f, 124f, 24f), "NIGHT + WAVE") && _session != null)
        {
            _session.SetNightForDevelopment();
        }
    }

    private void DrawStatusPanel()
    {
        float width = 292f;
        float x = Mathf.Max(14f, Screen.width - width - 14f);
        Rect panel = new(x, 12f, width, 62f);
        GUI.Box(panel, GUIContent.none, _panelStyle);

        int selected = _selection == null ? 0 : _selection.SelectedCount;
        int wood = _stockpile == null ? 0 : _stockpile.Wood;
        int enemies = EnemyUnit.ActiveEnemies.Count;
        GUI.Label(new Rect(x + 14f, 19f, 264f, 20f), $"Hero {selected}     Wood {wood}     Enemies {enemies}", _bodyStyle);

        int health = _session == null || _session.CampCore == null ? 0 : _session.CampCore.Health;
        int maxHealth = _session == null || _session.CampCore == null ? 100 : _session.CampCore.MaxHealth;
        float healthRatio = maxHealth <= 0 ? 0f : Mathf.Clamp01((float)health / maxHealth);

        GUI.Label(new Rect(x + 14f, 42f, 68f, 18f), "Shrine", _hintStyle);
        Rect bar = new(x + 72f, 45f, 160f, 11f);
        GUI.Box(bar, GUIContent.none, _barBackgroundStyle);
        GUI.Box(new Rect(bar.x, bar.y, bar.width * healthRatio, bar.height), GUIContent.none, _barFillStyle);
        GUI.Label(new Rect(x + 238f, 40f, 45f, 20f), $"{health}", _hintStyle);
    }

    private void DrawBuildingPanel()
    {
        TownBuilding building = _selection == null ? null : _selection.SelectedBuilding;
        if (building == null)
        {
            return;
        }

        float width = 320f;
        float x = Mathf.Max(14f, Screen.width - width - 14f);
        float y = Screen.height - 140f;
        GUI.Box(new Rect(x, y, width, 126f), GUIContent.none, _panelStyle);
        GUI.Label(new Rect(x + 14f, y + 10f, width - 28f, 24f), building.DisplayName, _titleStyle);

        if (!building.IsRuined)
        {
            _phaseStyle.normal.textColor = new Color(0.48f, 0.92f, 0.55f);
            GUI.Label(new Rect(x + 14f, y + 41f, width - 28f, 22f), "OPERATIONAL", _phaseStyle);
            GUI.Label(new Rect(x + 14f, y + 67f, width - 28f, 42f), "Worker assignment will be added here next.", _hintStyle);
            return;
        }

        int wood = _stockpile == null ? 0 : _stockpile.Wood;
        int missing = Mathf.Max(0, building.RestorationCost - wood);
        _phaseStyle.normal.textColor = new Color(1f, 0.54f, 0.24f);
        GUI.Label(new Rect(x + 14f, y + 39f, width - 28f, 22f), $"RUINED  /  restoration: {building.RestorationCost} wood", _phaseStyle);
        GUI.Label(
            new Rect(x + 14f, y + 63f, width - 28f, 20f),
            missing > 0 ? $"Need {missing} more wood" : "Materials ready",
            _hintStyle);

        GUI.enabled = missing == 0;
        if (GUI.Button(new Rect(x + 14f, y + 88f, width - 28f, 26f), "RESTORE BUILDING"))
        {
            building.TryRestore(_stockpile);
        }
        GUI.enabled = true;
    }

    private void DrawControlsPanel()
    {
        if (!_showControls)
        {
            Rect collapsed = new(14f, Screen.height - 38f, 112f, 26f);
            GUI.Box(collapsed, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(24f, Screen.height - 34f, 96f, 20f), "H  Controls", _hintStyle);
            return;
        }

        float width = Mathf.Min(510f, Screen.width - 28f);
        Rect panel = new(14f, Screen.height - 70f, width, 56f);
        GUI.Box(panel, GUIContent.none, _panelStyle);
        GUI.Label(
            new Rect(28f, Screen.height - 62f, width - 28f, 22f),
            "LMB select hero/building    RMB move or gather    WASD pan    Wheel zoom",
            _bodyStyle);
        GUI.Label(
            new Rect(28f, Screen.height - 39f, width - 28f, 20f),
            "Hero gathers wood. Click a ruined building to restore it. H or F1 hides this help.",
            _hintStyle);
    }

    private string BuildPhaseText()
    {
        if (_session.Phase == GamePhase.Day)
        {
            return "DAY  -  no automatic timer";
        }

        if (_session.Phase == GamePhase.Night)
        {
            return "NIGHT  -  defend the shrine";
        }

        return _session.Phase == GamePhase.Victory
            ? "DAWN  -  the village survived"
            : "DEFEAT  -  the shrine fell";
    }

    private Color GetPhaseColor()
    {
        if (_session == null || _session.Phase == GamePhase.Day)
        {
            return new Color(0.96f, 0.77f, 0.34f);
        }

        if (_session.Phase == GamePhase.Night)
        {
            return new Color(0.56f, 0.70f, 1f);
        }

        return _session.Phase == GamePhase.Victory
            ? new Color(0.48f, 0.92f, 0.55f)
            : new Color(1f, 0.38f, 0.34f);
    }

    private void EnsureStyles()
    {
        if (_panelStyle != null)
        {
            return;
        }

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = CreateTexture(new Color(0.035f, 0.04f, 0.045f, 0.88f)) },
            border = new RectOffset(1, 1, 1, 1)
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.94f, 0.84f, 0.58f) }
        };

        _phaseStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.92f, 0.93f, 0.91f) }
        };

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.67f, 0.71f, 0.70f) }
        };

        _barBackgroundStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = CreateTexture(new Color(0.10f, 0.12f, 0.12f, 1f)) }
        };

        _barFillStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = CreateTexture(new Color(0.30f, 0.72f, 0.40f, 1f)) }
        };
    }

    private static Texture2D CreateTexture(Color color)
    {
        Texture2D texture = new(1, 1)
        {
            hideFlags = HideFlags.DontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
}
