using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class ExpeditionGatherable : MonoBehaviour, IExpeditionInteractable
{
    private ExpeditionSceneController _sceneController;
    private ResourceType _resourceType;
    private string _displayName;
    private int _remaining;

    public Vector3 InteractionPosition => transform.position;
    public string Prompt => $"E — собрать: {_displayName} ({_remaining})";

    public void Configure(
        ExpeditionSceneController sceneController,
        ResourceType resourceType,
        int amount,
        string displayName)
    {
        _sceneController = sceneController;
        _resourceType = resourceType;
        _remaining = Mathf.Max(1, amount);
        _displayName = displayName;
        _sceneController.RegisterInteractable(this);
    }

    public bool CanInteract(ExpeditionHeroController hero)
    {
        return hero != null && hero.Backpack != null && _remaining > 0;
    }

    public void Interact(ExpeditionHeroController hero)
    {
        int accepted = hero.Backpack.TryAdd(_resourceType, _remaining);
        if (accepted <= 0)
        {
            _sceneController.ShowMessage("В рюкзаке нет места для этого ресурса", 2f);
            return;
        }

        _remaining -= accepted;
        _sceneController.ShowMessage($"Собрано: {ResourceNames.GetShort(_resourceType)} +{accepted}", 1.6f);
        if (_remaining <= 0)
        {
            _sceneController.UnregisterInteractable(this);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        _sceneController?.UnregisterInteractable(this);
    }
}

public enum ExpeditionPortalKind
{
    EnterAnomaly,
    LeaveAnomaly,
    ReturnHome
}

public sealed class ExpeditionPortal : MonoBehaviour, IExpeditionInteractable
{
    private ExpeditionSceneController _sceneController;
    private ExpeditionPortalKind _kind;

    public Vector3 InteractionPosition => transform.position;
    public string Prompt => _kind switch
    {
        ExpeditionPortalKind.EnterAnomaly => "E — войти в нестабильную аномалию",
        ExpeditionPortalKind.LeaveAnomaly => "E — покинуть аномалию",
        _ => "E — вернуться в поселение"
    };

    public void Configure(ExpeditionSceneController sceneController, ExpeditionPortalKind kind)
    {
        _sceneController = sceneController;
        _kind = kind;
        _sceneController.RegisterInteractable(this);
    }

    public bool CanInteract(ExpeditionHeroController hero)
    {
        return _sceneController != null && hero != null;
    }

    public void Interact(ExpeditionHeroController hero)
    {
        switch (_kind)
        {
            case ExpeditionPortalKind.EnterAnomaly:
                _sceneController.EnterAnomaly(hero);
                break;
            case ExpeditionPortalKind.LeaveAnomaly:
                _sceneController.LeaveAnomaly(hero);
                break;
            default:
                _sceneController.RequestReturnHome(false);
                break;
        }
    }

    private void OnDestroy()
    {
        _sceneController?.UnregisterInteractable(this);
    }
}

public sealed class ExpeditionRewardShrine : MonoBehaviour, IExpeditionInteractable
{
    private ExpeditionSceneController _sceneController;
    private bool _claimed;

    public Vector3 InteractionPosition => transform.position;
    public string Prompt => _claimed
        ? "Переход уже использован"
        : _sceneController != null && _sceneController.IsFinalDungeonFloor
            ? "E — очистить сердце подземелья"
            : "E — спуститься на следующий этаж";

    public void Configure(ExpeditionSceneController sceneController)
    {
        _sceneController = sceneController;
        _sceneController.RegisterInteractable(this);
    }

    public bool CanInteract(ExpeditionHeroController hero)
    {
        return !_claimed && _sceneController != null && _sceneController.EnemiesRemaining == 0;
    }

    public void Interact(ExpeditionHeroController hero)
    {
        if (!CanInteract(hero))
        {
            return;
        }

        _claimed = true;
        _sceneController.ResolveDungeonGate();
    }

    private void OnDestroy()
    {
        _sceneController?.UnregisterInteractable(this);
    }
}

}
