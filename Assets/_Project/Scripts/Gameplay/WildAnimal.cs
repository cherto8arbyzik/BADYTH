using Hollowwest.Economy;
using Hollowwest.Navigation;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class WildAnimal : MonoBehaviour
{
    private NavigationAgent _agent;
    private ResourceNode _resource;
    private Renderer[] _renderers;
    private Vector3 _home;
    private float _wanderRadius;
    private float _nextDecisionTime;
    private int _seed;
    private bool _hidden;

    public string SpeciesName { get; private set; }

    public void Initialize(
        string speciesName,
        NavigationAgent agent,
        ResourceNode resource,
        Vector3 home,
        float wanderRadius,
        int seed)
    {
        SpeciesName = speciesName;
        _agent = agent;
        _resource = resource;
        _renderers = GetComponentsInChildren<Renderer>(true);
        _home = home;
        _wanderRadius = Mathf.Max(3f, wanderRadius);
        _seed = seed;
        _nextDecisionTime = Time.time + 1f + Hash01(_seed++) * 2f;
    }

    private void Update()
    {
        if (_resource == null || _agent == null)
        {
            return;
        }

        bool shouldHide = _resource.IsDepleted;
        if (shouldHide != _hidden)
        {
            _hidden = shouldHide;
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = !_hidden;
                }
            }

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = !_hidden;
            }

            if (_hidden)
            {
                _agent.Stop();
                _nextDecisionTime = Time.time + 2f;
            }
        }

        if (_hidden || _agent.IsMoving || Time.time < _nextDecisionTime)
        {
            return;
        }

        float angle = Hash01(_seed++) * Mathf.PI * 2f;
        float distance = Mathf.Lerp(_wanderRadius * 0.25f, _wanderRadius, Hash01(_seed++));
        Vector3 destination = _home + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
        if (!_agent.SetDestination(destination))
        {
            _nextDecisionTime = Time.time + 1.2f;
            return;
        }

        _nextDecisionTime = Time.time + 4f + Hash01(_seed++) * 4f;
    }

    private static float Hash01(int value)
    {
        unchecked
        {
            uint hash = (uint)value;
            hash ^= hash >> 16;
            hash *= 0x7feb352d;
            hash ^= hash >> 15;
            hash *= 0x846ca68b;
            hash ^= hash >> 16;
            return (hash & 0x00ffffff) / 16777215f;
        }
    }
}
}
