using Hollowwest.Gameplay;
using Hollowwest.Navigation;
using UnityEngine;

namespace Hollowwest.Presentation
{

public sealed class DialogueController : MonoBehaviour
{
    private const float ConversationDistance = 2.7f;

    private Transform _hero;
    private NavigationAgent _heroAgent;
    private RtsCameraController _cameraController;
    private NpcDialogue _pendingNpc;
    private NpcDialogue _activeNpc;
    private TownResident _pausedResident;
    private Transform _focusAnchor;
    private string _currentResponse;
    private float _nextApproachRetry;
    private GUIStyle _panelStyle;
    private GUIStyle _nameStyle;
    private GUIStyle _roleStyle;
    private GUIStyle _bodyStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _promptStyle;

    public static bool IsBlockingWorldInput { get; private set; }
    public bool IsOpen => _activeNpc != null;
    public bool IsApproaching => _pendingNpc != null;

    public void Initialize(Transform hero, NavigationAgent heroAgent, RtsCameraController cameraController)
    {
        _hero = hero;
        _heroAgent = heroAgent;
        _cameraController = cameraController;
        GameObject anchor = new("Dialogue Camera Focus");
        anchor.transform.SetParent(transform, false);
        _focusAnchor = anchor.transform;
    }

    public bool TryBeginInteraction(NpcDialogue npc)
    {
        if (npc == null || _hero == null || _heroAgent == null)
        {
            return false;
        }

        if (_activeNpc != null)
        {
            CloseConversation();
        }

        _pendingNpc = npc;
        _nextApproachRetry = 0f;
        if (HorizontalDistance(_hero.position, npc.transform.position) <= ConversationDistance)
        {
            OpenConversation(npc);
            return true;
        }

        MoveHeroNear(npc);
        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_activeNpc != null)
            {
                CloseConversation();
            }
            else if (_pendingNpc != null)
            {
                _pendingNpc = null;
                _heroAgent?.Stop();
            }
        }

        if (_pendingNpc != null)
        {
            if (HorizontalDistance(_hero.position, _pendingNpc.transform.position) <= ConversationDistance)
            {
                OpenConversation(_pendingNpc);
            }
            else if (!_heroAgent.IsMoving && Time.time >= _nextApproachRetry)
            {
                MoveHeroNear(_pendingNpc);
            }
        }

        if (_activeNpc == null)
        {
            return;
        }

        if (_hero == null || _activeNpc == null)
        {
            CloseConversation();
            return;
        }

        _focusAnchor.position = (_hero.position + _activeNpc.transform.position) * 0.5f;
        FaceEachOther(_hero, _activeNpc.transform);
        FaceEachOther(_activeNpc.transform, _hero);
    }

    private void MoveHeroNear(NpcDialogue npc)
    {
        if (npc == null)
        {
            _pendingNpc = null;
            return;
        }

        Vector3 away = _hero.position - npc.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
        {
            away = Vector3.back;
        }

        Vector3 destination = npc.transform.position + away.normalized * 1.75f;
        if (!_heroAgent.SetDestination(destination))
        {
            _nextApproachRetry = Time.time + 0.8f;
        }
    }

    private void OpenConversation(NpcDialogue npc)
    {
        _pendingNpc = null;
        _activeNpc = npc;
        _heroAgent.Stop();
        _currentResponse = npc.Greeting;
        _pausedResident = npc.GetComponent<TownResident>();
        _pausedResident?.BeginDialogue();
        _focusAnchor.position = (_hero.position + npc.transform.position) * 0.5f;
        _cameraController?.BeginFocus(_focusAnchor, 10.5f);
        IsBlockingWorldInput = true;
    }

    private void CloseConversation()
    {
        _pausedResident?.EndDialogue();
        _pausedResident = null;
        _activeNpc = null;
        _currentResponse = string.Empty;
        _cameraController?.EndFocus();
        IsBlockingWorldInput = false;
    }

    private void OnGUI()
    {
        if (_pendingNpc != null && _activeNpc == null)
        {
            EnsureStyles();
            Rect hint = new(Screen.width * 0.5f - 190f, 68f, 380f, 34f);
            GUI.Box(hint, "Герой идёт поговорить с: " + _pendingNpc.DisplayName, _promptStyle);
        }

        if (_activeNpc == null)
        {
            return;
        }

        EnsureStyles();
        float width = Mathf.Min(920f, Screen.width - 48f);
        float height = Mathf.Min(285f, Screen.height * 0.38f);
        Rect panel = new((Screen.width - width) * 0.5f, Screen.height - height - 24f, width, height);
        GUI.Box(panel, GUIContent.none, _panelStyle);

        float leftWidth = width * 0.58f;
        GUI.Label(new Rect(panel.x + 24f, panel.y + 18f, leftWidth - 36f, 30f), _activeNpc.DisplayName, _nameStyle);
        GUI.Label(new Rect(panel.x + 24f, panel.y + 49f, leftWidth - 36f, 22f), _activeNpc.Role, _roleStyle);
        GUI.Label(new Rect(panel.x + 24f, panel.y + 78f, leftWidth - 36f, 44f), _activeNpc.StoryHook, _roleStyle);
        GUI.Label(new Rect(panel.x + 24f, panel.y + 132f, leftWidth - 36f, height - 150f), _currentResponse, _bodyStyle);

        float choicesX = panel.x + leftWidth + 8f;
        float choicesWidth = width - leftWidth - 30f;
        float buttonY = panel.y + 18f;
        foreach (DialogueTopic topic in _activeNpc.Topics)
        {
            if (GUI.Button(new Rect(choicesX, buttonY, choicesWidth, 39f), topic.Prompt, _buttonStyle))
            {
                if (topic.EndsConversation)
                {
                    CloseConversation();
                    return;
                }

                _currentResponse = topic.Response;
            }

            buttonY += 45f;
        }
    }

    private void EnsureStyles()
    {
        if (_panelStyle != null)
        {
            return;
        }

        Texture2D panelTexture = MakeTexture(new Color(0.035f, 0.045f, 0.04f, 0.96f));
        Texture2D buttonTexture = MakeTexture(new Color(0.15f, 0.18f, 0.14f, 0.96f));
        Texture2D buttonHover = MakeTexture(new Color(0.29f, 0.25f, 0.14f, 0.98f));
        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTexture }
        };
        _nameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.95f, 0.79f, 0.40f) }
        };
        _roleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            wordWrap = true,
            normal = { textColor = new Color(0.70f, 0.76f, 0.70f) }
        };
        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            wordWrap = true,
            normal = { textColor = new Color(0.92f, 0.91f, 0.84f) }
        };
        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(13, 8, 5, 5),
            normal = { background = buttonTexture, textColor = new Color(0.92f, 0.90f, 0.80f) },
            hover = { background = buttonHover, textColor = Color.white }
        };
        _promptStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = panelTexture, textColor = new Color(0.92f, 0.85f, 0.58f) }
        };
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new(1, 1) { hideFlags = HideFlags.DontSave };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static void FaceEachOther(Transform source, Transform target)
    {
        Vector3 direction = target.position - source.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        source.rotation = Quaternion.Slerp(
            source.rotation,
            Quaternion.LookRotation(direction.normalized, Vector3.up),
            8f * Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_activeNpc != null)
        {
            CloseConversation();
        }

        IsBlockingWorldInput = false;
    }
}
}
