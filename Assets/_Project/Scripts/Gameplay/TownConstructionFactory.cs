using Hollowwest.Economy;
using Hollowwest.Navigation;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public static class TownConstructionFactory
{
    private const string VillageModelPath = "Models/Quaternius/MedievalVillage/";

    public static GameObject CreatePreview(
        BuildingDefinition definition,
        Transform parent,
        out Renderer[] renderers,
        string modelNameOverride = null)
    {
        return CreateArt(definition, parent, "Construction Preview", out renderers, modelNameOverride);
    }

    public static TownBuilding CreateConstructionSite(
        BuildingDefinition definition,
        Transform parent,
        Vector3 position,
        float yaw,
        GridNavigationService navigation,
        SettlementState settlement,
        ResourceStockpile stockpile,
        string modelNameOverride = null)
    {
        GameObject root = new(definition.DisplayName + " Construction Site");
        root.transform.SetParent(parent, false);

        GameObject blueprintVisual = CreateArt(
            definition,
            root.transform,
            "Blueprint",
            out Renderer[] blueprintRenderers,
            modelNameOverride);
        GameObject constructionVisual = CreateArt(
            definition,
            root.transform,
            "Building In Progress",
            out Renderer[] constructionRenderers,
            modelNameOverride);
        GameObject constructionPiecesVisual = CreateConstructionPieces(
            definition,
            root.transform,
            out Renderer[] constructionPieces);
        ConfigureBlueprintMaterials(blueprintRenderers);

        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Physics.SyncTransforms();

        if (!TryGetRendererBounds(constructionRenderers, out Bounds visualBounds))
        {
            visualBounds = new Bounds(
                position + Vector3.up * 1.5f,
                new Vector3(definition.Footprint, 3f, definition.Footprint * 0.75f));
        }

        BoxCollider blocker = root.AddComponent<BoxCollider>();
        float localHeight = Mathf.Max(1f, visualBounds.size.y);
        blocker.center = new Vector3(0f, localHeight * 0.5f, 0f);
        blocker.size = new Vector3(
            definition.Footprint * 0.84f,
            localHeight,
            definition.Footprint * 0.72f);

        Physics.SyncTransforms();
        var navigationReservation = navigation?.ReserveBlocked(blocker.bounds, 0.24f);

        TownBuilding building = root.AddComponent<TownBuilding>();
        TownConstructionSite constructionSite = root.AddComponent<TownConstructionSite>();
        TownWorkplace workplace = root.AddComponent<TownWorkplace>();
        workplace.Initialize(building, definition, stockpile);
        building.InitializeConstruction(
            definition,
            settlement,
            constructionSite,
            workplace,
            navigation,
            navigationReservation,
            blueprintVisual,
            constructionVisual,
            constructionPiecesVisual,
            constructionPieces,
            constructionRenderers,
            visualBounds);
        constructionSite.Initialize(building, definition.ConstructionWork);
        return building;
    }

    private static GameObject CreateConstructionPieces(
        BuildingDefinition definition,
        Transform parent,
        out Renderer[] renderers)
    {
        GameObject root = new("Visible Construction Materials");
        root.transform.SetParent(parent, false);

        bool brickBuilding = false;
        if (definition.ConstructionCosts != null)
        {
            foreach (ResourceAmount cost in definition.ConstructionCosts)
            {
                if (cost.Type == ResourceType.Brick || cost.Type == ResourceType.Stone)
                {
                    brickBuilding = true;
                    break;
                }
            }
        }

        Material foundation = CreateMaterial(brickBuilding
            ? new Color(0.52f, 0.31f, 0.22f)
            : new Color(0.38f, 0.39f, 0.36f));
        Material timber = CreateMaterial(new Color(0.58f, 0.36f, 0.16f));
        float half = definition.Footprint * 0.42f;
        float depth = definition.Footprint * 0.62f;

        for (int side = -1; side <= 1; side += 2)
        {
            for (int index = 0; index < 5; index++)
            {
                float x = Mathf.Lerp(-half, half, index / 4f);
                CreatePrimitive(root.transform, "Foundation block", PrimitiveType.Cube,
                    new Vector3(x, 0.16f, side * depth * 0.5f),
                    new Vector3(definition.Footprint * 0.19f, 0.30f, 0.42f),
                    Quaternion.identity,
                    foundation);
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            for (int layer = 0; layer < 4; layer++)
            {
                CreatePrimitive(root.transform, "Wall board", PrimitiveType.Cube,
                    new Vector3(0f, 0.55f + layer * 0.45f, side * depth * 0.5f),
                    new Vector3(definition.Footprint * 0.82f, 0.18f, 0.16f),
                    Quaternion.identity,
                    timber);
            }
        }

        for (int xSide = -1; xSide <= 1; xSide += 2)
        {
            for (int zSide = -1; zSide <= 1; zSide += 2)
            {
                CreatePrimitive(root.transform, "Frame post", PrimitiveType.Cube,
                    new Vector3(xSide * half, 1.25f, zSide * depth * 0.5f),
                    new Vector3(0.20f, 2.35f, 0.20f),
                    Quaternion.identity,
                    timber);
            }
        }

        for (int index = 0; index < 6; index++)
        {
            float x = Mathf.Lerp(-half, half, index / 5f);
            CreatePrimitive(root.transform, "Roof rafter", PrimitiveType.Cube,
                new Vector3(x, 2.55f, 0f),
                new Vector3(0.16f, 0.16f, depth * 1.18f),
                Quaternion.Euler(0f, 0f, index % 2 == 0 ? 18f : -18f),
                timber);
        }

        renderers = root.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        return root;
    }

    private static GameObject CreateArt(
        BuildingDefinition definition,
        Transform parent,
        string objectName,
        out Renderer[] renderers,
        string modelNameOverride = null)
    {
        GameObject root = new(objectName);
        root.transform.SetParent(parent, false);

        string selectedModelName = string.IsNullOrEmpty(modelNameOverride)
            ? definition.ModelName
            : modelNameOverride;
        string modelPath = selectedModelName.Contains("/")
            ? "Models/" + selectedModelName
            : VillageModelPath + selectedModelName;
        GameObject prefab = Resources.Load<GameObject>(modelPath);
        if (prefab == null)
        {
            BuildFallback(root.transform, definition.Footprint);
            renderers = root.GetComponentsInChildren<Renderer>();
            return root;
        }

        GameObject model = Object.Instantiate(prefab, root.transform);
        model.name = "Building Art";
        Vector3 importedScale = model.transform.localScale;
        Quaternion importedRotation = model.transform.localRotation;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = importedRotation;
        model.transform.localScale = importedScale;
        DisableColliders(model);

        renderers = model.GetComponentsInChildren<Renderer>();
        if (!TryGetRendererBounds(renderers, out Bounds initialBounds))
        {
            Object.Destroy(model);
            BuildFallback(root.transform, definition.Footprint);
            renderers = root.GetComponentsInChildren<Renderer>();
            return root;
        }

        float horizontalSize = Mathf.Max(initialBounds.size.x, initialBounds.size.z);
        float modelScale = horizontalSize > 0.001f
            ? definition.Footprint / horizontalSize
            : 1f;
        model.transform.localScale = importedScale * modelScale;

        if (TryGetRendererBounds(renderers, out Bounds scaledBounds))
        {
            model.transform.position += new Vector3(
                -scaledBounds.center.x,
                -scaledBounds.min.y,
                -scaledBounds.center.z);
        }

        CreateIdentityDecor(definition, root.transform);
        renderers = root.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        return root;
    }

    private static void CreateIdentityDecor(BuildingDefinition definition, Transform root)
    {
        float half = definition.Footprint * 0.43f;
        switch (definition.Id)
        {
            case "izba":
                // Both custom visual variants carry their own porch and folk details.
                break;
            case "longhouse":
                AddDecoration(root, "Kenney/Survival/signpost", new Vector3(-half, 0f, -half), 18f, 1.7f);
                AddDecoration(root, "Kenney/Survival/bedroll", new Vector3(half, 0f, half * 0.55f), 80f, 0.45f);
                break;
            case "storehouse":
                AddDecoration(root, "Kenney/Survival/box-large", new Vector3(-half, 0f, half * 0.25f), 12f, 1.05f);
                AddDecoration(root, "Kenney/Survival/chest", new Vector3(half, 0f, -half * 0.15f), -18f, 0.9f);
                AddDecoration(root, "Kenney/Survival/barrel", new Vector3(half * 0.55f, 0f, half), 8f, 0.82f);
                break;
            case "common_yard":
                AddDecoration(root, "Kenney/Survival/campfire-pit", new Vector3(0f, 0f, -half), 0f, 0.35f);
                AddDecoration(root, "Kenney/Survival/signpost", new Vector3(half, 0f, half * 0.4f), -25f, 1.65f);
                break;
            case "lumber_camp":
                AddDecoration(root, "Kenney/Survival/resource-wood", new Vector3(-half, 0f, half * 0.35f), 25f, 0.8f);
                AddDecoration(root, "Kenney/Survival/tree-log", new Vector3(half, 0f, -half * 0.4f), 72f, 0.65f);
                AddDecoration(root, "Kenney/Survival/tool-axe", new Vector3(half * 0.55f, 0f, half), -12f, 0.75f);
                break;
            case "hunter_hut":
                AddDecoration(root, "Kenney/Survival/tent", new Vector3(-half * 0.65f, 0f, half * 0.6f), 26f, 1.65f);
                AddDecoration(root, "Kenney/Survival/campfire-pit", new Vector3(half, 0f, -half * 0.4f), 0f, 0.3f);
                break;
            case "forager_shed":
                AddDecoration(root, "Kenney/Survival/bucket", new Vector3(-half, 0f, 0f), 14f, 0.72f);
                AddDecoration(root, "Kenney/Survival/patch-grass-large", new Vector3(half, 0f, half * 0.3f), 42f, 0.45f);
                break;
            case "clay_yard":
                AddDecoration(root, "Kenney/Survival/tool-shovel", new Vector3(-half, 0f, -half * 0.2f), -18f, 0.95f);
                AddDecoration(root, "Kenney/Survival/bucket", new Vector3(half, 0f, half * 0.3f), 18f, 0.7f);
                AddDecoration(root, "Kenney/Nature/rock_smallA", new Vector3(half * 0.2f, 0f, half), 40f, 0.5f);
                break;
            case "fishing_station":
                AddDecoration(root, "Kenney/Survival/campfire-fishing-stand", new Vector3(-half, 0f, -half * 0.15f), 15f, 1.25f);
                AddDecoration(root, "Kenney/Survival/fish-large", new Vector3(half, 0f, half * 0.25f), -25f, 0.55f);
                AddDecoration(root, "Kenney/Survival/bucket", new Vector3(half * 0.45f, 0f, half), 6f, 0.65f);
                break;
            case "workshop":
                AddDecoration(root, "Kenney/Survival/workbench", new Vector3(-half, 0f, half * 0.2f), 18f, 1.15f);
                AddDecoration(root, "Kenney/Survival/resource-planks", new Vector3(half, 0f, -half * 0.3f), 72f, 0.75f);
                break;
            case "smithy":
                AddDecoration(root, "Kenney/Survival/workbench-anvil", new Vector3(-half, 0f, 0f), 20f, 1.1f);
                AddDecoration(root, "Kenney/Survival/tool-hammer", new Vector3(half, 0f, half * 0.25f), -18f, 0.72f);
                break;
            case "tannery":
                AddDecoration(root, "Kenney/Survival/barrel", new Vector3(-half, 0f, half * 0.2f), 0f, 0.85f);
                AddDecoration(root, "Kenney/Survival/workbench", new Vector3(half, 0f, -half * 0.15f), -22f, 1.05f);
                break;
            case "mill":
                AddDecoration(root, "Kenney/Survival/resource-planks", new Vector3(-half, 0f, half * 0.2f), 12f, 0.78f);
                AddDecoration(root, "Kenney/Survival/barrel", new Vector3(half, 0f, -half * 0.15f), 0f, 0.82f);
                break;
            case "watchtower":
                AddDecoration(root, "Kenney/Castle/flag-pennant", new Vector3(0f, 2.8f, 0f), 0f, 1.8f);
                break;
            case "guardhouse":
                AddDecoration(root, "Kenney/Castle/wall-narrow-wood-fence", new Vector3(-half, 0f, 0f), 90f, 1.4f);
                AddDecoration(root, "Kenney/Survival/chest", new Vector3(half, 0f, half * 0.2f), -14f, 0.82f);
                break;
            case "palisade_yard":
                AddDecoration(root, "Kenney/Survival/fence-fortified", new Vector3(-half, 0f, 0f), 90f, 1.45f);
                AddDecoration(root, "Kenney/Survival/resource-planks", new Vector3(half, 0f, half * 0.25f), 18f, 0.78f);
                break;
            case "fortified_gate":
                AddDecoration(root, "Kenney/Castle/gate", new Vector3(0f, 0f, -half * 0.8f), 0f, 3.0f);
                AddDecoration(root, "Kenney/Castle/flag-banner-long", new Vector3(half, 1.2f, 0f), 0f, 1.5f);
                break;
            case "kapishche":
                AddDecoration(root, "Kenney/Castle/flag-banner-long", new Vector3(0f, 0f, -half), 0f, 2.0f);
                AddDecoration(root, "Kenney/Castle/rocks-small", new Vector3(half, 0f, half * 0.3f), 35f, 0.72f);
                break;
            case "healer_banya":
                AddDecoration(root, "Kenney/Survival/bottle", new Vector3(-half, 0f, half * 0.15f), 10f, 0.55f);
                AddDecoration(root, "Kenney/Survival/bucket", new Vector3(half, 0f, -half * 0.2f), -15f, 0.7f);
                break;
            case "herb_garden":
                AddDecoration(root, "Kenney/Survival/patch-grass-large", new Vector3(-half, 0f, half * 0.2f), 15f, 0.5f);
                AddDecoration(root, "Kenney/Survival/fence", new Vector3(half, 0f, 0f), 90f, 1.1f);
                break;
            case "ward_house":
                AddDecoration(root, "Kenney/Castle/flag-pennant", new Vector3(-half, 0f, -half * 0.2f), 0f, 1.55f);
                AddDecoration(root, "Kenney/Nature/rock_tallA", new Vector3(half, 0f, half * 0.2f), 18f, 1.4f);
                break;
        }
    }

    private static void AddDecoration(
        Transform parent,
        string modelPath,
        Vector3 localGroundPosition,
        float yaw,
        float targetHeight)
    {
        GameObject prefab = Resources.Load<GameObject>("Models/" + modelPath);
        if (prefab == null)
        {
            return;
        }

        GameObject prop = Object.Instantiate(prefab, parent);
        prop.name = modelPath.Substring(modelPath.LastIndexOf('/') + 1);
        Vector3 importedScale = prop.transform.localScale;
        Quaternion importedRotation = prop.transform.localRotation;
        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * importedRotation;
        prop.transform.localScale = importedScale;
        DisableColliders(prop);

        Renderer[] propRenderers = prop.GetComponentsInChildren<Renderer>();
        if (!TryGetRendererBounds(propRenderers, out Bounds initialBounds))
        {
            Object.Destroy(prop);
            return;
        }

        float scale = initialBounds.size.y > 0.001f ? targetHeight / initialBounds.size.y : 1f;
        prop.transform.localScale = importedScale * scale;
        if (!TryGetRendererBounds(propRenderers, out Bounds scaledBounds))
        {
            return;
        }

        Vector3 desired = parent.TransformPoint(localGroundPosition);
        prop.transform.position += new Vector3(
            desired.x - scaledBounds.center.x,
            desired.y - scaledBounds.min.y,
            desired.z - scaledBounds.center.z);
    }

    private static void BuildFallback(Transform parent, float footprint)
    {
        Material wall = CreateMaterial(new Color(0.54f, 0.43f, 0.29f));
        Material roof = CreateMaterial(new Color(0.25f, 0.12f, 0.07f));
        Material timber = CreateMaterial(new Color(0.28f, 0.17f, 0.09f));

        CreatePrimitive(parent, "Walls", PrimitiveType.Cube, new Vector3(0f, 1.1f, 0f), new Vector3(footprint, 2.2f, footprint * 0.72f), Quaternion.identity, wall);
        CreatePrimitive(parent, "Roof Left", PrimitiveType.Cube, new Vector3(-footprint * 0.18f, 2.45f, 0f), new Vector3(footprint * 0.62f, 0.30f, footprint * 0.88f), Quaternion.Euler(0f, 0f, -30f), roof);
        CreatePrimitive(parent, "Roof Right", PrimitiveType.Cube, new Vector3(footprint * 0.18f, 2.45f, 0f), new Vector3(footprint * 0.62f, 0.30f, footprint * 0.88f), Quaternion.Euler(0f, 0f, 30f), roof);
        CreatePrimitive(parent, "Door", PrimitiveType.Cube, new Vector3(0f, 0.78f, -footprint * 0.37f), new Vector3(0.78f, 1.55f, 0.12f), Quaternion.identity, timber);
    }

    private static void CreatePrimitive(
        Transform parent,
        string name,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private static Material CreateMaterial(Color color)
    {
        return new Material(Shader.Find("Standard"))
        {
            color = color,
            hideFlags = HideFlags.DontSave
        };
    }

    private static void ConfigureBlueprintMaterials(Renderer[] renderers)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            Material[] sourceMaterials = renderer.sharedMaterials;
            Material[] materials = new Material[sourceMaterials.Length];
            for (int index = 0; index < sourceMaterials.Length; index++)
            {
                Material source = sourceMaterials[index];
                if (source == null)
                {
                    continue;
                }

                Material material = new(source)
                {
                    hideFlags = HideFlags.DontSave
                };
                materials[index] = material;
                material.color = new Color(0.24f, 0.76f, 0.92f, 0.24f);
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }

            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private static void DisableColliders(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }
    }

    private static bool TryGetRendererBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        if (renderers == null)
        {
            return false;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }
}
}
