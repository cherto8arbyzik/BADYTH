using System.Collections.Generic;
using Hollowwest.Core;
using UnityEngine;

namespace Hollowwest.Navigation
{

public sealed class NavigationAgent : MonoBehaviour
{
    private static readonly List<NavigationAgent> ActiveAgentsInternal = new();

    [SerializeField] private float speed = 4f;
    [SerializeField] private float stoppingDistance = 0.12f;
    [SerializeField] private float separationRadius = 1.1f;
    [SerializeField] private float separationWeight = 0.8f;

    private readonly List<Vector3> _path = new();
    private INavigationService _navigation;
    private int _waypointIndex;

    public bool IsMoving => _waypointIndex < _path.Count;
    public float Speed
    {
        get => speed;
        set => speed = Mathf.Max(0f, value);
    }

    public void Initialize(INavigationService navigation)
    {
        _navigation = navigation;
    }

    public bool SetDestination(Vector3 destination)
    {
        _waypointIndex = 0;
        if (_navigation == null || !_navigation.TryFindPath(transform.position, destination, _path))
        {
            _path.Clear();
            return false;
        }

        return true;
    }

    public void Stop()
    {
        _path.Clear();
        _waypointIndex = 0;
    }

    private void OnEnable()
    {
        if (!ActiveAgentsInternal.Contains(this))
        {
            ActiveAgentsInternal.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveAgentsInternal.Remove(this);
    }

    private void Update()
    {
        if (!IsMoving)
        {
            return;
        }

        Vector3 target = _path[_waypointIndex];
        target.y = transform.position.y;
        Vector3 toTarget = target - transform.position;

        if (toTarget.sqrMagnitude <= stoppingDistance * stoppingDistance)
        {
            _waypointIndex++;
            if (!IsMoving)
            {
                return;
            }

            target = _path[_waypointIndex];
            target.y = transform.position.y;
            toTarget = target - transform.position;
        }

        Vector3 desired = toTarget.normalized;
        Vector3 separation = CalculateSeparation();
        Vector3 direction = desired + separation * separationWeight;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = desired;
        }

        direction.Normalize();
        transform.position = Vector3.MoveTowards(
            transform.position,
            transform.position + direction,
            speed * Time.deltaTime);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, facing, 12f * Time.deltaTime);
        }
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 separation = Vector3.zero;
        float radiusSquared = separationRadius * separationRadius;

        foreach (NavigationAgent other in ActiveAgentsInternal)
        {
            if (other == this || other == null)
            {
                continue;
            }

            Vector3 offset = transform.position - other.transform.position;
            offset.y = 0f;
            float distanceSquared = offset.sqrMagnitude;

            if (distanceSquared <= 0.0001f || distanceSquared >= radiusSquared)
            {
                continue;
            }

            float distance = Mathf.Sqrt(distanceSquared);
            separation += offset / distance * (1f - distance / separationRadius);
        }

        return separation;
    }
}
}
