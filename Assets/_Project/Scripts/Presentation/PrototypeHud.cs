using Hollowwest.Economy;
using Hollowwest.Selection;
using UnityEngine;

namespace Hollowwest.Presentation
{

public sealed class PrototypeHud : MonoBehaviour
{
    private SelectionController _selection;
    private ResourceStockpile _stockpile;
    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;

    public void Initialize(SelectionController selection, ResourceStockpile stockpile)
    {
        _selection = selection;
        _stockpile = stockpile;
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUI.Box(new Rect(18f, 18f, 430f, 154f), GUIContent.none);
        GUI.Label(new Rect(34f, 28f, 380f, 30f), "ZASTAVA - day raid proof", _titleStyle);
        GUI.Label(
            new Rect(34f, 62f, 390f, 96f),
            "LMB / drag: select   |   RMB ground: move\nRMB wood cache: gather   |   WASD: pan   |   Wheel: zoom\n" +
            $"Selected: {(_selection == null ? 0 : _selection.SelectedCount)}\n" +
            $"Wood: {(_stockpile == null ? 0 : _stockpile.Wood)}",
            _bodyStyle);
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
