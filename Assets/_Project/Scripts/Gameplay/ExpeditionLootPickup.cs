using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class ExpeditionLootPickup : MonoBehaviour, IExpeditionInteractable
{
    private ExpeditionSceneController _sceneController;
    private ResourceType _resourceType;
    private int _amount;
    private Vector3 _start;
    private Vector3 _target;
    private float _flightTime;

    public Vector3 InteractionPosition => transform.position;
    public string Prompt => $"E — поднять: {ResourceNames.GetShort(_resourceType)} ({_amount})";

    public void Configure(
        ExpeditionSceneController sceneController,
        ResourceType resourceType,
        int amount,
        Vector3 launchOffset)
    {
        _sceneController = sceneController;
        _resourceType = resourceType;
        _amount = Mathf.Max(1, amount);
        _start = transform.position;
        _target = transform.position + Vector3.ProjectOnPlane(launchOffset, Vector3.up);
        _sceneController.RegisterInteractable(this);
    }

    public bool CanInteract(ExpeditionHeroController hero)
    {
        return hero != null && hero.Backpack != null && _amount > 0 && _flightTime >= 0.55f;
    }

    public void Interact(ExpeditionHeroController hero)
    {
        int accepted = hero.Backpack.TryAdd(_resourceType, _amount);
        if (accepted <= 0)
        {
            _sceneController.ShowMessage("В рюкзаке нет свободных ячеек", 1.8f);
            return;
        }

        _amount -= accepted;
        _sceneController.ShowMessage($"Подобрано: {ResourceNames.GetShort(_resourceType)} +{accepted}", 1.3f);
        if (_amount <= 0)
        {
            _sceneController.UnregisterInteractable(this);
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (_flightTime < 0.55f)
        {
            _flightTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_flightTime / 0.55f);
            transform.position = Vector3.Lerp(_start, _target, progress) + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 1.8f + 0.28f);
            transform.Rotate(Vector3.up, 420f * Time.deltaTime, Space.World);
        }
        else
        {
            transform.position = new Vector3(_target.x, _target.y + 0.28f, _target.z);
            transform.Rotate(Vector3.up, 48f * Time.deltaTime, Space.World);
        }
    }

    private void OnDestroy()
    {
        _sceneController?.UnregisterInteractable(this);
    }
}

}
