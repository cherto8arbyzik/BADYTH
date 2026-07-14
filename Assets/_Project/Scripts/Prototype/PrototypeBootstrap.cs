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
    private const string VillageModelPath = "Models/Quaternius/MedievalVillage/";

    private static readonly Color GroundColor = new(0.20f, 0.28f, 0.18f);
    private static readonly Color GroundEdgeColor = new(0.13f, 0.18f, 0.14f);
    private static readonly Color DirtColor = new(0.33f, 0.25f, 0.17f);
    private static readonly Color PathColor = new(0.42f, 0.31f, 0.20f);
    private static readonly Color StoneColor = new(0.33f, 0.36f, 0.34f);
    private static readonly Color WoodColor = new(0.31f, 0.19f, 0.10f);
    private static readonly Color LightWoodColor = new(0.52f, 0.32f, 0.14f);
    private static readonly Color BurnedWoodColor = new(0.11f, 0.09f, 0.08f);
    private static readonly Color AshColor = new(0.10f, 0.09f, 0.08f);
    private static readonly Color EmberColor = new(1.00f, 0.34f, 0.08f);
    private static readonly Color FoliageColor = new(0.14f, 0.31f, 0.18f);
    private static readonly Color FoliageLightColor = new(0.24f, 0.42f, 0.22f);
    private static readonly Color HeroColor = new(0.55f, 0.18f, 0.13f);
    private static readonly Color PawnColor = new(0.20f, 0.36f, 0.42f);
    private static readonly Color SkinColor = new(0.73f, 0.49f, 0.31f);
    private static readonly Color HairColor = new(0.16f, 0.10f, 0.07f);
    private static readonly Color AccentColor = new(0.76f, 0.61f, 0.28f);
    private static readonly Color SelectionColor = new(0.30f, 1f, 0.45f, 0.72f);
    private static readonly Color CommandColor = new(1f, 0.72f, 0.20f, 0.85f);
    private static readonly Color EnemyColor = new(0.32f, 0.08f, 0.13f);

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
        Material groundEdgeMaterial = CreateMaterial(GroundEdgeColor);
        Material dirtMaterial = CreateMaterial(DirtColor);
        Material pathMaterial = CreateMaterial(PathColor);
        Material stoneMaterial = CreateMaterial(StoneColor);
        Material woodMaterial = CreateMaterial(WoodColor);
        Material lightWoodMaterial = CreateMaterial(LightWoodColor);
        Material burnedWoodMaterial = CreateMaterial(BurnedWoodColor);
        Material ashMaterial = CreateMaterial(AshColor);
        Material emberMaterial = CreateMaterial(EmberColor);
        Material foliageMaterial = CreateMaterial(FoliageColor);
        Material foliageLightMaterial = CreateMaterial(FoliageLightColor);
        Material heroMaterial = CreateMaterial(HeroColor);
        Material pawnMaterial = CreateMaterial(PawnColor);
        Material skinMaterial = CreateMaterial(SkinColor);
        Material hairMaterial = CreateMaterial(HairColor);
        Material accentMaterial = CreateMaterial(AccentColor);
        Material selectionMaterial = CreateMaterial(SelectionColor, true);
        Material commandMaterial = CreateMaterial(CommandColor, true);
        Material enemyMaterial = CreateMaterial(EnemyColor);
        ResourceStockpile stockpile = gameObject.AddComponent<ResourceStockpile>();

        Bounds playBounds = new(Vector3.zero, new Vector3(32f, 1f, 24f));
        CreateGround(groundMaterial, groundEdgeMaterial, dirtMaterial, pathMaterial);
        Camera worldCamera = CreateCamera(playBounds);
        CreateLighting();

        GridNavigationService navigation = new(
            new Vector3(playBounds.min.x, 0f, playBounds.min.z),
            44,
            34,
            0.75f);

        CreateVillage(
            navigation,
            woodMaterial,
            burnedWoodMaterial,
            ashMaterial,
            foliageMaterial,
            foliageLightMaterial,
            stoneMaterial);

        CampCore campCore = CreateCampCore(stoneMaterial, woodMaterial, emberMaterial, navigation);
        CreateResourceNode(new Vector3(11.5f, 0f, 1.8f), woodMaterial, lightWoodMaterial, navigation);
        CreateResourceNode(new Vector3(-11.2f, 0f, 5.2f), woodMaterial, lightWoodMaterial, navigation);
        CreateResourceNode(new Vector3(0.5f, 0f, -9.2f), woodMaterial, lightWoodMaterial, navigation);

        CreateSquad(
            heroMaterial,
            pawnMaterial,
            skinMaterial,
            hairMaterial,
            accentMaterial,
            selectionMaterial,
            navigation,
            stockpile);

        SelectionController selection = gameObject.AddComponent<SelectionController>();
        selection.Initialize(worldCamera, commandMaterial);

        WaveDirector waveDirector = gameObject.AddComponent<WaveDirector>();
        waveDirector.Initialize(navigation, campCore, enemyMaterial);

        GameSession session = gameObject.AddComponent<GameSession>();
        session.Initialize(waveDirector, campCore);

        PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
        hud.Initialize(selection, stockpile, session);
    }

    private void CreateGround(
        Material groundMaterial,
        Material edgeMaterial,
        Material dirtMaterial,
        Material pathMaterial)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Village Ground";
        ground.transform.SetParent(transform);
        ground.transform.position = new Vector3(0f, -0.35f, 0f);
        ground.transform.localScale = new Vector3(32f, 0.7f, 24f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        ground.AddComponent<GroundSurface>();

        CreateVisualPrimitive(
            transform,
            "Dark Ground Border",
            PrimitiveType.Cube,
            new Vector3(0f, -0.56f, 0f),
            new Vector3(34f, 0.45f, 26f),
            Quaternion.identity,
            edgeMaterial,
            false);

        CreateVisualPrimitive(
            transform,
            "Village Courtyard",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.015f, 0f),
            new Vector3(4.8f, 0.018f, 4.8f),
            Quaternion.identity,
            dirtMaterial,
            false);

        CreatePath(new Vector3(-4.2f, 0.025f, 3.0f), new Vector3(6.8f, 0.04f, 1.2f), -35f, pathMaterial);
        CreatePath(new Vector3(4.5f, 0.025f, 2.8f), new Vector3(7.4f, 0.04f, 1.15f), 31f, pathMaterial);
        CreatePath(new Vector3(-4.2f, 0.025f, -3.2f), new Vector3(7.0f, 0.04f, 1.1f), 37f, pathMaterial);
        CreatePath(new Vector3(4.3f, 0.025f, -3.3f), new Vector3(7.0f, 0.04f, 1.1f), -36f, pathMaterial);
        CreatePath(new Vector3(0f, 0.03f, -7.2f), new Vector3(8.0f, 0.04f, 1.35f), 90f, pathMaterial);
    }

    private void CreatePath(Vector3 position, Vector3 scale, float yaw, Material material)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        float width = scale.z;
        float bodyLength = Mathf.Max(0.1f, scale.x - width);
        Vector3 pathDirection = rotation * Vector3.right;
        float capOffset = bodyLength * 0.5f;

        CreateVisualPrimitive(
            transform,
            "Packed Earth Path",
            PrimitiveType.Cube,
            position,
            new Vector3(bodyLength, scale.y, width),
            rotation,
            material,
            false);

        Vector3 capScale = new(width, scale.y * 0.5f, width);
        CreateVisualPrimitive(transform, "Path End", PrimitiveType.Cylinder, position + pathDirection * capOffset, capScale, Quaternion.identity, material, false);
        CreateVisualPrimitive(transform, "Path End", PrimitiveType.Cylinder, position - pathDirection * capOffset, capScale, Quaternion.identity, material, false);
    }

    private Camera CreateCamera(Bounds playBounds)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(transform);

        Camera worldCamera = cameraObject.AddComponent<Camera>();
        worldCamera.clearFlags = CameraClearFlags.SolidColor;
        worldCamera.backgroundColor = new Color(0.055f, 0.065f, 0.075f);
        worldCamera.fieldOfView = 47f;
        worldCamera.nearClipPlane = 0.1f;
        worldCamera.farClipPlane = 160f;

        RtsCameraController controller = cameraObject.AddComponent<RtsCameraController>();
        controller.Initialize(new Vector3(0f, 0f, 0.8f), playBounds);
        return worldCamera;
    }

    private void CreateLighting()
    {
        GameObject sunObject = new("Low Frontier Sun");
        sunObject.transform.SetParent(transform);
        sunObject.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

        Light sunlight = sunObject.AddComponent<Light>();
        sunlight.type = LightType.Directional;
        sunlight.color = new Color(1f, 0.79f, 0.58f);
        sunlight.intensity = 1.35f;
        sunlight.shadows = LightShadows.Soft;

        GameObject fillObject = new("Cool Sky Fill");
        fillObject.transform.SetParent(transform);
        fillObject.transform.rotation = Quaternion.Euler(55f, 145f, 0f);

        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.36f, 0.46f, 0.62f);
        fill.intensity = 0.34f;
        fill.shadows = LightShadows.None;

        RenderSettings.ambientLight = new Color(0.25f, 0.27f, 0.31f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.12f, 0.14f, 0.15f);
        RenderSettings.fogDensity = 0.013f;
    }

    private void CreateVillage(
        GridNavigationService navigation,
        Material woodMaterial,
        Material burnedWoodMaterial,
        Material ashMaterial,
        Material foliageMaterial,
        Material foliageLightMaterial,
        Material stoneMaterial)
    {
        GameObject village = new("Zastava Village");
        village.transform.SetParent(transform);

        CreateImportedBuilding(village.transform, "Elder House", "Inn", new Vector3(-7.4f, 0f, 5.4f), 145f, 5.7f, navigation);
        CreateImportedBuilding(village.transform, "Village Smithy", "Blacksmith", new Vector3(7.2f, 0f, 5.0f), -142f, 5.0f, navigation);
        CreateImportedBuilding(village.transform, "Northern Homestead", "House_2", new Vector3(-8.2f, 0f, -5.1f), 38f, 4.6f, navigation);
        CreateImportedBuilding(village.transform, "Damaged Storehouse", "Sawmill", new Vector3(7.9f, 0f, -5.2f), -35f, 5.2f, navigation);

        CreateRuinDetails(village.transform, burnedWoodMaterial, ashMaterial, navigation);
        CreatePalisade(village.transform, woodMaterial, navigation);
        CreateVillageTrees(village.transform, woodMaterial, foliageMaterial, foliageLightMaterial, stoneMaterial, navigation);
    }

    private void CreateImportedBuilding(
        Transform parent,
        string name,
        string modelName,
        Vector3 groundPosition,
        float yaw,
        float targetFootprint,
        GridNavigationService navigation)
    {
        GameObject prefab = Resources.Load<GameObject>(VillageModelPath + modelName);
        if (prefab == null)
        {
            CreateFallbackHouse(parent, name, groundPosition, yaw, targetFootprint, navigation);
            return;
        }

        GameObject wrapper = new(name);
        wrapper.transform.SetParent(parent);

        GameObject model = Instantiate(prefab, wrapper.transform);
        model.name = "Quaternius Art";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        DisableColliders(model);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (!TryGetRendererBounds(renderers, out Bounds initialBounds))
        {
            Destroy(wrapper);
            CreateFallbackHouse(parent, name, groundPosition, yaw, targetFootprint, navigation);
            return;
        }

        float horizontalSize = Mathf.Max(initialBounds.size.x, initialBounds.size.z);
        float modelScale = horizontalSize > 0.001f ? targetFootprint / horizontalSize : 1f;
        model.transform.localScale = Vector3.one * modelScale;
        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        if (!TryGetRendererBounds(renderers, out Bounds scaledBounds))
        {
            return;
        }

        wrapper.transform.position += new Vector3(
            groundPosition.x - scaledBounds.center.x,
            groundPosition.y - scaledBounds.min.y,
            groundPosition.z - scaledBounds.center.z);

        TryGetRendererBounds(renderers, out Bounds finalBounds);
        BoxCollider blocker = wrapper.AddComponent<BoxCollider>();
        blocker.center = wrapper.transform.InverseTransformPoint(finalBounds.center);
        blocker.size = finalBounds.size;

        foreach (Renderer renderer in renderers)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        Physics.SyncTransforms();
        navigation.SetBlocked(finalBounds, 0.22f);
    }

    private void CreateFallbackHouse(
        Transform parent,
        string name,
        Vector3 groundPosition,
        float yaw,
        float targetFootprint,
        GridNavigationService navigation)
    {
        Material wall = CreateMaterial(new Color(0.58f, 0.47f, 0.32f));
        Material roof = CreateMaterial(new Color(0.26f, 0.12f, 0.08f));
        Material timber = CreateMaterial(WoodColor);

        GameObject house = new(name + " (Fallback)");
        house.transform.SetParent(parent);
        house.transform.position = groundPosition;
        house.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateVisualPrimitive(house.transform, "Plastered Walls", PrimitiveType.Cube, new Vector3(0f, 1.1f, 0f), new Vector3(targetFootprint, 2.2f, targetFootprint * 0.72f), Quaternion.identity, wall, false, true);
        CreateVisualPrimitive(house.transform, "Roof Left", PrimitiveType.Cube, new Vector3(-0.86f, 2.35f, 0f), new Vector3(targetFootprint * 0.62f, 0.28f, targetFootprint * 0.88f), Quaternion.Euler(0f, 0f, -30f), roof, false, true);
        CreateVisualPrimitive(house.transform, "Roof Right", PrimitiveType.Cube, new Vector3(0.86f, 2.35f, 0f), new Vector3(targetFootprint * 0.62f, 0.28f, targetFootprint * 0.88f), Quaternion.Euler(0f, 0f, 30f), roof, false, true);
        CreateVisualPrimitive(house.transform, "Door", PrimitiveType.Cube, new Vector3(0f, 0.8f, -targetFootprint * 0.37f), new Vector3(0.8f, 1.55f, 0.12f), Quaternion.identity, timber, false, true);

        BoxCollider blocker = house.AddComponent<BoxCollider>();
        blocker.center = new Vector3(0f, 1.1f, 0f);
        blocker.size = new Vector3(targetFootprint, 2.2f, targetFootprint * 0.72f);
        Physics.SyncTransforms();
        navigation.SetBlocked(blocker.bounds, 0.22f);
    }

    private void CreateRuinDetails(
        Transform parent,
        Material burnedWoodMaterial,
        Material ashMaterial,
        GridNavigationService navigation)
    {
        CreateVisualPrimitive(parent, "Storehouse Ash", PrimitiveType.Cylinder, new Vector3(6.2f, 0.025f, -5.6f), new Vector3(2.2f, 0.025f, 1.7f), Quaternion.identity, ashMaterial, false);
        CreateRuinBeam(parent, new Vector3(5.8f, 0.30f, -4.7f), new Vector3(3.8f, 0.34f, 0.34f), 22f, burnedWoodMaterial, navigation);
        CreateRuinBeam(parent, new Vector3(7.0f, 0.25f, -6.2f), new Vector3(3.1f, 0.30f, 0.30f), -28f, burnedWoodMaterial, navigation);
        CreateRuinBeam(parent, new Vector3(6.9f, 0.62f, -5.1f), new Vector3(0.30f, 1.8f, 0.30f), 14f, burnedWoodMaterial, navigation);
    }

    private void CreateRuinBeam(
        Transform parent,
        Vector3 position,
        Vector3 scale,
        float yaw,
        Material material,
        GridNavigationService navigation)
    {
        GameObject beam = CreateVisualPrimitive(parent, "Charred Fallen Beam", PrimitiveType.Cube, position, scale, Quaternion.Euler(8f, yaw, 4f), material, true, true);
        Physics.SyncTransforms();
        navigation.SetBlocked(beam.GetComponent<Collider>().bounds, 0.08f);
    }

    private void CreatePalisade(Transform parent, Material material, GridNavigationService navigation)
    {
        CreateFenceSection(parent, new Vector3(-8.0f, 0f, 9.4f), 5, Vector3.right, material, navigation);
        CreateFenceSection(parent, new Vector3(5.2f, 0f, 9.4f), 5, Vector3.right, material, navigation);
        CreateFenceSection(parent, new Vector3(-13.2f, 0f, -1.6f), 5, Vector3.forward, material, navigation);
        CreateFenceSection(parent, new Vector3(13.2f, 0f, -1.8f), 5, Vector3.forward, material, navigation);
    }

    private void CreateFenceSection(
        Transform parent,
        Vector3 start,
        int count,
        Vector3 direction,
        Material material,
        GridNavigationService navigation)
    {
        Bounds combined = new(start, Vector3.zero);

        for (int index = 0; index < count; index++)
        {
            Vector3 position = start + direction * (index * 0.62f);
            float height = 1.45f + (index % 2) * 0.18f;
            GameObject stake = CreateVisualPrimitive(
                parent,
                "Palisade Stake",
                PrimitiveType.Cylinder,
                position + Vector3.up * (height * 0.5f),
                new Vector3(0.24f, height * 0.5f, 0.24f),
                Quaternion.identity,
                material,
                true,
                true);

            combined.Encapsulate(stake.GetComponent<Collider>().bounds);
        }

        navigation.SetBlocked(combined, 0.12f);
    }

    private void CreateVillageTrees(
        Transform parent,
        Material trunkMaterial,
        Material foliageMaterial,
        Material foliageLightMaterial,
        Material stoneMaterial,
        GridNavigationService navigation)
    {
        Vector3[] treePositions =
        {
            new(-13.6f, 0f, 8.4f),
            new(-11.3f, 0f, -8.8f),
            new(13.5f, 0f, 8.2f),
            new(12.8f, 0f, -8.9f),
            new(-3.6f, 0f, 10.3f),
            new(4.0f, 0f, 10.4f)
        };

        for (int index = 0; index < treePositions.Length; index++)
        {
            CreateTree(
                parent,
                treePositions[index],
                0.85f + (index % 3) * 0.10f,
                trunkMaterial,
                index % 2 == 0 ? foliageMaterial : foliageLightMaterial,
                navigation);
        }

        CreateRock(parent, new Vector3(-12.4f, 0f, -4.0f), new Vector3(1.8f, 0.8f, 1.1f), 18f, stoneMaterial, navigation);
        CreateRock(parent, new Vector3(12.1f, 0f, 4.0f), new Vector3(1.4f, 0.65f, 1.0f), -12f, stoneMaterial, navigation);
    }

    private void CreateTree(
        Transform parent,
        Vector3 position,
        float scale,
        Material trunkMaterial,
        Material foliageMaterial,
        GridNavigationService navigation)
    {
        GameObject root = new("Pine Tree");
        root.transform.SetParent(parent);
        root.transform.position = position;

        GameObject trunk = CreateVisualPrimitive(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.25f * scale, 0f), new Vector3(0.30f, 1.25f, 0.30f) * scale, Quaternion.identity, trunkMaterial, true, true);
        CreateVisualPrimitive(root.transform, "Lower Crown", PrimitiveType.Sphere, new Vector3(0f, 2.35f * scale, 0f), new Vector3(1.45f, 0.70f, 1.45f) * scale, Quaternion.identity, foliageMaterial, false, true);
        CreateVisualPrimitive(root.transform, "Middle Crown", PrimitiveType.Sphere, new Vector3(0f, 3.15f * scale, 0f), new Vector3(1.12f, 0.68f, 1.12f) * scale, Quaternion.identity, foliageMaterial, false, true);
        CreateVisualPrimitive(root.transform, "Top Crown", PrimitiveType.Sphere, new Vector3(0f, 3.75f * scale, 0f), new Vector3(0.72f, 0.58f, 0.72f) * scale, Quaternion.identity, foliageMaterial, false, true);

        Physics.SyncTransforms();
        navigation.SetBlocked(trunk.GetComponent<Collider>().bounds, 0.25f);
    }

    private void CreateRock(
        Transform parent,
        Vector3 position,
        Vector3 scale,
        float yaw,
        Material material,
        GridNavigationService navigation)
    {
        GameObject rock = CreateVisualPrimitive(parent, "Mossy Boundary Stone", PrimitiveType.Sphere, position + Vector3.up * (scale.y * 0.38f), scale, Quaternion.Euler(0f, yaw, 8f), material, true, true);
        Physics.SyncTransforms();
        navigation.SetBlocked(rock.GetComponent<Collider>().bounds, 0.18f);
    }

    private CampCore CreateCampCore(
        Material stoneMaterial,
        Material woodMaterial,
        Material emberMaterial,
        GridNavigationService navigation)
    {
        GameObject core = new("Broken Shrine Core");
        core.transform.SetParent(transform);
        core.transform.position = Vector3.zero;

        BoxCollider blocker = core.AddComponent<BoxCollider>();
        blocker.center = new Vector3(0f, 0.72f, 0f);
        blocker.size = new Vector3(1.7f, 1.45f, 1.7f);

        CreateVisualPrimitive(core.transform, "Stone Plinth", PrimitiveType.Cylinder, new Vector3(0f, 0.20f, 0f), new Vector3(1.25f, 0.20f, 1.25f), Quaternion.identity, stoneMaterial, false, true);
        CreateVisualPrimitive(core.transform, "Old Idol", PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f), new Vector3(0.72f, 1.50f, 0.62f), Quaternion.Euler(0f, 8f, 3f), woodMaterial, false, true);
        CreateVisualPrimitive(core.transform, "Carved Ember", PrimitiveType.Sphere, new Vector3(0f, 1.04f, -0.36f), new Vector3(0.28f, 0.38f, 0.14f), Quaternion.identity, emberMaterial, false, true);
        CreateVisualPrimitive(core.transform, "Broken Crossbeam", PrimitiveType.Cube, new Vector3(0.26f, 1.48f, 0f), new Vector3(1.05f, 0.20f, 0.20f), Quaternion.Euler(0f, 0f, 13f), woodMaterial, false, true);

        CreateCampfire(core.transform, emberMaterial, stoneMaterial);

        Physics.SyncTransforms();
        navigation.SetBlocked(blocker.bounds, 0.15f);
        return core.AddComponent<CampCore>();
    }

    private void CreateCampfire(Transform parent, Material emberMaterial, Material stoneMaterial)
    {
        GameObject fire = CreateVisualPrimitive(parent, "Campfire Embers", PrimitiveType.Sphere, new Vector3(1.65f, 0.16f, -1.15f), new Vector3(0.55f, 0.16f, 0.55f), Quaternion.identity, emberMaterial, false);

        for (int index = 0; index < 6; index++)
        {
            float angle = index * Mathf.PI * 2f / 6f;
            Vector3 offset = new(Mathf.Cos(angle) * 0.56f, 0.12f, Mathf.Sin(angle) * 0.56f);
            CreateVisualPrimitive(parent, "Fire Ring Stone", PrimitiveType.Sphere, new Vector3(1.65f, 0f, -1.15f) + offset, new Vector3(0.28f, 0.19f, 0.24f), Quaternion.identity, stoneMaterial, false, true);
        }

        Light light = fire.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.36f, 0.12f);
        light.intensity = 1.8f;
        light.range = 6.5f;
        light.shadows = LightShadows.Soft;
    }

    private void CreateResourceNode(
        Vector3 position,
        Material barkMaterial,
        Material cutMaterial,
        GridNavigationService navigation)
    {
        GameObject resource = new("Wood Cache");
        resource.transform.SetParent(transform);
        resource.transform.position = position;
        Material resourceBark = new(barkMaterial)
        {
            hideFlags = HideFlags.DontSave
        };

        BoxCollider collider = resource.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.48f, 0f);
        collider.size = new Vector3(2.0f, 0.95f, 1.35f);

        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                Vector3 localPosition = new Vector3((column - 1) * 0.58f, 0.30f + row * 0.42f, 0f);
                CreateVisualPrimitive(resource.transform, "Split Log", PrimitiveType.Cylinder, localPosition, new Vector3(0.25f, 0.62f, 0.25f), Quaternion.Euler(0f, 0f, 90f), resourceBark, false, true);
                CreateVisualPrimitive(resource.transform, "Cut End", PrimitiveType.Cylinder, localPosition + new Vector3(0.64f, 0f, 0f), new Vector3(0.20f, 0.02f, 0.20f), Quaternion.Euler(0f, 0f, 90f), cutMaterial, false, true);
            }
        }

        resource.AddComponent<ResourceNode>();
        Physics.SyncTransforms();
        navigation.SetBlocked(collider.bounds, 0.18f);
    }

    private void CreateSquad(
        Material heroMaterial,
        Material pawnMaterial,
        Material skinMaterial,
        Material hairMaterial,
        Material accentMaterial,
        Material selectionMaterial,
        GridNavigationService navigation,
        ResourceStockpile stockpile)
    {
        Vector3[] positions =
        {
            new(-1.20f, 0f, -2.00f),
            new(-2.30f, 0f, -3.05f),
            new(-0.70f, 0f, -3.25f),
            new(0.90f, 0f, -3.10f),
            new(2.20f, 0f, -2.45f),
            new(2.50f, 0f, -3.75f)
        };

        for (int index = 0; index < positions.Length; index++)
        {
            bool isHero = index == 0;
            GameObject unit = new(isHero ? "Main Hero" : $"Pawn {index}");
            unit.transform.SetParent(transform);
            unit.transform.position = positions[index];

            CapsuleCollider bodyCollider = unit.AddComponent<CapsuleCollider>();
            bodyCollider.center = new Vector3(0f, isHero ? 0.95f : 0.82f, 0f);
            bodyCollider.radius = isHero ? 0.40f : 0.34f;
            bodyCollider.height = isHero ? 1.90f : 1.65f;

            Material cloth = new(isHero ? heroMaterial : pawnMaterial);
            if (!isHero)
            {
                Color tint = Color.Lerp(PawnColor, new Color(0.34f, 0.29f, 0.20f), index / 7f);
                cloth.color = tint;
            }

            StylizedCharacterBuilder.BuildHuman(
                unit.transform,
                cloth,
                skinMaterial,
                hairMaterial,
                accentMaterial,
                isHero,
                index);

            SelectableUnit selectable = unit.AddComponent<SelectableUnit>();
            selectable.Initialize(selectionMaterial);

            NavigationAgent agent = unit.AddComponent<NavigationAgent>();
            agent.Initialize(navigation);
            agent.Speed = isHero ? 4.15f : 3.75f;

            ResourceCollector collector = unit.AddComponent<ResourceCollector>();
            collector.Initialize(stockpile);

            unit.AddComponent<UnitCombat>();
        }
    }

    private static GameObject CreateVisualPrimitive(
        Transform parent,
        string name,
        PrimitiveType primitive,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material,
        bool keepCollider,
        bool castShadows = false)
    {
        GameObject visual = GameObject.CreatePrimitive(primitive);
        visual.name = name;
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = position;
        visual.transform.localRotation = rotation;
        visual.transform.localScale = scale;

        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = keepCollider;
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        return visual;
    }

    private static void DisableColliders(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
    }

    private static bool TryGetRendererBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private static Material CreateMaterial(Color color, bool transparent = false)
    {
        Shader shader = Shader.Find("Standard");
        Material material = new(shader)
        {
            color = color,
            hideFlags = HideFlags.DontSave
        };

        material.SetFloat("_Glossiness", 0.16f);

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
