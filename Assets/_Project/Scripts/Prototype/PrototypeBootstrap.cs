using Hollowwest.Economy;
using Hollowwest.Gameplay;
using Hollowwest.Navigation;
using Hollowwest.Presentation;
using Hollowwest.Selection;
using UnityEngine;

namespace Hollowwest.Prototype
{

public sealed class PrototypeBootstrap : MonoBehaviour
{
    private static readonly Color GroundColor = new(0.20f, 0.24f, 0.18f);
    private static readonly Color ObstacleColor = new(0.28f, 0.25f, 0.23f);
    private static readonly Color UnitColor = new(0.30f, 0.48f, 0.68f);
    private static readonly Color SelectionColor = new(0.30f, 1f, 0.45f, 0.72f);
    private static readonly Color CommandColor = new(1f, 0.72f, 0.20f, 0.85f);
    private static readonly Color WoodColor = new(0.42f, 0.28f, 0.15f);
    private static readonly Color CoreColor = new(0.48f, 0.38f, 0.24f);
    private static readonly Color EnemyColor = new(0.58f, 0.18f, 0.24f);
    private static readonly Color HeroColor = new(0.82f, 0.68f, 0.38f);
    private static readonly Color PawnColor = new(0.34f, 0.48f, 0.58f);
    private static readonly Color RuinWoodColor = new(0.30f, 0.20f, 0.13f);
    private static readonly Color AshColor = new(0.10f, 0.09f, 0.08f);
    private static readonly Color EmberColor = new(1.00f, 0.38f, 0.12f);

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
        Material selectionMaterial = CreateMaterial(SelectionColor, true);
        Material commandMaterial = CreateMaterial(CommandColor, true);
        Material woodMaterial = CreateMaterial(WoodColor);
        Material coreMaterial = CreateMaterial(CoreColor);
        Material enemyMaterial = CreateMaterial(EnemyColor);
        Material heroMaterial = CreateMaterial(HeroColor);
        Material pawnMaterial = CreateMaterial(PawnColor);
        Material ruinMaterial = CreateMaterial(RuinWoodColor);
        Material ashMaterial = CreateMaterial(AshColor);
        Material emberMaterial = CreateMaterial(EmberColor);
        ResourceStockpile stockpile = gameObject.AddComponent<ResourceStockpile>();

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

        CampCore campCore = CreateCampCore(coreMaterial, navigation);
        CreateRuinedBase(ruinMaterial, ashMaterial, emberMaterial, navigation);
        CreateResourceNode(new Vector3(8f, 0.5f, 0f), woodMaterial, navigation);
        CreateResourceNode(new Vector3(-8f, 0.5f, 5f), woodMaterial, navigation);
        CreateResourceNode(new Vector3(0f, 0.5f, -6f), woodMaterial, navigation);

        CreateSquad(heroMaterial, pawnMaterial, selectionMaterial, navigation, stockpile);

        SelectionController selection = gameObject.AddComponent<SelectionController>();
        selection.Initialize(worldCamera, commandMaterial);

        WaveDirector waveDirector = gameObject.AddComponent<WaveDirector>();
        waveDirector.Initialize(navigation, campCore, enemyMaterial);

        GameSession session = gameObject.AddComponent<GameSession>();
        session.Initialize(waveDirector, campCore);

        PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
        hud.Initialize(selection, stockpile, session);
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

    private CampCore CreateCampCore(Material material, GridNavigationService navigation)
    {
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
        core.name = "Broken Shrine Core";
        core.transform.SetParent(transform);
        core.transform.position = new Vector3(0f, 0.65f, 0f);
        core.transform.localScale = new Vector3(1.45f, 1.3f, 1.45f);
        core.GetComponent<Renderer>().sharedMaterial = material;

        Physics.SyncTransforms();
        navigation.SetBlocked(core.GetComponent<Collider>().bounds, 0.1f);
        return core.AddComponent<CampCore>();
    }

