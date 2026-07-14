using Hollowwest.Navigation;
using Hollowwest.Presentation;
using Hollowwest.Selection;
using UnityEngine;

namespace Hollowwest.Prototype;

public sealed class PrototypeBootstrap : MonoBehaviour
{
    private static readonly Color GroundColor = new(0.20f, 0.24f, 0.18f);
    private static readonly Color ObstacleColor = new(0.28f, 0.25f, 0.23f);
    private static readonly Color UnitColor = new(0.30f, 0.48f, 0.68f);
    private static readonly Color SelectionColor = new(0.30f, 1f, 0.45f, 0.72f);
    private static readonly Color CommandColor = new(1f, 0.72f, 0.20f, 0.85f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfNeeded()
    {
        if (FindFirstObjectByType<PrototypeBootstrap>() != null)
        {
            return;
        }

        GameObject root = new("PrototypeRoot");
        root.AddComponent<PrototypeBootstrap>().Build();
    }

    private void Build()
    {
        Application.targetFrameRate = 60;

        Material groundMaterial = CreateMaterial(GroundColor);
        Material obstacleMaterial = CreateMaterial(ObstacleColor);
        Material unitMaterial = CreateMaterial(UnitColor);
        Material selectionMaterial = CreateMaterial(SelectionColor, true);
        Material commandMaterial = CreateMaterial(CommandColor, true);

        Bounds playBounds = new(Vector3.zero, new Vector3(24f, 1f, 18f));
        CreateGround(groundMaterial);
        Camera worldCamera = CreateCamera(playBounds);
        CreateLighting();

        GridNavigationService navigation = new(
            new Vector3(playBounds.min.x, 0f, playBounds.min.z),
            32,
            24,
            0.75f);

        CreateObstacle(new Vector3(-2f, 0.75f, 0f), new Vector3(2f, 1.5f, 7f), obstacleMaterial, navigation);
        CreateObstacle(new Vector3(4f, 0.75f, 4f), new Vector3(5f, 1.5f, 1.5f), obstacleMaterial, navigation);
        CreateObstacle(new Vector3(4f, 0.75f, -4f), new Vector3(5f, 1.5f, 1.5f), obstacleMaterial, navigation);

        CreateSquad(unitMaterial, selectionMaterial, navigation);

        SelectionController selection = gameObject.AddComponent<SelectionController>();
        selection.Initialize(worldCamera, commandMaterial);

        PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
        hud.Initialize(selection);
    }

    private void CreateGround(Material material)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(transform);
        ground.transform.position = new Vector3(0f, -0.25f, 0f);
        ground.transform.localScale = new Vector3(24f, 0.5f, 18f);
        ground.GetComponent<Renderer>().sharedMaterial = material;
        ground.AddComponent<GroundSurface>();
    }

    private Camera CreateCamera(Bounds playBounds)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(transform);

        Camera worldCamera = cameraObject.AddComponent<Camera>();
        worldCamera.clearFlags = CameraClearFlags.SolidColor;
        worldCamera.backgroundColor = new Color(0.055f, 0.045f, 0.06f);
        worldCamera.fieldOfView = 48f;
        worldCamera.nearClipPlane = 0.1f;
        worldCamera.farClipPlane = 200f;

        RtsCameraController controller = cameraObject.AddComponent<RtsCameraController>();
        controller.Initialize(Vector3.zero, playBounds);
        return worldCamera;
    }

    private void CreateLighting()
    {
        GameObject lightObject = new("Frontier Sun");
        lightObject.transform.SetParent(transform);
        lightObject.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

        Light sunlight = lightObject.AddComponent<Light>();
        sunlight.type = LightType.Directional;
        sunlight.color = new Color(1f, 0.84f, 0.66f);
        sunlight.intensity = 1.35f;
        sunlight.shadows = LightShadows.Soft;

        RenderSettings.ambientLight = new Color(0.24f, 0.25f, 0.30f);
    }

    private void CreateObstacle(
        Vector3 position,
        Vector3 scale,
        Material material,
        GridNavigationService navigation)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "Rock Formation";
        obstacle.transform.SetParent(transform);
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        obstacle.GetComponent<Renderer>().sharedMaterial = material;

        Physics.SyncTransforms();
        navigation.SetBlocked(obstacle.GetComponent<Collider>().bounds, 0.38f);
    }

    private void CreateSquad(
        Material unitMaterial,
        Material selectionMaterial,
        GridNavigationService navigation)
    {
        Vector3[] positions =
        {
            new(-8.0f, 0.78f, -2.0f),
            new(-6.7f, 0.78f, -2.0f),
            new(-8.0f, 0.78f, -0.6f),
            new(-6.7f, 0.78f, -0.6f),
            new(-8.0f, 0.78f, 0.8f),
            new(-6.7f, 0.78f, 0.8f)
        };

        for (int index = 0; index < positions.Length; index++)
        {
            GameObject unit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unit.name = $"Posse {index + 1}";
            unit.transform.SetParent(transform);
            unit.transform.position = positions[index];
            unit.transform.localScale = new Vector3(0.72f, 0.78f, 0.72f);

            Material individualMaterial = new(unitMaterial);
            individualMaterial.color = Color.Lerp(UnitColor, Color.white, index * 0.035f);
            unit.GetComponent<Renderer>().sharedMaterial = individualMaterial;

            SelectableUnit selectable = unit.AddComponent<SelectableUnit>();
            selectable.Initialize(selectionMaterial);

            NavigationAgent agent = unit.AddComponent<NavigationAgent>();
            agent.Initialize(navigation);
        }
    }

    private static Material CreateMaterial(Color color, bool transparent = false)
    {
        Shader shader = Shader.Find("Standard");
        Material material = new(shader)
        {
            color = color,
            hideFlags = HideFlags.DontSave
        };

        if (transparent)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        return material;
    }
}
