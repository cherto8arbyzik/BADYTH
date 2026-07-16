using Hollowwest.Controls;
using UnityEngine;

namespace Hollowwest.Presentation
{

public sealed class RtsCameraController : MonoBehaviour
{
    private const float MinZoomDistance = 11f;
    private const float MaxZoomDistance = 72f;

    private Vector3 _pivot;
    private Bounds _movementBounds;
    private float _distance = 27f;
    private float _yaw = -35f;
    private float _pitch = 48f;
    private Transform _focusTarget;
    private Vector3 _savedPivot;
    private float _savedDistance;
    private float _savedYaw;
    private float _savedPitch;

    public bool IsFocused => _focusTarget != null;

    public void Initialize(Vector3 pivot, Bounds movementBounds)
    {
        _pivot = pivot;
        _movementBounds = movementBounds;
        ApplyTransform();
    }

    private void Update()
    {
        if (_focusTarget != null)
        {
            _pivot = _focusTarget.position + Vector3.up * 0.75f;
            ApplyTransform();
            return;
        }

        GameInputRouter inputRouter = GameInputRouter.Instance;
        Vector2 input = inputRouter != null && inputRouter.Context == PlayerControlContext.Settlement
            ? inputRouter.SettlementMove.ReadValue<Vector2>()
            : ReadLegacyMovement();

        if (input.sqrMagnitude > 0f)
        {
            input.Normalize();
            float panSpeed = Mathf.Lerp(9f, 27f, Mathf.InverseLerp(MinZoomDistance, MaxZoomDistance, _distance));
            Vector3 cameraForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 panDirection = cameraRight * input.x + cameraForward * input.y;
            if (panDirection.sqrMagnitude > 1f)
            {
                panDirection.Normalize();
            }

            _pivot += panDirection * (panSpeed * Time.deltaTime);
            _pivot.x = Mathf.Clamp(_pivot.x, _movementBounds.min.x + 3f, _movementBounds.max.x - 3f);
            _pivot.z = Mathf.Clamp(_pivot.z, _movementBounds.min.z + 3f, _movementBounds.max.z - 3f);
        }

        float scroll = inputRouter != null && inputRouter.Context == PlayerControlContext.Settlement
            ? inputRouter.SettlementZoom.ReadValue<float>()
            : Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _distance = Mathf.Clamp(_distance - Mathf.Sign(scroll) * 3.2f, MinZoomDistance, MaxZoomDistance);
        }

        bool orbiting = inputRouter != null && inputRouter.Context == PlayerControlContext.Settlement
            ? inputRouter.SettlementOrbit.IsPressed()
            : Input.GetMouseButton(2);
        if (orbiting)
        {
            Vector2 lookDelta = inputRouter != null && inputRouter.Context == PlayerControlContext.Settlement
                ? inputRouter.SettlementLookDelta.ReadValue<Vector2>()
                : new Vector2(Input.GetAxisRaw("Mouse X") * 18f, Input.GetAxisRaw("Mouse Y") * 18f);
            _yaw += lookDelta.x * 0.18f;
            _pitch = Mathf.Clamp(_pitch - lookDelta.y * 0.15f, 28f, 76f);
        }

        ApplyTransform();
    }

    private static Vector2 ReadLegacyMovement()
    {
        Vector2 input = Vector2.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
        return input;
    }

    public void BeginFocus(Transform target, float distance = 11f)
    {
        if (target == null)
        {
            return;
        }

        if (_focusTarget == null)
        {
            _savedPivot = _pivot;
            _savedDistance = _distance;
            _savedYaw = _yaw;
            _savedPitch = _pitch;
        }

        _focusTarget = target;
        _distance = Mathf.Clamp(distance, 7f, 18f);
        _pitch = Mathf.Clamp(_pitch, 34f, 55f);
    }

    public void EndFocus()
    {
        if (_focusTarget == null)
        {
            return;
        }

        _focusTarget = null;
        _pivot = _savedPivot;
        _distance = _savedDistance;
        _yaw = _savedYaw;
        _pitch = _savedPitch;
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = _pivot - transform.forward * _distance;
    }
}
}
