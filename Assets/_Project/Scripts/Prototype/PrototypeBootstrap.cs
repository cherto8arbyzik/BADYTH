using System.Collections.Generic;
using Hollowwest.Economy;
using Hollowwest.Gameplay;
using Hollowwest.Controls;
using Hollowwest.Navigation;
using Hollowwest.Presentation;
using Hollowwest.Selection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollowwest.Prototype
{

public sealed class PrototypeBootstrap : MonoBehaviour
{
    private const float WorldWidth = 310f;
    private const float WorldDepth = 280f;
    private const float NavigationCellSize = 0.75f;
    private const float IslandCornerCutX = 52f;
    private const float IslandCornerCutZ = 38f;
    private const int GrassTuftTarget = 3800;
    private const int TreeTarget = 240;
    private const int BushTarget = 110;
    private const string VillageModelPath = "Models/Quaternius/MedievalVillage/";
    private const string NatureModelPath = "Models/Quaternius/UltimateStylizedNature/";
    private const string RuinsModelPath = "Models/Quaternius/UltimateModularRuins/";

    private static readonly Vector3 TownHallPosition = new(-16f, 0f, -8f);
    private static readonly Vector3[] MainRoadPoints =
    {
        TownHallPosition + new Vector3(0f, 0f, -7f),
        TownHallPosition + new Vector3(0f, 0f, -17f),
        TownHallPosition + new Vector3(0f, 0f, -31f)
    };

    private static readonly LakeLayout[] Lakes =
    {
        new(new Vector3(88f, 0f, 44f), new Vector2(23f, 15f)),
        new(new Vector3(-102f, 0f, -52f), new Vector2(13f, 8.5f))
    };

    private static readonly ForestLayout[] Forests =
    {
        new(new Vector2(-124f, 69f), new Vector2(31f, 24f)),
        new(new Vector2(-126f, -70f), new Vector2(34f, 25f)),
        new(new Vector2(118f, 68f), new Vector2(36f, 26f)),
        new(new Vector2(116f, -68f), new Vector2(32f, 24f)),
        new(new Vector2(22f, 78f), new Vector2(27f, 18f))
    };

    private static readonly Color GroundColor = new(0.22f, 0.29f, 0.17f);
    private static readonly Color GroundEdgeColor = new(0.16f, 0.17f, 0.14f);
    private static readonly Color DirtColor = new(0.31f, 0.28f, 0.22f);
    private static readonly Color StoneColor = new(0.33f, 0.36f, 0.34f);
    private static readonly Color WoodColor = new(0.31f, 0.19f, 0.10f);
    private static readonly Color LightWoodColor = new(0.52f, 0.32f, 0.14f);
    private static readonly Color BurnedWoodColor = new(0.11f, 0.09f, 0.08f);
    private static readonly Color AshColor = new(0.10f, 0.09f, 0.08f);
    private static readonly Color FoliageColor = new(0.14f, 0.31f, 0.18f);
    private static readonly Color FoliageLightColor = new(0.24f, 0.42f, 0.22f);
    private static readonly Color HeroColor = new(0.55f, 0.18f, 0.13f);
    private static readonly Color SkinColor = new(0.73f, 0.49f, 0.31f);
    private static readonly Color HairColor = new(0.16f, 0.10f, 0.07f);
    private static readonly Color AccentColor = new(0.76f, 0.61f, 0.28f);
    private static readonly Color SelectionColor = new(0.30f, 1f, 0.45f, 0.72f);
    private static readonly Color CommandColor = new(1f, 0.72f, 0.20f, 0.85f);
    private static readonly Color EnemyColor = new(0.32f, 0.08f, 0.13f);

    private readonly struct LakeLayout
    {
        public LakeLayout(Vector3 center, Vector2 radii)
        {
            Center = center;
            Radii = radii;
        }

        public Vector3 Center { get; }
        public Vector2 Radii { get; }
    }

    private readonly struct ForestLayout
    {
        public ForestLayout(Vector2 center, Vector2 radii)
        {
            Center = center;
            Radii = radii;
        }

        public Vector2 Center { get; }
        public Vector2 Radii { get; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfNeeded()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Town" && sceneName != "Prototype")
        {
            return;
        }

        if (FindFirstObjectByType<PrototypeBootstrap>() != null)
        {
            return;
        }

        GameObject root = new("PrototypeRoot");
        root.AddComponent<PrototypeBootstrap>().Build();
    }

    private void Build()
    {
        GameInputRouter.EnsureExists().ActivateSettlement();
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.shadowDistance = 150f;
        QualitySettings.antiAliasing = 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

        Material groundMaterial = CreateMaterial(GroundColor);
        Material groundEdgeMaterial = CreateMaterial(GroundEdgeColor);
        Material dirtMaterial = CreateMaterial(DirtColor);
        Material stoneMaterial = CreateMaterial(StoneColor);
        Material woodMaterial = CreateMaterial(WoodColor);
        Material lightWoodMaterial = CreateMaterial(LightWoodColor);
        Material burnedWoodMaterial = CreateMaterial(BurnedWoodColor);
        Material ashMaterial = CreateMaterial(AshColor);
        Material foliageMaterial = CreateMaterial(FoliageColor);
        Material foliageLightMaterial = CreateMaterial(FoliageLightColor);
        Material heroMaterial = CreateMaterial(HeroColor);
        Material skinMaterial = CreateMaterial(SkinColor);
        Material hairMaterial = CreateMaterial(HairColor);
        Material accentMaterial = CreateMaterial(AccentColor);
        Material selectionMaterial = CreateMaterial(SelectionColor, true);
        Material commandMaterial = CreateMaterial(CommandColor, true);
        Material enemyMaterial = CreateMaterial(EnemyColor);
        ConfigureGroundMaterial(groundMaterial);
        ResourceStockpile stockpile = gameObject.AddComponent<ResourceStockpile>();
        stockpile.Initialize(300, new[]
        {
            new ResourceAmount(ResourceType.Timber, 90),
            new ResourceAmount(ResourceType.Stone, 45),
            new ResourceAmount(ResourceType.Clay, 15),
            new ResourceAmount(ResourceType.Food, 70),
            new ResourceAmount(ResourceType.Herb, 12),
            new ResourceAmount(ResourceType.Hide, 5),
            new ResourceAmount(ResourceType.Plank, 8),
            new ResourceAmount(ResourceType.Tool, 8),
            new ResourceAmount(ResourceType.Provisions, 20),
            new ResourceAmount(ResourceType.OldIron, 5)
        });
        SettlementState settlement = gameObject.AddComponent<SettlementState>();
        settlement.Initialize(3, 3, 300);
        SettlementEconomy economy = gameObject.AddComponent<SettlementEconomy>();
        economy.Initialize(stockpile, settlement, 60f);

        Bounds playBounds = new(Vector3.zero, new Vector3(WorldWidth, 1f, WorldDepth));
        RoadNetwork roadNetwork = new();
        roadNetwork.RegisterPolyline(MainRoadPoints, 3.2f);
        CreateGround(groundMaterial);
        Camera worldCamera = CreateCamera(playBounds);
        CreateLighting(out Light sunlight, out Light fillLight);

        GridNavigationService navigation = new(
            new Vector3(playBounds.min.x, 0f, playBounds.min.z),
            Mathf.CeilToInt(playBounds.size.x / NavigationCellSize),
            Mathf.CeilToInt(playBounds.size.z / NavigationCellSize),
            NavigationCellSize,
            point => GroundSurface.TryProjectPoint(point, out Vector3 projected)
                ? projected
                : point);

        BlockOutsideIsland(navigation);
        CreateWildlife(navigation);

        CampCore campCore = CreateCampCore();
        CreateResourceNode(new Vector3(42f, 0f, -25f), woodMaterial, lightWoodMaterial, navigation);
        CreateResourceNode(new Vector3(-48f, 0f, 18f), woodMaterial, lightWoodMaterial, navigation);
        CreateResourceNode(new Vector3(28f, 0f, -42f), woodMaterial, lightWoodMaterial, navigation);
        CreateResourceNode(new Vector3(-52f, 0f, -35f), woodMaterial, lightWoodMaterial, navigation);
        CreateResourceNode(new Vector3(5f, 0f, 38f), woodMaterial, lightWoodMaterial, navigation);
        CreateResourceDeposit("Разбираемый каменный завал", ResourceType.Stone, new Vector3(-64f, 0f, 26f), new Vector3(4.5f, 2.1f, 3.6f), stoneMaterial, 110, 5, navigation);
        CreateResourceDeposit("Глиняная жила", ResourceType.Clay, Lakes[0].Center + new Vector3(-17f, 0f, -4f), new Vector3(4.8f, 0.8f, 3.8f), dirtMaterial, 130, 4, navigation);
        CreateResourceDeposit("Глиняная жила", ResourceType.Clay, Lakes[1].Center + new Vector3(10f, 0f, 3f), new Vector3(3.8f, 0.7f, 3.2f), dirtMaterial, 90, 4, navigation);
        CreateResourceDeposit("Лечебные травы", ResourceType.Herb, new Vector3(34f, 0f, 52f), new Vector3(2.8f, 1.2f, 2.8f), foliageLightMaterial, 65, 3, navigation, false);
        CreateResourceDeposit("Ягодные заросли", ResourceType.Food, new Vector3(-76f, 0f, -18f), new Vector3(3.2f, 1.4f, 3.2f), foliageMaterial, 75, 3, navigation, false);

        GameObject hero = CreateSquad(
            heroMaterial,
            skinMaterial,
            hairMaterial,
            accentMaterial,
            selectionMaterial,
            navigation,
            stockpile,
            roadNetwork);

        DialogueController dialogue = gameObject.AddComponent<DialogueController>();
        dialogue.Initialize(
            hero.transform,
            hero.GetComponent<NavigationAgent>(),
            worldCamera.GetComponent<RtsCameraController>());
        SelectionController selection = gameObject.AddComponent<SelectionController>();
        selection.Initialize(worldCamera, commandMaterial, dialogue);
        GatheringAreaController gathering = gameObject.AddComponent<GatheringAreaController>();
        gathering.Initialize(worldCamera, stockpile);

        GameObject playerBuildings = new("Player Buildings");
        playerBuildings.transform.SetParent(transform);
        PlacementGridOverlay placementGrid = gameObject.AddComponent<PlacementGridOverlay>();
        placementGrid.Initialize(2f, 12);
        BuildingPlacementController placement = gameObject.AddComponent<BuildingPlacementController>();
        placement.Initialize(
            worldCamera,
            stockpile,
            navigation,
            settlement,
            playBounds,
            MainRoadPoints,
            roadNetwork,
            placementGrid,
            playerBuildings.transform);
        GameObject playerRoads = new("Built Roads");
        playerRoads.transform.SetParent(transform);
        RoadPlacementController roadPlacement = gameObject.AddComponent<RoadPlacementController>();
        roadPlacement.Initialize(
            worldCamera,
            roadNetwork,
            playerRoads.transform,
            groundEdgeMaterial,
            dirtMaterial,
            placement,
            stockpile,
            settlement);
        IReadOnlyList<BuildingDefinition> buildingCatalog = BuildingCatalog.CreatePrototypeCatalog();
        ExpeditionSystem expedition = gameObject.AddComponent<ExpeditionSystem>();
        expedition.Initialize(stockpile, settlement, 20f);
        CreateExpeditionEntrance(hero.transform, expedition, woodMaterial, accentMaterial);
        int rescuedResidentIndex = 0;
        expedition.ResidentsRescued += rescuedCount =>
        {
            for (int index = 0; index < rescuedCount; index++)
            {
                int residentSeed = 601 + rescuedResidentIndex * 37;
                float angle = rescuedResidentIndex * 1.7f;
                Vector3 home = TownHallPosition + new Vector3(Mathf.Cos(angle) * 4f, 0f, Mathf.Sin(angle) * 4f);
                CreateResident(
                    $"Спасённый житель {rescuedResidentIndex + 1}",
                    home,
                    home,
                    7f,
                    residentSeed,
                    Color.Lerp(new Color(0.25f, 0.34f, 0.29f), new Color(0.48f, 0.30f, 0.22f), Mathf.Repeat(rescuedResidentIndex * 0.31f, 1f)),
                    skinMaterial,
                    hairMaterial,
                    accentMaterial,
                    navigation,
                    roadNetwork);
                rescuedResidentIndex++;
            }
        };

        WaveDirector waveDirector = gameObject.AddComponent<WaveDirector>();
        waveDirector.Initialize(navigation, campCore, enemyMaterial, playBounds);

        GameSession session = gameObject.AddComponent<GameSession>();
        session.Initialize(waveDirector, campCore);

        TownDayNightVisuals lighting = gameObject.AddComponent<TownDayNightVisuals>();
        lighting.Initialize(session, worldCamera, sunlight, fillLight);

        PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
        hud.Initialize(selection, stockpile, session, placement, roadPlacement, gathering, buildingCatalog, settlement, economy, expedition);
    }

    private void CreateExpeditionEntrance(
        Transform hero,
        ExpeditionSystem expedition,
        Material woodMaterial,
        Material accentMaterial)
    {
        Vector3 entrancePosition = new(-16f, 0.05f, -113f);
        entrancePosition = ProjectToGround(entrancePosition, 0.05f);
        GameObject entrance = new("Rope Ladder To Outer Islands");
        entrance.transform.SetParent(transform);
        entrance.transform.position = entrancePosition;

        Material ropeMaterial = new(woodMaterial) { hideFlags = HideFlags.DontSave };
        for (int side = -1; side <= 1; side += 2)
        {
            CreateVisualPrimitive(
                entrance.transform,
                "Rope",
                PrimitiveType.Cylinder,
                new Vector3(side * 0.58f, -2.7f, -0.30f),
                new Vector3(0.09f, 3.1f, 0.09f),
                Quaternion.identity,
                ropeMaterial,
                false,
                true);
        }

        for (int rung = 0; rung < 8; rung++)
        {
            CreateVisualPrimitive(
                entrance.transform,
                "Ladder Rung",
                PrimitiveType.Cube,
                new Vector3(0f, -0.15f - rung * 0.72f, -0.30f),
                new Vector3(1.35f, 0.10f, 0.16f),
                Quaternion.identity,
                ropeMaterial,
                false,
                true);
        }

        GameObject marker = CreateVisualPrimitive(
            entrance.transform,
            "Entrance Marker",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.04f, 1.2f),
            new Vector3(2.1f, 0.035f, 2.1f),
            Quaternion.identity,
            new Material(accentMaterial) { hideFlags = HideFlags.DontSave },
            false,
            true);
        marker.GetComponent<Renderer>().sharedMaterial.color = new Color(0.88f, 0.65f, 0.22f, 0.72f);

        GameObject labelObject = new("Expedition Entrance Label");
        labelObject.transform.SetParent(entrance.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 2.6f, 0.8f);
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = "СПУСК НА ВНЕШНИЕ ОСТРОВА";
        label.fontSize = 38;
        label.characterSize = 0.075f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(1f, 0.88f, 0.55f);

        entrance.AddComponent<ExpeditionEntrance>().Initialize(hero, expedition, label);
    }

    private void CreateGround(Material groundMaterial)
    {
        if (!StartingIsland3VisualFactory.TryCreate(transform, out GameObject visualRoot))
        {
            Debug.LogError("Starting map 3.0 visual could not be loaded.");
            CreateFallbackGround(groundMaterial);
            return;
        }

        if (!StartingMap3GroundFactory.TryCreate(visualRoot.transform, out _))
        {
            Debug.LogError("Starting map 3.0 gameplay collision could not be loaded.");
            CreateFallbackGround(groundMaterial);
        }
    }

    private void CreateFallbackGround(Material groundMaterial)
    {
        GameObject ground = CreateIslandTop(groundMaterial);
        ground.transform.SetParent(transform);
        ground.AddComponent<GroundSurface>();
        ground.GetComponent<MeshRenderer>().enabled = false;
    }

    private static GameObject CreateIslandTop(Material groundMaterial)
    {
        Vector3[] outline = GetIslandOutline(0f);
        List<Vector3> vertices = new(1 + outline.Length * 2) { Vector3.zero };
        vertices.AddRange(outline);
        foreach (Vector3 point in outline)
        {
            vertices.Add(point + Vector3.down * 0.7f);
        }

        List<Vector2> uvs = new(vertices.Count);
        foreach (Vector3 vertex in vertices)
        {
            uvs.Add(new Vector2(
                vertex.x / WorldWidth + 0.5f,
                vertex.z / WorldDepth + 0.5f));
        }

        List<int> triangles = new(outline.Length * 9);
        for (int index = 0; index < outline.Length; index++)
        {
            int next = (index + 1) % outline.Length;
            int topCurrent = index + 1;
            int topNext = next + 1;
            int bottomCurrent = outline.Length + index + 1;
            int bottomNext = outline.Length + next + 1;

            triangles.Add(0);
            triangles.Add(topCurrent);
            triangles.Add(topNext);

            triangles.Add(topCurrent);
            triangles.Add(bottomCurrent);
            triangles.Add(topNext);
            triangles.Add(topNext);
            triangles.Add(bottomCurrent);
            triangles.Add(bottomNext);
        }

        Mesh mesh = new()
        {
            name = "Floating Island Top",
            hideFlags = HideFlags.DontSave
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        groundMaterial.SetInt("_Cull", 0);
        GameObject ground = new("Village Ground");
        ground.AddComponent<MeshFilter>().sharedMesh = mesh;
        ground.AddComponent<MeshRenderer>().sharedMaterial = groundMaterial;
        ground.AddComponent<MeshCollider>().sharedMesh = mesh;
        return ground;
    }

    private static void ConfigureGroundMaterial(Material material)
    {
        Texture2D color = Resources.Load<Texture2D>("Textures/AmbientCG/Grass002/Grass002_1K-JPG_Color");
        Texture2D normal = Resources.Load<Texture2D>("Textures/AmbientCG/Grass002/Grass002_1K-JPG_NormalGL");
        Texture2D roughness = Resources.Load<Texture2D>("Textures/AmbientCG/Grass002/Grass002_1K-JPG_Roughness");
        if (color != null)
        {
            material.mainTexture = color;
            material.mainTextureScale = new Vector2(20f, 14f);
            material.color = new Color(0.62f, 0.69f, 0.52f);
        }

        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.SetTextureScale("_BumpMap", new Vector2(20f, 14f));
            material.SetFloat("_BumpScale", 0.42f);
            material.EnableKeyword("_NORMALMAP");
        }

        if (roughness != null)
        {
            material.SetTexture("_MetallicGlossMap", roughness);
            material.SetTextureScale("_MetallicGlossMap", new Vector2(20f, 14f));
        }

        material.SetFloat("_Glossiness", 0.12f);
        material.SetFloat("_Metallic", 0f);
    }

    private void CreateFertilityPatches(Material grassSource, Material dirtSource)
    {
        GameObject root = new("Fertility Mosaic");
        root.transform.SetParent(transform);
        CreateFertilityPatch(root.transform, "Rich Meadow", new Vector3(42f, 0f, 46f), new Vector2(32f, 22f), 0.92f, grassSource, new Color(0.32f, 0.48f, 0.20f));
        CreateFertilityPatch(root.transform, "Rich Lakeside Soil", Lakes[0].Center + new Vector3(-27f, 0f, -14f), new Vector2(24f, 15f), 0.84f, grassSource, new Color(0.34f, 0.45f, 0.20f));
        CreateFertilityPatch(root.transform, "Thin Windward Soil", new Vector3(-70f, 0f, 62f), new Vector2(30f, 17f), 0.22f, dirtSource, new Color(0.37f, 0.30f, 0.21f));
        CreateFertilityPatch(root.transform, "Ashen Ground", new Vector3(68f, 0f, -58f), new Vector2(28f, 18f), 0.12f, dirtSource, new Color(0.29f, 0.27f, 0.23f));
        CreateFertilityPatch(root.transform, "Old Garden Soil", new Vector3(-42f, 0f, -30f), new Vector2(22f, 15f), 0.72f, grassSource, new Color(0.29f, 0.40f, 0.17f));
    }

    private static void CreateFertilityPatch(
        Transform parent,
        string name,
        Vector3 center,
        Vector2 radii,
        float fertility,
        Material source,
        Color tint)
    {
        Material material = new(source)
        {
            color = tint,
            hideFlags = HideFlags.DontSave
        };
        if (material.mainTexture != null)
        {
            material.mainTextureScale = new Vector2(5f, 4f);
        }

        GameObject patch = CreateEllipseDisc(parent, name, center, radii, 0.026f, material, false);
        patch.AddComponent<FertilityPatch>().Configure(center, radii, fertility);
    }

    private static Vector3[] GetIslandOutline(float inset)
    {
        float halfWidth = WorldWidth * 0.5f - inset;
        float halfDepth = WorldDepth * 0.5f - inset;
        return new[]
        {
            new Vector3(-halfWidth + IslandCornerCutX, 0f, -halfDepth),
            new Vector3(-halfWidth, 0f, -halfDepth + IslandCornerCutZ),
            new Vector3(-halfWidth, 0f, halfDepth - IslandCornerCutZ),
            new Vector3(-halfWidth + IslandCornerCutX, 0f, halfDepth),
            new Vector3(halfWidth - IslandCornerCutX, 0f, halfDepth),
            new Vector3(halfWidth, 0f, halfDepth - IslandCornerCutZ),
            new Vector3(halfWidth, 0f, -halfDepth + IslandCornerCutZ),
            new Vector3(halfWidth - IslandCornerCutX, 0f, -halfDepth)
        };
    }

    private static bool IsInsideIslandSurface(float x, float z, float inset)
    {
        float halfWidth = WorldWidth * 0.5f - inset;
        float halfDepth = WorldDepth * 0.5f - inset;
        float absoluteX = Mathf.Abs(x);
        float absoluteZ = Mathf.Abs(z);
        if (absoluteX > halfWidth || absoluteZ > halfDepth)
        {
            return false;
        }

        float cornerX = Mathf.Max(0f, absoluteX - (halfWidth - IslandCornerCutX));
        float cornerZ = Mathf.Max(0f, absoluteZ - (halfDepth - IslandCornerCutZ));
        return cornerX / IslandCornerCutX + cornerZ / IslandCornerCutZ <= 1f;
    }

    private static void BlockOutsideIsland(GridNavigationService navigation)
    {
        StartingMap3NavigationFootprint.TryLoad(out StartingMap3NavigationFootprint footprint);
        for (int x = 0; x < navigation.Width; x++)
        {
            for (int y = 0; y < navigation.Depth; y++)
            {
                Vector2Int cell = new(x, y);
                Vector3 world = navigation.CellToWorldUnprojected(cell);
                bool inside = footprint != null
                    ? footprint.Contains(world, 0.8f)
                    : IsInsideIslandSurface(world.x, world.z, 0.8f);
                if (!inside)
                {
                    navigation.SetCellBlocked(cell);
                }
            }
        }
    }

    private void CreateLakes(
        Material shoreMaterial,
        Material stoneMaterial,
        GridNavigationService navigation)
    {
        Material waterMaterial = CreateMaterial(new Color(0.16f, 0.46f, 0.60f, 0.76f), true);
        waterMaterial.SetFloat("_Glossiness", 0.78f);
        waterMaterial.SetFloat("_Metallic", 0.05f);
        waterMaterial.EnableKeyword("_EMISSION");
        waterMaterial.SetColor("_EmissionColor", new Color(0.015f, 0.055f, 0.075f));

        GameObject lakesRoot = new("Island Lakes");
        lakesRoot.transform.SetParent(transform);

        for (int lakeIndex = 0; lakeIndex < Lakes.Length; lakeIndex++)
        {
            LakeLayout lake = Lakes[lakeIndex];
            GameObject lakeRoot = new(lakeIndex == 0 ? "Great Sky Lake" : "Small Sky Lake");
            lakeRoot.transform.SetParent(lakesRoot.transform);
            lakeRoot.AddComponent<LakeShoreArea>().Configure(lake.Center, lake.Radii);

            CreateEllipseDisc(
                lakeRoot.transform,
                "Muddy Shore",
                lake.Center,
                lake.Radii + new Vector2(2.2f, 1.8f),
                0.022f,
                shoreMaterial,
                false);
            CreateEllipseDisc(
                lakeRoot.transform,
                "Lake Water",
                lake.Center,
                lake.Radii,
                0.055f,
                waterMaterial,
                true);

            Bounds blocked = new(
                lake.Center + Vector3.up * 0.12f,
                new Vector3(lake.Radii.x * 2f, 0.35f, lake.Radii.y * 2f));
            navigation.SetBlocked(blocked, 0.8f);
            CreateLakeShoreDetails(lakeRoot.transform, lake, lakeIndex, stoneMaterial);
        }
    }

    private void CreateLakeShoreDetails(
        Transform parent,
        LakeLayout lake,
        int lakeIndex,
        Material stoneMaterial)
    {
        Material reedMaterial = CreateMaterial(new Color(0.26f, 0.34f, 0.12f));
        int reedCount = lakeIndex == 0 ? 26 : 14;
        for (int index = 0; index < reedCount; index++)
        {
            float angle = (index / (float)reedCount) * Mathf.PI * 2f + Noise01(index * 31 + lakeIndex * 71) * 0.18f;
            float x = lake.Center.x + Mathf.Cos(angle) * (lake.Radii.x + 0.55f);
            float z = lake.Center.z + Mathf.Sin(angle) * (lake.Radii.y + 0.45f);
            float height = Mathf.Lerp(0.45f, 0.85f, Noise01(index * 53 + lakeIndex * 97));
            CreateVisualPrimitive(
                parent,
                "Lake Reed",
                PrimitiveType.Cylinder,
                new Vector3(x, height * 0.5f, z),
                new Vector3(0.055f, height * 0.5f, 0.055f),
                Quaternion.Euler(Noise01(index * 17) * 7f, index * 43f, Noise01(index * 23) * 7f),
                reedMaterial,
                false,
                false);
        }

        int rockCount = lakeIndex == 0 ? 10 : 6;
        for (int index = 0; index < rockCount; index++)
        {
            float angle = (index / (float)rockCount) * Mathf.PI * 2f + 0.27f;
            Vector3 position = new(
                lake.Center.x + Mathf.Cos(angle) * (lake.Radii.x + 1.3f),
                0.18f,
                lake.Center.z + Mathf.Sin(angle) * (lake.Radii.y + 1.1f));
            float size = Mathf.Lerp(0.45f, 1.05f, Noise01(index * 67 + lakeIndex * 101));
            CreateVisualPrimitive(
                parent,
                "Shore Stone",
                PrimitiveType.Sphere,
                position,
                new Vector3(size, size * 0.48f, size * 0.72f),
                Quaternion.Euler(0f, index * 37f, 0f),
                stoneMaterial,
                false,
                true);
        }
    }

    private static GameObject CreateEllipseDisc(
        Transform parent,
        string name,
        Vector3 center,
        Vector2 radii,
        float height,
        Material material,
        bool addCollider)
    {
        const int segments = 40;
        List<Vector3> vertices = new(segments + 1) { new Vector3(center.x, height, center.z) };
        List<Vector2> uvs = new(segments + 1) { new Vector2(0.5f, 0.5f) };
        List<int> triangles = new(segments * 3);

        for (int index = 0; index < segments; index++)
        {
            float angle = index * Mathf.PI * 2f / segments;
            vertices.Add(new Vector3(
                center.x + Mathf.Cos(angle) * radii.x,
                height,
                center.z + Mathf.Sin(angle) * radii.y));
            uvs.Add(new Vector2(
                Mathf.Cos(angle) * 0.5f + 0.5f,
                Mathf.Sin(angle) * 0.5f + 0.5f));
        }

        for (int index = 0; index < segments; index++)
        {
            triangles.Add(0);
            triangles.Add((index + 1) % segments + 1);
            triangles.Add(index + 1);
        }

        Mesh mesh = new()
        {
            name = name + " Mesh",
            hideFlags = HideFlags.DontSave
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject disc = new(name);
        disc.transform.SetParent(parent);
        disc.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = disc.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;

        if (addCollider)
        {
            disc.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        return disc;
    }

    private void CreateIslandUnderside(Material sourceMaterial)
    {
        const float rimHeight = -0.72f;
        const float lowerHeight = -15f;
        const float tipHeight = -31f;
        Vector3[] outline = GetIslandOutline(0f);
        List<Vector3> vertices = new(outline.Length * 2 + 1);
        foreach (Vector3 point in outline)
        {
            vertices.Add(new Vector3(point.x, rimHeight, point.z));
        }

        foreach (Vector3 point in outline)
        {
            vertices.Add(new Vector3(point.x * 0.43f, lowerHeight, point.z * 0.43f));
        }

        int tipIndex = vertices.Count;
        vertices.Add(new Vector3(0f, tipHeight, 0f));

        List<int> triangles = new(outline.Length * 9);
        for (int index = 0; index < outline.Length; index++)
        {
            int next = (index + 1) % outline.Length;
            int lowerCurrent = outline.Length + index;
            int lowerNext = outline.Length + next;
            triangles.Add(index);
            triangles.Add(next);
            triangles.Add(lowerNext);
            triangles.Add(index);
            triangles.Add(lowerNext);
            triangles.Add(lowerCurrent);
            triangles.Add(lowerCurrent);
            triangles.Add(lowerNext);
            triangles.Add(tipIndex);
        }

        Mesh mesh = new()
        {
            name = "Floating Island Underside",
            hideFlags = HideFlags.DontSave
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Material cliffMaterial = new(sourceMaterial)
        {
            color = new Color(0.20f, 0.18f, 0.14f),
            hideFlags = HideFlags.DontSave
        };
        cliffMaterial.SetInt("_Cull", 0);

        GameObject underside = new("Floating Island Cliffs");
        underside.transform.SetParent(transform);
        underside.AddComponent<MeshFilter>().sharedMesh = mesh;
        underside.AddComponent<MeshRenderer>().sharedMaterial = cliffMaterial;
    }

    private void CreateCloudLayer()
    {
        Material cloudMaterial = CreateMaterial(new Color(0.86f, 0.91f, 0.95f, 0.24f), true);
        cloudMaterial.SetFloat("_Glossiness", 0f);
        cloudMaterial.EnableKeyword("_EMISSION");
        cloudMaterial.SetColor("_EmissionColor", new Color(0.055f, 0.070f, 0.085f));

        GameObject cloudRoot = new("Clouds Below Island");
        cloudRoot.transform.SetParent(transform);
        const int cloudCount = 12;
        for (int index = 0; index < cloudCount; index++)
        {
            float angle = index * Mathf.PI * 2f / cloudCount;
            float radiusX = WorldWidth * Mathf.Lerp(0.48f, 0.64f, Noise01(index * 41 + 3));
            float radiusZ = WorldDepth * Mathf.Lerp(0.48f, 0.64f, Noise01(index * 59 + 7));
            Vector3 center = new(
                Mathf.Cos(angle) * radiusX,
                Mathf.Lerp(-32f, -20f, Noise01(index * 73 + 11)),
                Mathf.Sin(angle) * radiusZ);

            for (int part = 0; part < 3; part++)
            {
                float partOffset = (part - 1) * Mathf.Lerp(6f, 11f, Noise01(index * 113 + part * 17));
                Vector3 tangent = new(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 scale = new(
                    Mathf.Lerp(10f, 19f, Noise01(index * 83 + part * 31 + 13)),
                    Mathf.Lerp(2.2f, 4.2f, Noise01(index * 97 + part * 37 + 17)),
                    Mathf.Lerp(7f, 14f, Noise01(index * 109 + part * 41 + 19)));
                CreateVisualPrimitive(
                    cloudRoot.transform,
                    "Low Cloud Puff",
                    PrimitiveType.Sphere,
                    center + tangent * partOffset + Vector3.up * (part == 1 ? 1.1f : 0f),
                    scale,
                    Quaternion.Euler(0f, index * 29f + part * 17f, 0f),
                    cloudMaterial,
                    false,
                    false);
            }
        }
    }

    private void CreateGrassField()
    {
        GameObject field = new("Wild Grass");
        field.transform.SetParent(transform);

        List<Vector3> vertices = new();
        List<int> triangles = new();
        for (int index = 0; index < GrassTuftTarget; index++)
        {
            float x = Mathf.Lerp(-WorldWidth * 0.49f, WorldWidth * 0.49f, Noise01(index * 13 + 7));
            float z = Mathf.Lerp(-WorldDepth * 0.49f, WorldDepth * 0.49f, Noise01(index * 29 + 19));
            if (!IsInsideIslandSurface(x, z, 2.5f) ||
                IsNearStreet(x, z) ||
                IsInsideLake(x, z, 1.4f) ||
                IsSettlementClearing(x, z, 1.2f))
            {
                continue;
            }

            float height = Mathf.Lerp(0.24f, 0.52f, Noise01(index * 47 + 3));
            float width = Mathf.Lerp(0.07f, 0.13f, Noise01(index * 61 + 11));
            float yaw = Noise01(index * 79 + 23) * Mathf.PI;
            Vector3 center = new(x, 0.015f, z);

            AddGrassQuad(vertices, triangles, center, width, height, yaw);
            AddGrassQuad(vertices, triangles, center, width, height * 0.88f, yaw + Mathf.PI * 0.5f);
        }

        Mesh mesh = new()
        {
            name = "Ruined Town Grass Mesh",
            hideFlags = HideFlags.DontSave
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        field.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = field.AddComponent<MeshRenderer>();
        Material grassMaterial = CreateMaterial(new Color(0.24f, 0.36f, 0.15f));
        grassMaterial.SetInt("_Cull", 0);
        grassMaterial.EnableKeyword("_EMISSION");
        grassMaterial.SetColor("_EmissionColor", new Color(0.025f, 0.045f, 0.012f));
        renderer.sharedMaterial = grassMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private static void AddGrassQuad(
        List<Vector3> vertices,
        List<int> triangles,
        Vector3 center,
        float width,
        float height,
        float yaw)
    {
        Vector3 right = new(Mathf.Cos(yaw) * width, 0f, Mathf.Sin(yaw) * width);
        Vector3 lean = new(-right.z * 0.32f, 0f, right.x * 0.32f);
        int start = vertices.Count;
        vertices.Add(center - right);
        vertices.Add(center + right);
        vertices.Add(center + right * 0.25f + lean + Vector3.up * height);
        vertices.Add(center - right * 0.25f + lean + Vector3.up * height);

        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 1);
        triangles.Add(start);
        triangles.Add(start + 3);
        triangles.Add(start + 2);
    }

    private static bool IsNearStreet(float x, float z)
    {
        return DistanceToStreet(x, z) < 2.25f;
    }

    private static float DistanceToStreet(float x, float z)
    {
        Vector2 point = new(x, z);
        float nearestDistance = float.PositiveInfinity;
        for (int index = 0; index < MainRoadPoints.Length - 1; index++)
        {
            Vector2 start = new(MainRoadPoints[index].x, MainRoadPoints[index].z);
            Vector2 end = new(MainRoadPoints[index + 1].x, MainRoadPoints[index + 1].z);
            Vector2 segment = end - start;
            float denominator = segment.sqrMagnitude;
            float t = denominator <= 0.0001f
                ? 0f
                : Mathf.Clamp01(Vector2.Dot(point - start, segment) / denominator);

            nearestDistance = Mathf.Min(nearestDistance, Vector2.Distance(point, start + segment * t));
        }

        return nearestDistance;
    }

    private static bool IsInsideLake(float x, float z, float padding)
    {
        foreach (LakeLayout lake in Lakes)
        {
            float radiusX = lake.Radii.x + padding;
            float radiusZ = lake.Radii.y + padding;
            float normalizedX = (x - lake.Center.x) / radiusX;
            float normalizedZ = (z - lake.Center.z) / radiusZ;
            if (normalizedX * normalizedX + normalizedZ * normalizedZ <= 1f)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSettlementClearing(float x, float z, float padding)
    {
        Vector2 point = new(x, z);
        if (Vector2.Distance(point, new Vector2(TownHallPosition.x, TownHallPosition.z)) < 12f + padding)
        {
            return true;
        }

        Vector2[] ruinCenters =
        {
            new(-34f, 8f),
            new(12f, -24f),
            new(13f, 20f)
        };
        foreach (Vector2 center in ruinCenters)
        {
            if (Vector2.Distance(point, center) < 7f + padding)
            {
                return true;
            }
        }

        return false;
    }

    private static float Noise01(int value)
    {
        unchecked
        {
            uint hash = (uint)value;
            hash ^= hash >> 16;
            hash *= 0x7feb352d;
            hash ^= hash >> 15;
            hash *= 0x846ca68b;
            hash ^= hash >> 16;
            return (hash & 0x00ffffff) / 16777215f;
        }
    }

    private void CreateStreetPolyline(Vector3[] points, float width, Material edgeMaterial, Material surfaceMaterial)
    {
        Material cobbleMaterial = CreateMaterial(new Color(0.34f, 0.34f, 0.30f));
        CreateRoadRibbon("Road Edge", points, width + 0.48f, 0.016f, edgeMaterial);
        CreateRoadRibbon("Packed Earth Road", points, width, 0.032f, surfaceMaterial);

        for (int index = 0; index < points.Length - 1; index++)
        {
            Vector3 start = points[index];
            Vector3 end = points[index + 1];
            Vector3 direction = end - start;
            float length = direction.magnitude;
            if (length <= 0.01f)
            {
                continue;
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);
            float yaw = rotation.eulerAngles.y;
            Vector3 side = rotation * Vector3.forward;

            int stoneCount = Mathf.Max(2, Mathf.RoundToInt(length / 12f));
            for (int stoneIndex = 0; stoneIndex < stoneCount; stoneIndex++)
            {
                float t = (stoneIndex + 1f) / (stoneCount + 1f);
                float lateral = (Noise01(index * 97 + stoneIndex * 17 + 5) - 0.5f) * width * 0.62f;
                Vector3 stonePosition = Vector3.Lerp(start, end, t) + side * lateral + Vector3.up * 0.07f;
                float stoneScale = Mathf.Lerp(0.35f, 0.62f, Noise01(index * 131 + stoneIndex * 23 + 9));
                CreateVisualPrimitive(
                    transform,
                    "Exposed Street Stone",
                    PrimitiveType.Cube,
                    stonePosition,
                    new Vector3(stoneScale, 0.08f, stoneScale * 0.72f),
                    Quaternion.Euler(0f, yaw + stoneIndex * 31f, 0f),
                    cobbleMaterial,
                    false,
                    true);
            }
        }
    }

    private void CreateRoadRibbon(string name, Vector3[] points, float width, float height, Material material)
    {
        if (points == null || points.Length < 2)
        {
            return;
        }

        List<Vector3> vertices = new(points.Length * 2);
        List<int> triangles = new((points.Length - 1) * 6);
        float halfWidth = width * 0.5f;

        for (int index = 0; index < points.Length; index++)
        {
            Vector3 previousDirection = index > 0
                ? (points[index] - points[index - 1]).normalized
                : (points[1] - points[0]).normalized;
            Vector3 nextDirection = index < points.Length - 1
                ? (points[index + 1] - points[index]).normalized
                : previousDirection;
            Vector3 tangent = previousDirection + nextDirection;
            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = nextDirection;
            }

            tangent.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
            Vector3 center = points[index] + Vector3.up * height;
            vertices.Add(center - side * halfWidth);
            vertices.Add(center + side * halfWidth);
        }

        for (int index = 0; index < points.Length - 1; index++)
        {
            int start = index * 2;
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        Mesh mesh = new()
        {
            name = $"{name} Mesh",
            hideFlags = HideFlags.DontSave
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject road = new(name);
        road.transform.SetParent(transform);
        road.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = road.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private Camera CreateCamera(Bounds playBounds)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(transform);

        Camera worldCamera = cameraObject.AddComponent<Camera>();
        worldCamera.clearFlags = CameraClearFlags.Skybox;
        worldCamera.backgroundColor = new Color(0.44f, 0.66f, 0.86f);
        worldCamera.fieldOfView = 47f;
        worldCamera.nearClipPlane = 0.1f;
        worldCamera.farClipPlane = 900f;
        worldCamera.allowHDR = true;
        worldCamera.allowMSAA = true;

        RtsCameraController controller = cameraObject.AddComponent<RtsCameraController>();
        controller.Initialize(TownHallPosition + new Vector3(5f, 0f, 2f), playBounds);
        return worldCamera;
    }

    private void CreateLighting(out Light sunlight, out Light fill)
    {
        GameObject sunObject = new("Low Frontier Sun");
        sunObject.transform.SetParent(transform);
        sunObject.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

        sunlight = sunObject.AddComponent<Light>();
        sunlight.type = LightType.Directional;
        sunlight.color = new Color(1f, 0.79f, 0.58f);
        sunlight.intensity = 1.35f;
        sunlight.shadows = LightShadows.Soft;

        GameObject fillObject = new("Cool Sky Fill");
        fillObject.transform.SetParent(transform);
        fillObject.transform.rotation = Quaternion.Euler(55f, 145f, 0f);

        fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.36f, 0.46f, 0.62f);
        fill.intensity = 0.34f;
        fill.shadows = LightShadows.None;

        Shader skyboxShader = Shader.Find("Skybox/Procedural");
        if (skyboxShader != null)
        {
            Material skyboxMaterial = new(skyboxShader)
            {
                name = "Floating Island Sky",
                hideFlags = HideFlags.DontSave
            };
            skyboxMaterial.SetColor("_SkyTint", new Color(0.28f, 0.52f, 0.82f));
            skyboxMaterial.SetColor("_GroundColor", new Color(0.24f, 0.44f, 0.66f));
            skyboxMaterial.SetFloat("_AtmosphereThickness", 0.82f);
            skyboxMaterial.SetFloat("_SunSize", 0.045f);
            skyboxMaterial.SetFloat("_SunSizeConvergence", 4f);
            skyboxMaterial.SetFloat("_Exposure", 0.82f);
            RenderSettings.skybox = skyboxMaterial;
        }
        RenderSettings.ambientLight = new Color(0.25f, 0.27f, 0.31f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.39f, 0.56f, 0.72f);
        RenderSettings.fogDensity = 0.0025f;
    }

    private void CreateVillage(
        GridNavigationService navigation,
        Material woodMaterial,
        Material burnedWoodMaterial,
        Material ashMaterial,
        Material foliageMaterial,
        Material foliageLightMaterial,
        Material stoneMaterial,
        bool includeEnvironment)
    {
        GameObject village = new("Zastava Settlement");
        village.transform.SetParent(transform);

        CreateImportedBuilding(village.transform, "Hero's Town Hall", "Inn", TownHallPosition, 158f, 9.0f, false, 0, burnedWoodMaterial, ashMaterial, navigation);

        if (includeEnvironment)
        {
            CreateImportedBuilding(village.transform, "Collapsed Chapel", "House_3", new Vector3(-34f, 0f, 8f), 132f, 7.2f, true, 0, burnedWoodMaterial, ashMaterial, navigation);
            CreateImportedBuilding(village.transform, "Burned Workshop", "Blacksmith", new Vector3(12f, 0f, -24f), -28f, 6.6f, true, 0, burnedWoodMaterial, ashMaterial, navigation);
            CreateImportedBuilding(village.transform, "Old Gate Remnant", "Stable", new Vector3(13f, 0f, 20f), -118f, 7.6f, true, 0, burnedWoodMaterial, ashMaterial, navigation);
            CreateVillageTrees(village.transform, woodMaterial, foliageMaterial, foliageLightMaterial, stoneMaterial, navigation);
            CreateRuinsProps(village.transform, navigation);
        }
    }

    private void CreateImportedBuilding(
        Transform parent,
        string name,
        string modelName,
        Vector3 groundPosition,
        float yaw,
        float targetFootprint,
        bool isRuined,
        int restorationCost,
        Material burnedWoodMaterial,
        Material ashMaterial,
        GridNavigationService navigation)
    {
        if (isRuined)
        {
            CreateDecorativeRuin(parent, name, groundPosition, yaw, targetFootprint, burnedWoodMaterial, ashMaterial, navigation);
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(VillageModelPath + modelName);
        if (prefab == null)
        {
            CreateFallbackHouse(parent, name, groundPosition, yaw, targetFootprint, isRuined, restorationCost, burnedWoodMaterial, ashMaterial, navigation);
            return;
        }

        GameObject wrapper = new(name);
        wrapper.transform.SetParent(parent);

        GameObject model = Instantiate(prefab, wrapper.transform);
        model.name = "Quaternius Art";
        Vector3 importedScale = model.transform.localScale;
        Quaternion importedRotation = model.transform.localRotation;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * importedRotation;
        model.transform.localScale = importedScale;

        DisableColliders(model);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (!TryGetRendererBounds(renderers, out Bounds initialBounds))
        {
            Destroy(wrapper);
            CreateFallbackHouse(parent, name, groundPosition, yaw, targetFootprint, isRuined, restorationCost, burnedWoodMaterial, ashMaterial, navigation);
            return;
        }

        float horizontalSize = Mathf.Max(initialBounds.size.x, initialBounds.size.z);
        float modelScale = horizontalSize > 0.001f ? targetFootprint / horizontalSize : 1f;
        model.transform.localScale = importedScale * modelScale;

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

        GameObject damageVisual = isRuined
            ? CreateBuildingDamage(parent, name, finalBounds, yaw, burnedWoodMaterial, ashMaterial)
            : null;
        TownBuilding building = wrapper.AddComponent<TownBuilding>();
        building.Initialize(name, restorationCost, isRuined, renderers, damageVisual, finalBounds, model);
    }

    private void CreateDecorativeRuin(
        Transform parent,
        string name,
        Vector3 groundPosition,
        float yaw,
        float footprint,
        Material burnedWoodMaterial,
        Material ashMaterial,
        GridNavigationService navigation)
    {
        Bounds bounds = new(
            groundPosition + Vector3.up * 1.5f,
            new Vector3(footprint, 3f, footprint * 0.72f));
        GameObject ruin = CreateBuildingDamage(parent, name, bounds, yaw, burnedWoodMaterial, ashMaterial);

        BoxCollider blocker = ruin.AddComponent<BoxCollider>();
        blocker.center = new Vector3(0f, 1.15f, 0f);
        blocker.size = new Vector3(footprint * 0.78f, 2.3f, footprint * 0.58f);
        Physics.SyncTransforms();
        navigation.SetBlocked(blocker.bounds, 0.12f);
    }

    private void CreateFallbackHouse(
        Transform parent,
        string name,
        Vector3 groundPosition,
        float yaw,
        float targetFootprint,
        bool isRuined,
        int restorationCost,
        Material burnedWoodMaterial,
        Material ashMaterial,
        GridNavigationService navigation)
    {
        Material wall = CreateMaterial(new Color(0.58f, 0.47f, 0.32f));
        Material roof = CreateMaterial(new Color(0.26f, 0.12f, 0.08f));
        Material timber = CreateMaterial(WoodColor);

        GameObject house = new(name + " (Fallback)");
        house.transform.SetParent(parent);
        house.transform.position = groundPosition;
        house.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        GameObject restoredArt = new("Restored Fallback Art");
        restoredArt.transform.SetParent(house.transform, false);
        CreateVisualPrimitive(restoredArt.transform, "Plastered Walls", PrimitiveType.Cube, new Vector3(0f, 1.1f, 0f), new Vector3(targetFootprint, 2.2f, targetFootprint * 0.72f), Quaternion.identity, wall, false, true);
        CreateVisualPrimitive(restoredArt.transform, "Roof Left", PrimitiveType.Cube, new Vector3(-0.86f, 2.35f, 0f), new Vector3(targetFootprint * 0.62f, 0.28f, targetFootprint * 0.88f), Quaternion.Euler(0f, 0f, -30f), roof, false, true);
        CreateVisualPrimitive(restoredArt.transform, "Roof Right", PrimitiveType.Cube, new Vector3(0.86f, 2.35f, 0f), new Vector3(targetFootprint * 0.62f, 0.28f, targetFootprint * 0.88f), Quaternion.Euler(0f, 0f, 30f), roof, false, true);
        CreateVisualPrimitive(restoredArt.transform, "Door", PrimitiveType.Cube, new Vector3(0f, 0.8f, -targetFootprint * 0.37f), new Vector3(0.8f, 1.55f, 0.12f), Quaternion.identity, timber, false, true);

        BoxCollider blocker = house.AddComponent<BoxCollider>();
        blocker.center = new Vector3(0f, 1.1f, 0f);
        blocker.size = new Vector3(targetFootprint, 2.2f, targetFootprint * 0.72f);
        Physics.SyncTransforms();
        navigation.SetBlocked(blocker.bounds, 0.22f);

        Renderer[] renderers = restoredArt.GetComponentsInChildren<Renderer>();
        GameObject damageVisual = isRuined
            ? CreateBuildingDamage(parent, name, blocker.bounds, yaw, burnedWoodMaterial, ashMaterial)
            : null;
        TownBuilding building = house.AddComponent<TownBuilding>();
        building.Initialize(name, restorationCost, isRuined, renderers, damageVisual, blocker.bounds, restoredArt);
    }

    private GameObject CreateBuildingDamage(
        Transform parent,
        string buildingName,
        Bounds bounds,
        float yaw,
        Material burnedWoodMaterial,
        Material ashMaterial)
    {
        GameObject damage = new(buildingName + " Ruins");
        damage.transform.SetParent(parent);
        damage.transform.position = new Vector3(bounds.center.x, 0f, bounds.center.z);
        damage.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        Material wallMaterial = CreateMaterial(new Color(0.42f, 0.38f, 0.31f));
        Material brokenPlasterMaterial = CreateMaterial(new Color(0.50f, 0.43f, 0.33f));
        Material rubbleMaterial = CreateMaterial(new Color(0.29f, 0.30f, 0.27f));
        float width = Mathf.Max(3.6f, bounds.size.x * 0.82f);
        float depth = Mathf.Max(3.2f, bounds.size.z * 0.78f);
        int seed = StableSeed(buildingName);

        CreateVisualPrimitive(damage.transform, "Cracked Foundation", PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(width, 0.18f, depth), Quaternion.identity, rubbleMaterial, false, true);
        CreateVisualPrimitive(damage.transform, "Ash Floor", PrimitiveType.Cylinder, new Vector3(0f, 0.20f, 0f), new Vector3(width * 0.43f, 0.025f, depth * 0.42f), Quaternion.identity, ashMaterial, false);

        float rearLeftHeight = Mathf.Lerp(1.8f, 3.6f, Noise01(seed + 3));
        float rearRightHeight = Mathf.Lerp(1.3f, 2.8f, Noise01(seed + 7));
        float sideHeight = Mathf.Lerp(1.7f, 3.2f, Noise01(seed + 11));
        CreateRuinWallChunk(damage.transform, "Rear Wall Left", new Vector3(-width * 0.27f, rearLeftHeight * 0.5f, depth * 0.43f), new Vector3(width * 0.40f, rearLeftHeight, 0.30f), Quaternion.Euler(0f, 0f, -3f), wallMaterial);
        CreateRuinWallChunk(damage.transform, "Rear Wall Right", new Vector3(width * 0.31f, rearRightHeight * 0.5f, depth * 0.43f), new Vector3(width * 0.27f, rearRightHeight, 0.30f), Quaternion.Euler(0f, 0f, 4f), brokenPlasterMaterial);
        CreateRuinWallChunk(damage.transform, "Side Wall", new Vector3(-width * 0.44f, sideHeight * 0.5f, depth * 0.05f), new Vector3(0.30f, sideHeight, depth * 0.66f), Quaternion.Euler(2f, 0f, 0f), wallMaterial);
        CreateRuinWallChunk(damage.transform, "Front Corner", new Vector3(width * 0.42f, 0.72f, -depth * 0.34f), new Vector3(0.34f, 1.44f, depth * 0.24f), Quaternion.Euler(-3f, 0f, 5f), brokenPlasterMaterial);

        CreateVisualPrimitive(damage.transform, "Charred Crossbeam", PrimitiveType.Cube, new Vector3(0.08f, 0.48f, -0.10f), new Vector3(width * 0.70f, 0.24f, 0.24f), Quaternion.Euler(8f, 24f, 7f), burnedWoodMaterial, false, true);
        CreateVisualPrimitive(damage.transform, "Broken Rafter", PrimitiveType.Cube, new Vector3(-width * 0.08f, 0.34f, depth * 0.10f), new Vector3(depth * 0.58f, 0.18f, 0.18f), Quaternion.Euler(-6f, -42f, 12f), burnedWoodMaterial, false, true);

        for (int index = 0; index < 11; index++)
        {
            float rubbleX = Mathf.Lerp(-width * 0.46f, width * 0.46f, Noise01(seed + 31 + index * 5));
            float rubbleZ = Mathf.Lerp(-depth * 0.46f, depth * 0.46f, Noise01(seed + 47 + index * 7));
            float rubbleSize = Mathf.Lerp(0.24f, 0.62f, Noise01(seed + 71 + index * 3));
            CreateVisualPrimitive(
                damage.transform,
                "Masonry Rubble",
                PrimitiveType.Cube,
                new Vector3(rubbleX, rubbleSize * 0.30f + 0.16f, rubbleZ),
                new Vector3(rubbleSize, rubbleSize * 0.55f, rubbleSize * 0.72f),
                Quaternion.Euler(index * 13f % 24f, index * 37f, index * 9f % 18f),
                index % 3 == 0 ? brokenPlasterMaterial : rubbleMaterial,
                false,
                true);
        }

        string modeledWall = seed % 4 == 0
            ? "Wall_ArchRound_Broken"
            : seed % 4 == 1
                ? "Wall_Double_Broken"
                : seed % 4 == 2
                    ? "Wall_Hole"
                    : "Wall_Broken";
        CreateImportedRuinProp(
            damage.transform,
            modeledWall,
            "Detailed Masonry Remnant",
            new Vector3(bounds.center.x, 0f, bounds.center.z),
            yaw,
            Mathf.Lerp(2.1f, 3.0f, Noise01(seed + 113)),
            false,
            null);

        return damage;
    }

    private void CreateRuinWallChunk(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
    {
        CreateVisualPrimitive(parent, name, PrimitiveType.Cube, position, scale, rotation, material, false, true);

        float capHeight = Mathf.Min(0.22f, scale.y * 0.12f);
        CreateVisualPrimitive(
            parent,
            "Jagged Wall Cap",
            PrimitiveType.Cube,
            position + Vector3.up * (scale.y * 0.5f),
            new Vector3(scale.x * 0.92f, capHeight, scale.z * 1.08f),
            rotation * Quaternion.Euler(0f, 0f, 6f),
            material,
            false,
            true);
    }

    private static int StableSeed(string text)
    {
        int seed = 17;
        foreach (char character in text)
        {
            seed = seed * 31 + character;
        }

        return seed;
    }

    private void CreatePalisade(Transform parent, Material material, GridNavigationService navigation)
    {
        CreateFenceSection(parent, new Vector3(-38f, 0f, 28f), 12, Vector3.right, material, navigation);
        CreateFenceSection(parent, new Vector3(21f, 0f, 28f), 12, Vector3.right, material, navigation);
        CreateFenceSection(parent, new Vector3(-38f, 0f, -28f), 12, Vector3.right, material, navigation);
        CreateFenceSection(parent, new Vector3(20f, 0f, -28f), 12, Vector3.right, material, navigation);
        CreateFenceSection(parent, new Vector3(-42f, 0f, -18f), 12, Vector3.forward, material, navigation);
        CreateFenceSection(parent, new Vector3(-42f, 0f, 10f), 12, Vector3.forward, material, navigation);
        CreateFenceSection(parent, new Vector3(42f, 0f, -18f), 12, Vector3.forward, material, navigation);
        CreateFenceSection(parent, new Vector3(42f, 0f, 10f), 12, Vector3.forward, material, navigation);
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
        List<Vector2> treePositions = new(TreeTarget);
        int placedTrees = 0;
        for (int attempt = 0; attempt < TreeTarget * 38 && placedTrees < TreeTarget; attempt++)
        {
            bool sparseTree = placedTrees >= TreeTarget - 30;
            float x;
            float z;
            if (sparseTree)
            {
                x = Mathf.Lerp(-WorldWidth * 0.47f, WorldWidth * 0.47f, Noise01(attempt * 37 + 101));
                z = Mathf.Lerp(-WorldDepth * 0.47f, WorldDepth * 0.47f, Noise01(attempt * 71 + 211));
            }
            else
            {
                ForestLayout forest = Forests[attempt % Forests.Length];
                float angle = Noise01(attempt * 53 + 307) * Mathf.PI * 2f;
                float radius = Mathf.Sqrt(Noise01(attempt * 83 + 419));
                x = forest.Center.x + Mathf.Cos(angle) * forest.Radii.x * radius;
                z = forest.Center.y + Mathf.Sin(angle) * forest.Radii.y * radius;
            }

            if (!IsNaturePositionAllowed(x, z, 5.8f, 4.0f, 2.5f))
            {
                continue;
            }

            Vector2 candidate = new(x, z);
            bool overlapsTree = false;
            foreach (Vector2 treePosition in treePositions)
            {
                float minimumSpacing = sparseTree ? 5.4f : 3.9f;
                if ((treePosition - candidate).sqrMagnitude < minimumSpacing * minimumSpacing)
                {
                    overlapsTree = true;
                    break;
                }
            }

            if (overlapsTree)
            {
                continue;
            }

            treePositions.Add(candidate);
            Vector3 position = new(x, 0f, z);
            bool imported = placedTrees % 6 != 0 && CreateImportedNature(
                parent,
                placedTrees % 2 == 0 ? "BirchTree_1" : "BirchTree_3",
                "Island Birch",
                position,
                Noise01(attempt * 89 + 17) * 360f,
                Mathf.Lerp(5.2f, 7.3f, Noise01(attempt * 107 + 29)),
                true,
                navigation);

            if (!imported)
            {
                CreateTree(
                    parent,
                    position,
                    Mathf.Lerp(0.85f, 1.18f, Noise01(attempt * 113 + 31)),
                    trunkMaterial,
                    placedTrees % 2 == 0 ? foliageMaterial : foliageLightMaterial,
                    navigation);
            }

            placedTrees++;
        }

        CreateVillageUndergrowth(parent, navigation);

        CreateRock(parent, new Vector3(-62f, 0f, -18f), new Vector3(1.8f, 0.8f, 1.1f), 18f, stoneMaterial, navigation);
        CreateRock(parent, new Vector3(54f, 0f, 7f), new Vector3(1.4f, 0.65f, 1.0f), -12f, stoneMaterial, navigation);
        CreateRock(parent, new Vector3(-28f, 0f, 52f), new Vector3(2.2f, 0.9f, 1.3f), 28f, stoneMaterial, navigation);
        CreateRock(parent, new Vector3(31f, 0f, -57f), new Vector3(1.7f, 0.7f, 1.2f), -18f, stoneMaterial, navigation);
        CreateRock(parent, new Vector3(128f, 0f, -28f), new Vector3(2.0f, 0.8f, 1.0f), 42f, stoneMaterial, navigation);
    }

    private void CreateVillageUndergrowth(Transform parent, GridNavigationService navigation)
    {
        int placedBushes = 0;
        for (int attempt = 0; attempt < BushTarget * 20 && placedBushes < BushTarget; attempt++)
        {
            float x = Mathf.Lerp(-WorldWidth * 0.47f, WorldWidth * 0.47f, Noise01(attempt * 43 + 401));
            float z = Mathf.Lerp(-WorldDepth * 0.47f, WorldDepth * 0.47f, Noise01(attempt * 79 + 503));
            if (!IsNaturePositionAllowed(x, z, 3.2f, 1.5f, 1.8f))
            {
                continue;
            }

            string model = placedBushes % 3 == 0 ? "Bush_Large" : placedBushes % 3 == 1 ? "Bush" : "Bush_Small";
            CreateImportedNature(
                parent,
                model,
                "Wild Bush",
                new Vector3(x, 0f, z),
                Noise01(attempt * 97 + 607) * 360f,
                Mathf.Lerp(0.72f, 1.18f, Noise01(attempt * 109 + 701)),
                false,
                navigation);
            placedBushes++;
        }
    }

    private static bool IsNaturePositionAllowed(
        float x,
        float z,
        float roadClearance,
        float lakePadding,
        float settlementPadding)
    {
        if (!IsInsideIslandSurface(x, z, 6f) ||
            DistanceToStreet(x, z) < roadClearance ||
            IsInsideLake(x, z, lakePadding) ||
            IsSettlementClearing(x, z, settlementPadding))
        {
            return false;
        }

        Vector2 point = new(x, z);
        Vector2[] resourceCenters =
        {
            new(42f, -25f),
            new(-48f, 18f),
            new(28f, -42f),
            new(-52f, -35f),
            new(5f, 38f)
        };
        foreach (Vector2 resourceCenter in resourceCenters)
        {
            if (Vector2.Distance(point, resourceCenter) < 5.5f)
            {
                return false;
            }
        }

        return true;
    }

    private bool CreateImportedNature(
        Transform parent,
        string modelName,
        string displayName,
        Vector3 groundPosition,
        float yaw,
        float targetHeight,
        bool blocksNavigation,
        GridNavigationService navigation)
    {
        GameObject prefab = Resources.Load<GameObject>(NatureModelPath + modelName);
        if (prefab == null)
        {
            return false;
        }

        GameObject wrapper = new(displayName);
        wrapper.transform.SetParent(parent);

        GameObject model = Instantiate(prefab, wrapper.transform);
        model.name = "Quaternius Nature Art";
        Vector3 importedScale = model.transform.localScale;
        Quaternion importedRotation = model.transform.localRotation;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * importedRotation;
        model.transform.localScale = importedScale;
        DisableColliders(model);
        ConfigureNatureMaterials(model);

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (!TryGetRendererBounds(renderers, out Bounds initialBounds))
        {
            Destroy(wrapper);
            return false;
        }

        float scale = initialBounds.size.y > 0.001f ? targetHeight / initialBounds.size.y : 1f;
        model.transform.localScale = importedScale * scale;
        if (!TryGetRendererBounds(renderers, out Bounds scaledBounds))
        {
            Destroy(wrapper);
            return false;
        }

        wrapper.transform.position += new Vector3(
            groundPosition.x - scaledBounds.center.x,
            groundPosition.y - scaledBounds.min.y,
            groundPosition.z - scaledBounds.center.z);

        foreach (Renderer renderer in renderers)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        if (blocksNavigation && TryGetRendererBounds(renderers, out Bounds finalBounds))
        {
            CapsuleCollider blocker = wrapper.AddComponent<CapsuleCollider>();
            blocker.center = wrapper.transform.InverseTransformPoint(finalBounds.center);
            blocker.height = Mathf.Max(1f, finalBounds.size.y);
            blocker.radius = Mathf.Max(0.25f, Mathf.Min(finalBounds.extents.x, finalBounds.extents.z) * 0.35f);
            Physics.SyncTransforms();
            navigation.SetBlocked(blocker.bounds, 0.16f);
        }

        if (displayName == "Island Birch")
        {
            ResourceNode node = wrapper.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Timber, 32, 4);
            node.ConfigureRenewal(2, 18f);
        }

        return true;
    }

    private static void ConfigureKenneyLandscapeMaterials(GameObject model, bool grassyHill)
    {
        Color baseColor = grassyHill
            ? new Color(0.30f, 0.38f, 0.19f)
            : new Color(0.34f, 0.36f, 0.33f);
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.materials;
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];
                if (material == null)
                {
                    material = CreateMaterial(baseColor);
                    materials[index] = material;
                    continue;
                }

                material.shader = Shader.Find("Standard");
                material.color = Color.Lerp(baseColor, Color.white, index * 0.06f);
                material.SetFloat("_Glossiness", 0.08f);
                material.SetFloat("_Metallic", 0f);
            }

            renderer.materials = materials;
        }
    }

    private static void ConfigureNatureMaterials(GameObject model)
    {
        Texture2D birchBark = Resources.Load<Texture2D>(NatureModelPath + "BirchTree_Bark");
        Texture2D birchNormal = Resources.Load<Texture2D>(NatureModelPath + "BirchTree_Bark_Normal");
        Texture2D birchLeaves = Resources.Load<Texture2D>(NatureModelPath + "BirchTree_Leaves");
        Texture2D bushLeaves = Resources.Load<Texture2D>(NatureModelPath + "Bush_Leaves");

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                string materialName = material.name;
                material.shader = Shader.Find("Standard");
                material.color = Color.white;
                material.SetFloat("_Glossiness", 0.08f);
                material.SetFloat("_Metallic", 0f);

                if (materialName.Contains("BirchTree_Bark"))
                {
                    material.mainTexture = birchBark;
                    material.SetTexture("_BumpMap", birchNormal);
                    material.SetFloat("_BumpScale", 0.55f);
                    material.EnableKeyword("_NORMALMAP");
                    SetOpaque(material);
                }
                else if (materialName.Contains("BirchTree_Leaves"))
                {
                    material.mainTexture = birchLeaves;
                    material.color = new Color(0.42f, 0.48f, 0.22f);
                    SetCutout(material, 0.32f);
                }
                else if (materialName.Contains("Bush_Leaves"))
                {
                    material.mainTexture = bushLeaves;
                    material.color = new Color(0.44f, 0.48f, 0.24f);
                    SetCutout(material, 0.34f);
                }
            }

            renderer.materials = materials;
        }
    }

    private void CreateRuinsProps(Transform parent, GridNavigationService navigation)
    {
        CreateImportedRuinProp(parent, "Cart", "Abandoned Cart", TownHallPosition + new Vector3(-7f, 0f, 5f), 24f, 1.45f, true, navigation);
        CreateImportedRuinProp(parent, "Barrel", "Rain Barrel", TownHallPosition + new Vector3(5.2f, 0f, -1.8f), -8f, 0.95f, false, navigation);
        CreateImportedRuinProp(parent, "Crate", "Salvage Crate", TownHallPosition + new Vector3(4.5f, 0f, -2.8f), 18f, 0.82f, false, navigation);
        CreateImportedRuinProp(parent, "Bricks", "Loose Bricks", new Vector3(15.5f, 0f, -20f), 51f, 0.48f, false, navigation);
        CreateImportedRuinProp(parent, "DeadTree_1", "Dead Town Tree", new Vector3(36f, 0f, -9f), -32f, 5.4f, true, navigation);
    }

    private bool CreateImportedRuinProp(
        Transform parent,
        string modelName,
        string displayName,
        Vector3 groundPosition,
        float yaw,
        float targetHeight,
        bool blocksNavigation,
        GridNavigationService navigation)
    {
        GameObject prefab = Resources.Load<GameObject>(RuinsModelPath + modelName);
        if (prefab == null)
        {
            return false;
        }

        GameObject wrapper = new(displayName);
        wrapper.transform.SetParent(parent);

        GameObject model = Instantiate(prefab, wrapper.transform);
        model.name = "Quaternius Ruins Art";
        Vector3 importedScale = model.transform.localScale;
        Quaternion importedRotation = model.transform.localRotation;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * importedRotation;
        model.transform.localScale = importedScale;
        DisableColliders(model);
        ConfigureRuinPropMaterials(model);

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (!TryGetRendererBounds(renderers, out Bounds initialBounds))
        {
            Destroy(wrapper);
            return false;
        }

        float scale = initialBounds.size.y > 0.001f ? targetHeight / initialBounds.size.y : 1f;
        model.transform.localScale = importedScale * scale;
        if (!TryGetRendererBounds(renderers, out Bounds scaledBounds))
        {
            Destroy(wrapper);
            return false;
        }

        wrapper.transform.position += new Vector3(
            groundPosition.x - scaledBounds.center.x,
            groundPosition.y - scaledBounds.min.y,
            groundPosition.z - scaledBounds.center.z);

        foreach (Renderer renderer in renderers)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        if (blocksNavigation && TryGetRendererBounds(renderers, out Bounds finalBounds))
        {
            BoxCollider blocker = wrapper.AddComponent<BoxCollider>();
            blocker.center = wrapper.transform.InverseTransformPoint(finalBounds.center);
            blocker.size = new Vector3(
                Mathf.Max(0.4f, finalBounds.size.x * 0.75f),
                Mathf.Max(0.6f, finalBounds.size.y),
                Mathf.Max(0.4f, finalBounds.size.z * 0.75f));
            Physics.SyncTransforms();
            navigation.SetBlocked(blocker.bounds, 0.10f);
        }

        return true;
    }

    private static void ConfigureRuinPropMaterials(GameObject model)
    {
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                material.shader = Shader.Find("Standard");
                material.SetFloat("_Glossiness", 0.08f);
                material.SetFloat("_Metallic", material.name.Contains("Metal") ? 0.28f : 0f);

                if (material.name.Contains("DarkWood"))
                {
                    material.color = new Color(0.14f, 0.085f, 0.045f);
                }
                else if (material.name.Contains("Wood") || material.name.Contains("Bark"))
                {
                    material.color = new Color(0.30f, 0.18f, 0.09f);
                }
                else if (material.name.Contains("Metal"))
                {
                    material.color = new Color(0.24f, 0.26f, 0.25f);
                }
                else if (material.name.Contains("Highlights"))
                {
                    material.color = new Color(0.54f, 0.46f, 0.34f);
                }
                else
                {
                    material.color = new Color(0.36f, 0.33f, 0.28f);
                }
            }

            renderer.materials = materials;
        }
    }

    private static void SetOpaque(Material material)
    {
        material.SetFloat("_Mode", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = -1;
    }

    private static void SetCutout(Material material, float cutoff)
    {
        material.SetFloat("_Mode", 1f);
        material.SetFloat("_Cutoff", cutoff);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.EnableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
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
        ResourceNode node = root.AddComponent<ResourceNode>();
        node.Configure(ResourceType.Timber, 28, 4);
        node.ConfigureRenewal(2, 18f);
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

    private void CreateLandscapeRelief(GridNavigationService navigation)
    {
        GameObject root = new("Island Relief and Stone Veins");
        root.transform.SetParent(transform);

        CreateKenneyLandscape(root.transform, "Kenney/Castle/ground-hills", "Windward Hills", new Vector3(-142f, 0f, 36f), 28f, 7.5f, navigation, false);
        CreateKenneyLandscape(root.transform, "Kenney/Nature/cliff_large_rock", "Northern Escarpment", new Vector3(-30f, 0f, 101f), -18f, 10f, navigation, false);
        CreateKenneyLandscape(root.transform, "Kenney/Nature/cliff_blockSlope_rock", "Eastern Ridge", new Vector3(148f, 0f, 30f), 65f, 8.5f, navigation, false);
        CreateKenneyLandscape(root.transform, "Kenney/Nature/cliff_block_rock", "Southern Ridge", new Vector3(45f, 0f, -103f), 12f, 8f, navigation, false);
        CreateKenneyLandscape(root.transform, "Kenney/Castle/ground-hills", "Lake Hills", new Vector3(112f, 0f, 94f), -32f, 5.5f, navigation, false);

        CreateKenneyLandscape(root.transform, "Kenney/Nature/rock_largeA", "Deep Stone Vein", new Vector3(-78f, 0f, 44f), 22f, 3.6f, navigation, true);
        CreateKenneyLandscape(root.transform, "Kenney/Nature/rock_largeB", "Deep Stone Vein", new Vector3(72f, 0f, -52f), -35f, 3.9f, navigation, true);
        CreateKenneyLandscape(root.transform, "Kenney/Nature/rock_largeC", "Deep Stone Vein", new Vector3(112f, 0f, 8f), 58f, 4.2f, navigation, true);
        CreateKenneyLandscape(root.transform, "Kenney/Nature/rock_largeD", "Deep Stone Vein", new Vector3(-104f, 0f, -10f), 6f, 3.8f, navigation, true);
        CreateKenneyLandscape(root.transform, "Kenney/Nature/rock_tallA", "Deep Stone Vein", new Vector3(54f, 0f, 78f), -16f, 4.6f, navigation, true);
        CreateKenneyLandscape(root.transform, "Kenney/Nature/rock_tallC", "Deep Stone Vein", new Vector3(-48f, 0f, -78f), 33f, 4.4f, navigation, true);
    }

    private bool CreateKenneyLandscape(
        Transform parent,
        string modelPath,
        string displayName,
        Vector3 groundPosition,
        float yaw,
        float targetHeight,
        GridNavigationService navigation,
        bool stoneResource)
    {
        GameObject prefab = Resources.Load<GameObject>("Models/" + modelPath);
        if (prefab == null)
        {
            return false;
        }

        GameObject wrapper = new(displayName);
        wrapper.transform.SetParent(parent);
        GameObject model = Instantiate(prefab, wrapper.transform);
        model.name = "Kenney Landscape Art";
        Vector3 importedScale = model.transform.localScale;
        Quaternion importedRotation = model.transform.localRotation;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * importedRotation;
        model.transform.localScale = importedScale;
        DisableColliders(model);
        ConfigureKenneyLandscapeMaterials(model, modelPath.Contains("ground-hills"));

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (!TryGetRendererBounds(renderers, out Bounds initialBounds))
        {
            Destroy(wrapper);
            return false;
        }

        float scale = initialBounds.size.y > 0.001f ? targetHeight / initialBounds.size.y : 1f;
        model.transform.localScale = importedScale * scale;
        if (!TryGetRendererBounds(renderers, out Bounds scaledBounds))
        {
            Destroy(wrapper);
            return false;
        }

        wrapper.transform.position += new Vector3(
            groundPosition.x - scaledBounds.center.x,
            groundPosition.y - scaledBounds.min.y,
            groundPosition.z - scaledBounds.center.z);
        TryGetRendererBounds(renderers, out Bounds finalBounds);

        BoxCollider blocker = wrapper.AddComponent<BoxCollider>();
        blocker.center = wrapper.transform.InverseTransformPoint(finalBounds.center);
        blocker.size = new Vector3(
            Mathf.Max(0.8f, finalBounds.size.x * 0.82f),
            Mathf.Max(1f, finalBounds.size.y),
            Mathf.Max(0.8f, finalBounds.size.z * 0.82f));
        Physics.SyncTransforms();
        navigation.SetBlocked(blocker.bounds, 0.18f);

        if (stoneResource)
        {
            ResourceNode node = wrapper.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Stone, 1600, 8);
            node.ConfigureRenewal(16, 12f);
        }

        return true;
    }

    private void CreateWildlife(GridNavigationService navigation)
    {
        GameObject root = new("Wildlife");
        root.transform.SetParent(transform);

        CreateWildAnimal(root.transform, "Deer", "Олень", new Vector3(-118f, 0f, 62f), 2.1f, 14f, 1101, 16, 3.1f, navigation);
        CreateWildAnimal(root.transform, "Deer", "Олень", new Vector3(-132f, 0f, -62f), 1.95f, 13f, 1109, 16, 3.0f, navigation);
        CreateWildAnimal(root.transform, "Deer", "Олень", new Vector3(112f, 0f, 58f), 2.05f, 15f, 1117, 16, 3.2f, navigation);
        CreateWildAnimal(root.transform, "Deer", "Олень", new Vector3(120f, 0f, -64f), 1.9f, 14f, 1123, 16, 3.0f, navigation);
        CreateWildAnimal(root.transform, "Stag", "Лось-рогач", new Vector3(28f, 0f, 78f), 2.45f, 16f, 1201, 22, 2.7f, navigation);
        CreateWildAnimal(root.transform, "Stag", "Лось-рогач", new Vector3(-96f, 0f, -74f), 2.35f, 14f, 1213, 22, 2.6f, navigation);
        CreateWildAnimal(root.transform, "Fox", "Лисица", new Vector3(88f, 0f, 78f), 1.05f, 18f, 1301, 9, 3.7f, navigation);
        CreateWildAnimal(root.transform, "Fox", "Лисица", new Vector3(-84f, 0f, 72f), 1.0f, 17f, 1319, 9, 3.8f, navigation);
        CreateWildAnimal(root.transform, "Wolf", "Серый волк", new Vector3(132f, 0f, -28f), 1.25f, 20f, 1409, 13, 3.5f, navigation);
    }

    private bool CreateWildAnimal(
        Transform parent,
        string modelName,
        string speciesName,
        Vector3 groundPosition,
        float targetHeight,
        float wanderRadius,
        int seed,
        int foodAmount,
        float movementSpeed,
        GridNavigationService navigation)
    {
        GameObject prefab = Resources.Load<GameObject>("Models/Quaternius/AnimatedAnimals/" + modelName);
        if (prefab == null)
        {
            return false;
        }

        GameObject wrapper = new(speciesName);
        wrapper.transform.SetParent(parent);
        GameObject model = Instantiate(prefab, wrapper.transform);
        model.name = modelName + " Art";
        Vector3 importedScale = model.transform.localScale;
        Quaternion importedRotation = model.transform.localRotation;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = importedRotation;
        model.transform.localScale = importedScale;
        DisableColliders(model);

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (!TryGetRendererBounds(renderers, out Bounds initialBounds))
        {
            Destroy(wrapper);
            return false;
        }

        float scale = initialBounds.size.y > 0.001f ? targetHeight / initialBounds.size.y : 1f;
        model.transform.localScale = importedScale * scale;
        if (!TryGetRendererBounds(renderers, out Bounds scaledBounds))
        {
            Destroy(wrapper);
            return false;
        }

        wrapper.transform.position += new Vector3(
            groundPosition.x - scaledBounds.center.x,
            groundPosition.y - scaledBounds.min.y,
            groundPosition.z - scaledBounds.center.z);
        TryGetRendererBounds(renderers, out Bounds finalBounds);

        CapsuleCollider collider = wrapper.AddComponent<CapsuleCollider>();
        collider.center = wrapper.transform.InverseTransformPoint(finalBounds.center);
        collider.height = Mathf.Max(0.7f, finalBounds.size.y * 0.85f);
        collider.radius = Mathf.Max(0.22f, Mathf.Min(finalBounds.extents.x, finalBounds.extents.z) * 0.5f);

        NavigationAgent agent = wrapper.AddComponent<NavigationAgent>();
        agent.Initialize(navigation);
        agent.Speed = movementSpeed;

        ResourceNode resource = wrapper.AddComponent<ResourceNode>();
        resource.Configure(ResourceType.Food, foodAmount, 3);
        resource.ConfigureRenewal(foodAmount, 90f + seed % 31);
        resource.SetVisualDepletion(false);

        WildAnimal animal = wrapper.AddComponent<WildAnimal>();
        animal.Initialize(speciesName, agent, resource, groundPosition, wanderRadius, seed);
        return true;
    }

    private CampCore CreateCampCore()
    {
        GameObject core = new("Town Hall Core");
        core.transform.SetParent(transform);
        core.transform.position = ProjectToGround(TownHallPosition);
        return core.AddComponent<CampCore>();
    }

    private void CreateResourceNode(
        Vector3 position,
        Material barkMaterial,
        Material cutMaterial,
        GridNavigationService navigation)
    {
        GameObject resource = new("Wood Cache");
        resource.transform.SetParent(transform);
        resource.transform.position = ProjectToGround(position);
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

        ResourceNode node = resource.AddComponent<ResourceNode>();
        node.Configure(ResourceType.Timber, 80, 5);
        Physics.SyncTransforms();
        navigation.SetBlocked(collider.bounds, 0.18f);
    }

    private void CreateResourceDeposit(
        string displayName,
        ResourceType resourceType,
        Vector3 position,
        Vector3 scale,
        Material material,
        int amount,
        int harvestPerTick,
        GridNavigationService navigation,
        bool blocksNavigation = true)
    {
        GameObject resource = new(displayName);
        resource.transform.SetParent(transform);
        resource.transform.position = ProjectToGround(position);

        PrimitiveType primitive = resourceType == ResourceType.Clay ? PrimitiveType.Cylinder : PrimitiveType.Sphere;
        CreateVisualPrimitive(
            resource.transform,
            ResourceNames.GetShort(resourceType),
            primitive,
            new Vector3(0f, scale.y * 0.35f, 0f),
            scale,
            Quaternion.Euler(0f, Noise01(amount) * 180f, resourceType == ResourceType.Stone ? 12f : 0f),
            new Material(material) { hideFlags = HideFlags.DontSave },
            false,
            true);

        BoxCollider collider = resource.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, scale.y * 0.35f, 0f);
        collider.size = new Vector3(scale.x, Mathf.Max(0.8f, scale.y), scale.z);
        ResourceNode node = resource.AddComponent<ResourceNode>();
        node.Configure(resourceType, amount, harvestPerTick);
        if (resourceType == ResourceType.Stone)
        {
            node.ConfigureRenewal(Mathf.Max(8, harvestPerTick * 2), 14f);
        }
        Physics.SyncTransforms();

        if (blocksNavigation)
        {
            navigation.SetBlocked(collider.bounds, 0.18f);
        }
    }

    private GameObject CreateSquad(
        Material heroMaterial,
        Material skinMaterial,
        Material hairMaterial,
        Material accentMaterial,
        Material selectionMaterial,
        GridNavigationService navigation,
        ResourceStockpile stockpile,
        RoadNetwork roadNetwork)
    {
        GameObject hero = new("Main Hero");
        hero.transform.SetParent(transform);
        hero.transform.position = ProjectToGround(
            TownHallPosition + new Vector3(4f, 0f, -2f));

        CapsuleCollider bodyCollider = hero.AddComponent<CapsuleCollider>();
        bodyCollider.center = new Vector3(0f, 0.84f, 0f);
        bodyCollider.radius = 0.27f;
        bodyCollider.height = 1.68f;

        StylizedCharacterBuilder.BuildHuman(
            hero.transform,
            new Material(heroMaterial),
            skinMaterial,
            hairMaterial,
            accentMaterial,
            true,
            0);

        SelectableUnit selectable = hero.AddComponent<SelectableUnit>();
        selectable.Initialize(selectionMaterial);

        NavigationAgent agent = hero.AddComponent<NavigationAgent>();
        agent.Initialize(navigation);
        agent.Speed = 4.3f;

        ResourceCollector collector = hero.AddComponent<ResourceCollector>();
        collector.Initialize(stockpile);
        hero.AddComponent<UnitCombat>();

        CreateResident("Mara - Builder", TownHallPosition + new Vector3(-2f, 0f, 3f), TownHallPosition + new Vector3(-1f, 0f, 2f), 6f, 101, new Color(0.24f, 0.38f, 0.47f), skinMaterial, hairMaterial, accentMaterial, navigation, roadNetwork);
        CreateResident("Yarik - Scavenger", TownHallPosition + new Vector3(3f, 0f, 1f), TownHallPosition + new Vector3(2f, 0f, 2.5f), 6.5f, 211, new Color(0.34f, 0.43f, 0.25f), skinMaterial, hairMaterial, accentMaterial, navigation, roadNetwork);
        CreateResident("Toma - Lookout", TownHallPosition + new Vector3(-1f, 0f, -5f), TownHallPosition + new Vector3(1f, 0f, -3f), 5.5f, 307, new Color(0.45f, 0.28f, 0.25f), skinMaterial, hairMaterial, accentMaterial, navigation, roadNetwork);
        return hero;
    }

    private void CreateResident(
        string name,
        Vector3 position,
        Vector3 home,
        float wanderRadius,
        int seed,
        Color clothColor,
        Material skinMaterial,
        Material hairMaterial,
        Material accentMaterial,
        GridNavigationService navigation,
        RoadNetwork roadNetwork)
    {
        GameObject resident = new(name);
        resident.transform.SetParent(transform);
        resident.transform.position = ProjectToGround(position);
        home = ProjectToGround(home);

        CapsuleCollider collider = resident.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.77f, 0f);
        collider.radius = 0.24f;
        collider.height = 1.54f;

        Material clothing = CreateMaterial(clothColor);
        StylizedCharacterBuilder.BuildHuman(
            resident.transform,
            clothing,
            skinMaterial,
            hairMaterial,
            accentMaterial,
            false,
            seed);

        NavigationAgent agent = resident.AddComponent<NavigationAgent>();
        agent.Initialize(navigation);
        agent.Speed = 2.15f + (seed % 3) * 0.12f;
        agent.ConfigureRoadMovement(roadNetwork, 1.5f);

        TownResident behaviour = resident.AddComponent<TownResident>();
        behaviour.Initialize(agent, home, wanderRadius, seed);
        resident.AddComponent<NpcDialogue>().ConfigureDefault(name, seed);
    }

    private static Vector3 ProjectToGround(Vector3 position, float verticalOffset = 0f)
    {
        return GroundSurface.TryProjectPoint(position, out Vector3 projected, verticalOffset)
            ? projected
            : position;
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
