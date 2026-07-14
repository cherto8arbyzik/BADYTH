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
    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;

    public void Initialize(SelectionController selection, ResourceStockpile stockpile, GameSession session)
    {
        _selection = selection;
        _stockpile = stockpile;
        _session = session;
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUI.Box(new Rect(18f, 18f, 455f, 198f), GUIContent.none);
        GUI.Label(new Rect(34f, 28f, 400f, 30f), "ZASTAVA - day/night proof", _titleStyle);
        GUI.Label(
            new Rect(34f, 62f, 420f, 136f),
            "LMB / drag: select   |   RMB ground: move\nRMB wood cache: gather   |   WASD: pan   |   Wheel: zoom\n" +
            $"Selected: {(_selection == null ? 0 : _selection.SelectedCount)}\n" +
            $"Wood: {(_stockpile == null ? 0 : _stockpile.Wood)}\n" +
            BuildSessionLine(),
            _bodyStyle);
    }

    private string BuildSessionLine()
    {
        if (_session == null)
        {
            return "Phase: -";
        }

        string coreHealth = _session.CampCore == null
            ? "-"
            : $"{_session.CampCore.Health}/{_session.CampCore.MaxHealth}";

        if (_session.Phase == GamePhase.Day)
        {
            return $"Phase: DAY   Time to night: {Mathf.CeilToInt(_session.TimeRemaining)}s   Core: {coreHealth}";
        }

        return $"Phase: {_session.Phase.ToString().ToUpperInvariant()}   Enemies: {EnemyUnit.ActiveEnemies.Count}   Core: {coreHealth}";
    }

    private void EnsureStyles()
    {
        if (_titleStyle != null)
        {
            return;
        }

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.93f, 0.84f, 0.56f) }
        };

        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
    }
}
}
