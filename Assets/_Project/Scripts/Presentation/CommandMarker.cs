using UnityEngine;

namespace Hollowwest.Presentation
{

public sealed class CommandMarker : MonoBehaviour
{
    private const float Lifetime = 0.65f;

    private Renderer _renderer;
    private float _remaining = Lifetime;

    public static void Spawn(Vector3 position, Material material)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "MoveCommand";
        marker.transform.position = position + Vector3.up * 0.035f;
        marker.transform.localScale = new Vector3(0.42f, 0.025f, 0.42f);
        marker.GetComponent<Collider>().enabled = false;
        marker.GetComponent<Renderer>().sharedMaterial = material;

        CommandMarker commandMarker = marker.AddComponent<CommandMarker>();
        commandMarker._renderer = marker.GetComponent<Renderer>();
    }

    private void Update()
    {
        _remaining -= Time.deltaTime;
        float normalized = Mathf.Clamp01(_remaining / Lifetime);
        transform.localScale = new Vector3(0.42f + (1f - normalized) * 0.5f, 0.025f, 0.42f + (1f - normalized) * 0.5f);

        if (_renderer != null)
        {
            Color color = _renderer.material.color;
            color.a = normalized;
            _renderer.material.color = color;
        }

        if (_remaining <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
}
