using UnityEngine;

namespace Hollowwest.Gameplay
{

/// <summary>
/// Physical town-side entrance to the run scene. The short cooldown prevents a
/// failed resource check from being repeated every frame while the hero stands
/// on the ladder marker.
/// </summary>
public sealed class ExpeditionEntrance : MonoBehaviour
{
    private Transform _hero;
    private ExpeditionSystem _expedition;
    private TextMesh _label;
    private float _retryCooldown;

    public void Initialize(Transform hero, ExpeditionSystem expedition, TextMesh label)
    {
        _hero = hero;
        _expedition = expedition;
        _label = label;
    }

    private void Update()
    {
        _retryCooldown = Mathf.Max(0f, _retryCooldown - Time.deltaTime);
        if (_hero == null || _expedition == null || _expedition.IsActive || _retryCooldown > 0f)
        {
            return;
        }

        Vector3 offset = _hero.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= 2.4f * 2.4f)
        {
            _expedition.TryStart(out _);
            _retryCooldown = 2f;
        }
    }

    private void LateUpdate()
    {
        if (_label == null || Camera.main == null)
        {
            return;
        }

        _label.transform.rotation = Quaternion.LookRotation(
            _label.transform.position - Camera.main.transform.position,
            Vector3.up);
    }
}

}
