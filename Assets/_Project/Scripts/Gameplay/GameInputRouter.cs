using UnityEngine;
using UnityEngine.InputSystem;

namespace Hollowwest.Controls
{

public enum PlayerControlContext
{
    Settlement,
    Hero
}

/// <summary>
/// Owns the project's context-sensitive input maps. The settlement can keep
/// its strategy controls while an expedition gives the same WASD keys to the
/// directly controlled hero without both systems receiving input.
/// </summary>
public sealed class GameInputRouter : MonoBehaviour
{
    private InputActionMap _settlementMap;
    private InputActionMap _heroMap;
    private InputActionMap _uiMap;

    public static GameInputRouter Instance { get; private set; }

    public PlayerControlContext Context { get; private set; }

    public InputAction SettlementMove { get; private set; }
    public InputAction SettlementZoom { get; private set; }
    public InputAction SettlementOrbit { get; private set; }
    public InputAction SettlementLookDelta { get; private set; }

    public InputAction HeroMove { get; private set; }
    public InputAction HeroAimPosition { get; private set; }
    public InputAction HeroAttack { get; private set; }
    public InputAction HeroSecondary { get; private set; }
    public InputAction HeroInteract { get; private set; }
    public InputAction HeroDodge { get; private set; }
    public InputAction HeroBackpack { get; private set; }
    public InputAction Cancel { get; private set; }

    public static GameInputRouter EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameInputRouter existing = FindFirstObjectByType<GameInputRouter>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject root = new("Game Input Router");
        DontDestroyOnLoad(root);
        return root.AddComponent<GameInputRouter>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildMaps();
        ActivateSettlement();
    }

    public void ActivateSettlement()
    {
        Context = PlayerControlContext.Settlement;
        _heroMap.Disable();
        _settlementMap.Enable();
        _uiMap.Enable();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ActivateHero()
    {
        Context = PlayerControlContext.Hero;
        _settlementMap.Disable();
        _heroMap.Enable();
        _uiMap.Enable();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void BuildMaps()
    {
        _settlementMap = new InputActionMap("Settlement");
        SettlementMove = _settlementMap.AddAction("Move", InputActionType.Value);
        AddWasdAndArrowBindings(SettlementMove);
        SettlementZoom = _settlementMap.AddAction("Zoom", InputActionType.PassThrough, "<Mouse>/scroll/y");
        SettlementOrbit = _settlementMap.AddAction("Orbit", InputActionType.Button, "<Mouse>/middleButton");
        SettlementLookDelta = _settlementMap.AddAction("LookDelta", InputActionType.PassThrough, "<Pointer>/delta");

        _heroMap = new InputActionMap("Hero");
        HeroMove = _heroMap.AddAction("Move", InputActionType.Value);
        AddWasdAndArrowBindings(HeroMove);
        HeroAimPosition = _heroMap.AddAction("AimPosition", InputActionType.PassThrough, "<Pointer>/position");
        HeroAttack = _heroMap.AddAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
        HeroSecondary = _heroMap.AddAction("Secondary", InputActionType.Button, "<Mouse>/rightButton");
        HeroInteract = _heroMap.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
        HeroDodge = _heroMap.AddAction("Dodge", InputActionType.Button, "<Keyboard>/space");
        HeroBackpack = _heroMap.AddAction("Backpack", InputActionType.Button, "<Keyboard>/tab");

        _uiMap = new InputActionMap("UI");
        Cancel = _uiMap.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
    }

    private static void AddWasdAndArrowBindings(InputAction action)
    {
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        _settlementMap?.Dispose();
        _heroMap?.Dispose();
        _uiMap?.Dispose();
        Instance = null;
    }
}

}
