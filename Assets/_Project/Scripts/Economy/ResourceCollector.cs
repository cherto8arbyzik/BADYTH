using Hollowwest.Navigation;
using UnityEngine;

namespace Hollowwest.Economy
{

public sealed class ResourceCollector : MonoBehaviour
{
    [SerializeField] private float harvestRange = 1.15f;
    [SerializeField] private float harvestInterval = 0.75f;

    private NavigationAgent _agent;
    private ResourceStockpile _stockpile;
    private ResourceNode _target;
    private float _harvestTimer;

    public void Initialize(ResourceStockpile stockpile)
    {
        _stockpile = stockpile;
        _agent = GetComponent<NavigationAgent>();
    }

    public void Gather(ResourceNode target)
    {
        _target = target;
        _harvestTimer = 0f;

        if (_agent != null && _target != null)
        {
            _agent.SetDestination(_target.InteractionPoint);
        }
    }

    private void Update()
    {
        if (_target == null || _target.IsDepleted || _stockpile == null)
        {
            _target = null;
            return;
        }

        Vector3 offset = _target.InteractionPoint - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude > harvestRange * harvestRange)
        {
            return;
        }

        _harvestTimer += Time.deltaTime;
        if (_harvestTimer < harvestInterval)
        {
            return;
        }

        _harvestTimer = 0f;
        int harvested = _target.Harvest();
        _stockpile.Add(_target.ResourceType, harvested);
    }
}
}
