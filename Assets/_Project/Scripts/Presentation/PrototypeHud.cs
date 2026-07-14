using Hollowwest.Selection;
using UnityEngine;

namespace Hollowwest.Presentation;

public sealed class PrototypeHud : MonoBehaviour
{
    private SelectionController _selection;
    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;

    public void Initialize(SelectionController selection)
    {
        _selection = selection;
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUI.Box(new Rect(18f, 18f, 390f, 134f), GUIContent.none);
        GUI.Label(new Rect(34f, 28f, 340f, 30f), "HOLLOWWEST — movement proof", _titleStyle);
        GUI.Label(
            new Rect(34f, 62f, 350f, 80f),
            "LMB / drag: select   •   RMB: formation move\nWASD / arrows: pan   •   Wheel: zoom   •   Esc: clear\n" +
            $"Selected: {(_selection == null ? 0 : _selection.SelectedCount)}",
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
