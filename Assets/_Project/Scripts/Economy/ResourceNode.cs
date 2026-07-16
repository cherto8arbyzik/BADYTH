using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Economy
{

public sealed class ResourceNode : MonoBehaviour
{
    private static readonly List<ResourceNode> ActiveNodesInternal = new();

    [SerializeField] private ResourceType resourceType = ResourceType.Timber;
    [SerializeField] private int amount = 80;
    [SerializeField] private int harvestPerTick = 5;
    [SerializeField] private int regenerationPerInterval;
    [SerializeField] private float regenerationInterval = 15f;

    private int _maximumAmount = 80;
    private float _regenerationTimer;
    private bool _visualizeDepletion = true;

    private Renderer _renderer;
    private Color _fullColor;
    private Vector3 _fullScale;

    public static IReadOnlyList<ResourceNode> ActiveNodes => ActiveNodesInternal;
    public int Amount => amount;
    public int MaximumAmount => _maximumAmount;
    public ResourceType ResourceType => resourceType;
    public bool IsDepleted => amount <= 0;
    public bool IsRenewable => regenerationPerInterval > 0;
    public Vector3 InteractionPoint => transform.position;

    public void Configure(ResourceType type, int totalAmount, int amountPerHarvest)
    {
        resourceType = type;
        amount = Mathf.Max(0, totalAmount);
        _maximumAmount = Mathf.Max(1, amount);
        harvestPerTick = Mathf.Max(1, amountPerHarvest);
    }

    public void ConfigureRenewal(int amountPerInterval, float intervalSeconds)
    {
        regenerationPerInterval = Mathf.Max(0, amountPerInterval);
        regenerationInterval = Mathf.Max(1f, intervalSeconds);
        _regenerationTimer = regenerationInterval;
    }

    public void SetVisualDepletion(bool enabled)
    {
        _visualizeDepletion = enabled;
    }

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _fullScale = transform.localScale;
        if (_renderer != null)
        {
            _renderer.material = new Material(_renderer.sharedMaterial)
            {
                hideFlags = HideFlags.DontSave
            };
            _fullColor = _renderer.material.color;
        }
    }

    private void OnEnable()
    {
        if (!ActiveNodesInternal.Contains(this))
        {
            ActiveNodesInternal.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveNodesInternal.Remove(this);
    }

    public int Harvest()
    {
        return Harvest(harvestPerTick);
    }

    public int Harvest(int requestedAmount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int harvested = Mathf.Min(Mathf.Max(1, requestedAmount), amount);
        amount -= harvested;
        RefreshVisuals();
        return harvested;
    }

    private void Update()
    {
        if (regenerationPerInterval <= 0 || amount >= _maximumAmount)
        {
            return;
        }

        _regenerationTimer -= Time.deltaTime;
        if (_regenerationTimer > 0f)
        {
            return;
        }

        _regenerationTimer = regenerationInterval;
        amount = Mathf.Min(_maximumAmount, amount + regenerationPerInterval);
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (_renderer == null)
        {
            return;
        }

        float fullness = Mathf.Clamp01((float)amount / _maximumAmount);
        _renderer.material.color = Color.Lerp(new Color(0.18f, 0.14f, 0.09f), _fullColor, fullness);
        if (_visualizeDepletion)
        {
            transform.localScale = new Vector3(
                _fullScale.x,
                _fullScale.y * Mathf.Lerp(0.25f, 1f, fullness),
                _fullScale.z);
        }
    }
}
}
