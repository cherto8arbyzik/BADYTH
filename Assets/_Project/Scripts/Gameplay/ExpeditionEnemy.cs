using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class ExpeditionEnemy : MonoBehaviour
{
    private ExpeditionHeroController _hero;
    private ExpeditionSceneController _sceneController;
    private Renderer[] _renderers;
    private Color[] _baseColors;
    private int _health;
    private float _attackCooldown;
    private float _hitFlash;
    private Vector3 _knockback;

    public bool IsAlive => _health > 0;

    public void Initialize(ExpeditionHeroController hero, ExpeditionSceneController sceneController, int health)
    {
        _hero = hero;
        _sceneController = sceneController;
        _health = Mathf.Max(1, health);
        _renderers = GetComponentsInChildren<Renderer>();
        _baseColors = new Color[_renderers.Length];
        for (int index = 0; index < _renderers.Length; index++)
        {
            _baseColors[index] = _renderers[index].material.color;
        }
    }

    public void TakeDamage(int damage, Vector3 direction, float force)
    {
        if (!IsAlive)
        {
            return;
        }

        _health = Mathf.Max(0, _health - Mathf.Max(0, damage));
        _hitFlash = 0.12f;
        _knockback += Vector3.ProjectOnPlane(direction, Vector3.up).normalized * Mathf.Max(0f, force);
        if (_health == 0)
        {
            _sceneController?.NotifyEnemyDefeated(this);
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!IsAlive || _hero == null || !_hero.IsAlive || !_sceneController.IsInAnomaly)
        {
            return;
        }

        _attackCooldown = Mathf.Max(0f, _attackCooldown - Time.deltaTime);
        UpdateFlash();

        if (_knockback.sqrMagnitude > 0.002f)
        {
            transform.position += _knockback * Time.deltaTime;
            _knockback = Vector3.Lerp(_knockback, Vector3.zero, Time.deltaTime * 7f);
        }

        Vector3 direction = _hero.transform.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;
        if (distance > 1.55f)
        {
            Vector3 movement = direction.normalized * (2.35f * Time.deltaTime);
            transform.position += movement;
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
        else if (_attackCooldown <= 0f)
        {
            _hero.TakeDamage(12);
            _attackCooldown = 1.05f;
        }
    }

    private void UpdateFlash()
    {
        if (_renderers == null)
        {
            return;
        }

        _hitFlash = Mathf.Max(0f, _hitFlash - Time.deltaTime);
        for (int index = 0; index < _renderers.Length; index++)
        {
            _renderers[index].material.color = _hitFlash > 0f ? Color.white : _baseColors[index];
        }
    }
}

}
