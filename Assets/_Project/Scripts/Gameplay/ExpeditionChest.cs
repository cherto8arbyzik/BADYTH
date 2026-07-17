using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class ExpeditionChest : MonoBehaviour, IExpeditionInteractable
{
    private ExpeditionSceneController _sceneController;
    private Transform _lid;
    private int _floor;
    private bool _opened;

    public Vector3 InteractionPosition => transform.position;
    public string Prompt => _opened ? "Сундук пуст" : "E — открыть сундук";

    public void Configure(ExpeditionSceneController sceneController, Transform lid, int floor)
    {
        _sceneController = sceneController;
        _lid = lid;
        _floor = floor;
        _sceneController.RegisterInteractable(this);
    }

    public bool CanInteract(ExpeditionHeroController hero)
    {
        return !_opened && hero != null && _sceneController != null;
    }

    public void Interact(ExpeditionHeroController hero)
    {
        if (!CanInteract(hero))
        {
            return;
        }

        _opened = true;
        if (_lid != null)
        {
            _lid.localRotation = Quaternion.Euler(-68f, 0f, 0f);
        }

        _sceneController.OpenChest(transform.position + Vector3.up * 0.7f, _floor);
    }

    private void OnDestroy()
    {
        _sceneController?.UnregisterInteractable(this);
    }
}

}
