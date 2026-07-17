using Hollowwest.Controls;
using UnityEngine;

namespace Hollowwest.Gameplay
{

[RequireComponent(typeof(CharacterController))]
public sealed class ExpeditionHeroController : MonoBehaviour
{
    private const float MoveSpeed = 6.2f;
    private const float AttackCooldown = 0.42f;

    private CharacterController _characterController;
    private Camera _worldCamera;
    private ExpeditionSceneController _sceneController;
    private ExpeditionBackpack _backpack;
    private Vector3 _movementCenter;
    private Vector2 _movementRadii;
    private float _attackCooldown;
    private float _dodgeCooldown;
    private float _invulnerability;
    private Vector3 _lastMoveDirection = Vector3.forward;
    private Transform _weaponPivot;
    private Quaternion _weaponRestRotation;
    private float _swingElapsed = -1f;
    private float _swingDuration;
    private bool _heavySwing;

    public int MaxHealth { get; private set; } = 100;
    public int Health { get; private set; } = 100;
    public ExpeditionBackpack Backpack => _backpack;
    public bool IsAlive => Health > 0;

    public void Initialize(
        Camera worldCamera,
        ExpeditionSceneController sceneController,
        ExpeditionBackpack backpack,
        Vector3 movementCenter,
        Vector2 movementRadii,
        Transform weaponPivot)
    {
        _characterController = GetComponent<CharacterController>();
        _worldCamera = worldCamera;
        _sceneController = sceneController;
        _backpack = backpack;
        _weaponPivot = weaponPivot;
        _weaponRestRotation = weaponPivot != null ? weaponPivot.localRotation : Quaternion.identity;
        SetMovementBounds(movementCenter, movementRadii);
    }

    public void SetMovementBounds(Vector3 center, Vector2 radii)
    {
        _movementCenter = center;
        _movementRadii = new Vector2(Mathf.Max(3f, radii.x), Mathf.Max(3f, radii.y));
    }

    public void Teleport(Vector3 position)
    {
        _characterController.enabled = false;
        transform.position = position;
        _characterController.enabled = true;
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive || _invulnerability > 0f)
        {
            return;
        }

        Health = Mathf.Max(0, Health - Mathf.Max(0, damage));
        _invulnerability = 0.55f;
        _sceneController?.ShowMessage($"Получен урон: {damage}", 1.1f);
        if (Health == 0)
        {
            _sceneController?.HandleHeroDefeated();
        }
    }

    private void Update()
    {
        GameInputRouter input = GameInputRouter.Instance;
        if (input == null || input.Context != PlayerControlContext.Hero || !IsAlive)
        {
            return;
        }

        _attackCooldown = Mathf.Max(0f, _attackCooldown - Time.deltaTime);
        _dodgeCooldown = Mathf.Max(0f, _dodgeCooldown - Time.deltaTime);
        _invulnerability = Mathf.Max(0f, _invulnerability - Time.deltaTime);

        Vector2 rawMove = input.HeroMove.ReadValue<Vector2>();
        Vector3 movement = GetCameraRelativeMovement(rawMove);
        if (movement.sqrMagnitude > 0.01f)
        {
            _lastMoveDirection = movement.normalized;
            _characterController.Move(_lastMoveDirection * (MoveSpeed * Time.deltaTime));
        }

        ClampToMovementBounds();
        UpdateAim(input.HeroAimPosition.ReadValue<Vector2>());

        if (input.HeroDodge.WasPressedThisFrame() && _dodgeCooldown <= 0f)
        {
            Vector3 dodgeDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : _lastMoveDirection;
            _characterController.Move(dodgeDirection * 3.4f);
            ClampToMovementBounds();
            _dodgeCooldown = 1f;
            _invulnerability = Mathf.Max(_invulnerability, 0.35f);
        }

        if (input.HeroAttack.WasPressedThisFrame())
        {
            Attack(false);
        }
        else if (input.HeroSecondary.WasPressedThisFrame())
        {
            Attack(true);
        }

        if (input.HeroInteract.WasPressedThisFrame())
        {
            _sceneController?.TryInteract(this);
        }

        UpdateWeaponAnimation();
    }

