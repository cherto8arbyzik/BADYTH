using UnityEngine;

namespace Hollowwest.Presentation
{

public sealed class RtsCameraController : MonoBehaviour
{
    private Vector3 _pivot;
    private Bounds _movementBounds;
    private float _distance = 36f;

    public void Initialize(Vector3 pivot, Bounds movementBounds)
    {
        _pivot = pivot;
        _movementBounds = movementBounds;
        transform.rotation = Quaternion.Euler(57f, -35f, 0f);
        ApplyTransform();
    }

    private void Update()
    {
        Vector2 input = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            input.y += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            input.y -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            input.x += 1f;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            input.x -= 1f;
        }

        if (input.sqrMagnitude > 0f)
        {
            input.Normalize();
            float panSpeed = Mathf.Lerp(9f, 19f, Mathf.InverseLerp(12f, 58f, _distance));
            _pivot += new Vector3(input.x, 0f, input.y) * (panSpeed * Time.deltaTime);
            _pivot.x = Mathf.Clamp(_pivot.x, _movementBounds.min.x + 3f, _movementBounds.max.x - 3f);
            _pivot.z = Mathf.Clamp(_pivot.z, _movementBounds.min.z + 3f, _movementBounds.max.z - 3f);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _distance = Mathf.Clamp(_distance - scroll * 2.4f, 12f, 58f);
        }

        ApplyTransform();
    }

    private void ApplyTransform()
    {
        transform.position = _pivot - transform.forward * _distance;
    }
}
}