    private void CreateRuinedBase(
        Material ruinMaterial,
        Material ashMaterial,
        Material emberMaterial,
        GridNavigationService navigation)
    {
        GameObject baseRoot = new("Ruined Zastava");
        baseRoot.transform.SetParent(transform);

        CreateRuinBlock(baseRoot.transform, "Collapsed Hall", new Vector3(-3.6f, 0.55f, 2.4f), new Vector3(2.9f, 1.1f, 1.7f), 8f, ruinMaterial, navigation, true);
        CreateRuinBlock(baseRoot.transform, "Burned Storehouse", new Vector3(3.3f, 0.5f, 2.8f), new Vector3(2.4f, 1.0f, 1.4f), -14f, ruinMaterial, navigation, true);
        CreateRuinBlock(baseRoot.transform, "Fallen Gate Beam", new Vector3(0f, 0.35f, -4.3f), new Vector3(4.8f, 0.45f, 0.45f), 4f, ruinMaterial, navigation, true);

        CreateRuinBlock(baseRoot.transform, "North Palisade Remnant", new Vector3(-4.7f, 0.55f, 5.1f), new Vector3(3.2f, 1.1f, 0.38f), 0f, ruinMaterial, navigation, true);
        CreateRuinBlock(baseRoot.transform, "North Palisade Remnant", new Vector3(4.6f, 0.55f, 5.1f), new Vector3(3.0f, 1.1f, 0.38f), 0f, ruinMaterial, navigation, true);
        CreateRuinBlock(baseRoot.transform, "West Palisade Remnant", new Vector3(-6.3f, 0.55f, -1.2f), new Vector3(0.38f, 1.1f, 4.2f), -6f, ruinMaterial, navigation, true);
        CreateRuinBlock(baseRoot.transform, "East Palisade Remnant", new Vector3(6.2f, 0.55f, -1.0f), new Vector3(0.38f, 1.1f, 4.0f), 8f, ruinMaterial, navigation, true);

        CreateRuinBlock(baseRoot.transform, "Ash Patch", new Vector3(0.1f, 0.03f, 2.0f), new Vector3(2.3f, 0.06f, 2.0f), 0f, ashMaterial, navigation, false);
        CreateRuinBlock(baseRoot.transform, "Supply Crate", new Vector3(-1.9f, 0.28f, -2.0f), new Vector3(0.7f, 0.55f, 0.7f), 12f, ruinMaterial, navigation, true);
        CreateRuinBlock(baseRoot.transform, "Supply Crate", new Vector3(-2.7f, 0.22f, -1.3f), new Vector3(0.6f, 0.45f, 0.6f), -16f, ruinMaterial, navigation, true);
        CreateCampfire(baseRoot.transform, emberMaterial);
    }

    private void CreateRuinBlock(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        float yaw,
        Material material,
        GridNavigationService navigation,
        bool blocksNavigation)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent);
        block.transform.position = position;
        block.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;

        if (!blocksNavigation)
        {
            block.GetComponent<Collider>().enabled = false;
            return;
        }

        Physics.SyncTransforms();
        navigation.SetBlocked(block.GetComponent<Collider>().bounds, 0.18f);
    }

    private void CreateCampfire(Transform parent, Material emberMaterial)
    {
        GameObject fire = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fire.name = "Weak Campfire";
        fire.transform.SetParent(parent);
        fire.transform.position = new Vector3(0.15f, 0.18f, -1.9f);
        fire.transform.localScale = new Vector3(0.55f, 0.18f, 0.55f);
        fire.GetComponent<Collider>().enabled = false;
        fire.GetComponent<Renderer>().sharedMaterial = emberMaterial;

        Light light = fire.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.42f, 0.18f);
        light.intensity = 1.3f;
        light.range = 5.5f;
    }

    private void CreateResourceNode(
        Vector3 position,
        Material material,
        GridNavigationService navigation)
    {
        GameObject resource = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        resource.name = "Wood Cache";
        resource.transform.SetParent(transform);
        resource.transform.position = position;
        resource.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        resource.GetComponent<Renderer>().sharedMaterial = new Material(material);
        resource.AddComponent<ResourceNode>();

        Physics.SyncTransforms();
        navigation.SetBlocked(resource.GetComponent<Collider>().bounds, 0.2f);
    }

    private void CreateSquad(
        Material heroMaterial,
        Material pawnMaterial,
        Material selectionMaterial,
        GridNavigationService navigation,
        ResourceStockpile stockpile)
    {
        Vector3[] positions =
        {
            new(-1.2f, 0.9f, -2.0f),
            new(-2.6f, 0.78f, -2.7f),
            new(0.2f, 0.78f, -2.8f),
            new(-3.0f, 0.78f, -0.8f),
            new(1.2f, 0.78f, -0.9f),
            new(-0.9f, 0.78f, -3.8f)
        };

        for (int index = 0; index < positions.Length; index++)
        {
            GameObject unit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bool isHero = index == 0;
            unit.name = isHero ? "Main Hero" : $"Pawn {index}";
            unit.transform.SetParent(transform);
            unit.transform.position = positions[index];
            unit.transform.localScale = isHero
                ? new Vector3(0.88f, 0.95f, 0.88f)
                : new Vector3(0.68f, 0.74f, 0.68f);

            Material individualMaterial = new(isHero ? heroMaterial : pawnMaterial);
            if (!isHero)
            {
                individualMaterial.color = Color.Lerp(PawnColor, Color.white, index * 0.025f);
            }
            unit.GetComponent<Renderer>().sharedMaterial = individualMaterial;

            SelectableUnit selectable = unit.AddComponent<SelectableUnit>();
            selectable.Initialize(selectionMaterial);

            NavigationAgent agent = unit.AddComponent<NavigationAgent>();
            agent.Initialize(navigation);

            ResourceCollector collector = unit.AddComponent<ResourceCollector>();
            collector.Initialize(stockpile);

            unit.AddComponent<UnitCombat>();
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
}