    private Vector3 GetCameraRelativeMovement(Vector2 rawMove)
    {
        if (_worldCamera == null || rawMove.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        Vector3 forward = Vector3.ProjectOnPlane(_worldCamera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(_worldCamera.transform.right, Vector3.up).normalized;
        Vector3 movement = right * rawMove.x + forward * rawMove.y;
        return movement.sqrMagnitude > 1f ? movement.normalized : movement;
    }

    private void UpdateAim(Vector2 screenPosition)
    {
        if (_worldCamera == null)
        {
            return;
        }

        Ray ray = _worldCamera.ScreenPointToRay(screenPosition);
        Plane plane = new(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        if (!plane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector3 direction = ray.GetPoint(distance) - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.04f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void Attack(bool heavy)
    {
        float requiredCooldown = heavy ? 0.8f : AttackCooldown;
        if (_attackCooldown > 0f)
        {
            return;
        }

        _attackCooldown = requiredCooldown;
        _swingElapsed = 0f;
        _swingDuration = heavy ? 0.46f : 0.28f;
        _heavySwing = heavy;
        int damage = heavy ? 52 : 34;
        float radius = heavy ? 2.15f : 1.55f;
        Vector3 center = transform.position + transform.forward * (heavy ? 1.25f : 1.05f) + Vector3.up * 0.8f;
        Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            ExpeditionEnemy enemy = hit.GetComponentInParent<ExpeditionEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, transform.forward, heavy ? 1.8f : 0.8f);
            }
        }

        _sceneController?.ShowAttackPulse(center, radius, heavy);
    }

    private void UpdateWeaponAnimation()
    {
        if (_weaponPivot == null || _swingElapsed < 0f)
        {
            return;
        }

        _swingElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_swingElapsed / Mathf.Max(0.01f, _swingDuration));
        float endAngle = _heavySwing ? 112f : 78f;
        float sweep = progress < 0.78f
            ? Mathf.SmoothStep(-92f, endAngle, progress / 0.78f)
            : Mathf.SmoothStep(endAngle, 0f, (progress - 0.78f) / 0.22f);
        _weaponPivot.localRotation = _weaponRestRotation * Quaternion.Euler(_heavySwing ? 16f : 8f, sweep, 0f);
        if (progress >= 1f)
        {
            _weaponPivot.localRotation = _weaponRestRotation;
            _swingElapsed = -1f;
        }
    }

    private void ClampToMovementBounds()
    {
        Vector3 position = transform.position;
        Vector2 offset = new(
            (position.x - _movementCenter.x) / _movementRadii.x,
            (position.z - _movementCenter.z) / _movementRadii.y);
        if (offset.sqrMagnitude > 1f)
        {
            offset.Normalize();
            position.x = _movementCenter.x + offset.x * _movementRadii.x;
            position.z = _movementCenter.z + offset.y * _movementRadii.y;
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;
        }

        if (transform.position.y < -0.1f || transform.position.y > 0.25f)
        {
            position = transform.position;
            position.y = 0.05f;
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;
        }
    }
}

public sealed class ExpeditionCameraController : MonoBehaviour
{
    private Transform _target;
    private Vector3 _velocity;
    private readonly Quaternion _rotation = Quaternion.Euler(52f, -38f, 0f);

    public void Initialize(Transform target)
    {
        _target = target;
        transform.rotation = _rotation;
        Snap();
    }

    public void Snap()
    {
        if (_target == null)
        {
            return;
        }

        transform.rotation = _rotation;
        transform.position = _target.position + Vector3.up * 1.1f - transform.forward * 18f;
        _velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 desired = _target.position + Vector3.up * 1.1f - transform.forward * 18f;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, 0.12f);
    }
}

}
