using UnityEngine;

namespace Hollowwest.Economy
{

public sealed class ResourceNode : MonoBehaviour
{
    [SerializeField] private int amount = 80;
    [SerializeField] private int harvestPerTick = 5;

    private Renderer _renderer;
    private Color _fullColor;

    public int Amount => amount;
    public bool IsDepleted => amount <= 0;
    public Vector3 InteractionPoint => transform.position;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            _fullColor = _renderer.sharedMaterial.color;
        }
    }

    public int Harvest()
    {
        if (amount <= 0)
        {
            return 0;
        }

        int harvested = Mathf.Min(harvestPerTick, amount);
        amount -= harvested;
        RefreshVisuals();
        return harvested;
    }

    private void RefreshVisuals()
    {
        if (_renderer == null)
        {
            return;
        }

        float fullness = Mathf.Clamp01(amount / 80f);
        _renderer.sharedMaterial.color = Color.Lerp(new Color(0.18f, 0.14f, 0.09f), _fullColor, fullness);
        transform.localScale = new Vector3(1f, Mathf.Lerp(0.25f, 1f, fullness), 1f);
    }
}
}
