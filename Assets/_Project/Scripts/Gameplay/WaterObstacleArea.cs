using Hollowwest.Navigation;
using UnityEngine;

namespace Hollowwest.Gameplay
{

/// <summary>
/// An invisible, persistent gameplay boundary for authored water.
/// The collider is intentionally shared by building and road placement,
/// while the navigation grid is blocked once during map bootstrap.
/// </summary>
public sealed class WaterObstacleArea : MonoBehaviour
{
    private const float ColliderHeight = 0.35f;
    private const float ColliderCenterY = 0.12f;

    private BoxCollider _collider;

    public Bounds WorldBounds => _collider != null
        ? _collider.bounds
        : new Bounds(transform.position, Vector3.zero);

    public static WaterObstacleArea Create(
        Transform parent,
        string objectName,
        Vector3 center,
        Vector2 size,
        GridNavigationService navigation,
        float navigationClearance = 0.8f)
    {
        GameObject owner = new(string.IsNullOrWhiteSpace(objectName)
            ? "Water Gameplay Obstacle"
            : objectName);
        owner.transform.SetParent(parent, false);

        WaterObstacleArea area = owner.AddComponent<WaterObstacleArea>();
        area.Configure(center, size, navigation, navigationClearance);
        return area;
    }

    public void Configure(
        Vector3 center,
        Vector2 size,
        GridNavigationService navigation,
        float navigationClearance = 0.8f)
    {
        transform.position = new Vector3(center.x, 0f, center.z);

        _collider = GetComponent<BoxCollider>();
        if (_collider == null)
        {
            _collider = gameObject.AddComponent<BoxCollider>();
        }

        _collider.center = new Vector3(0f, ColliderCenterY, 0f);
        _collider.size = new Vector3(
            Mathf.Max(0.1f, size.x),
            ColliderHeight,
            Mathf.Max(0.1f, size.y));
        _collider.isTrigger = false;

        navigation?.SetBlocked(_collider.bounds, Mathf.Max(0f, navigationClearance));
    }
}
}
