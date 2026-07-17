using System.Collections.Generic;
using Hollowwest.Economy;
using Hollowwest.Prototype;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class ExpeditionSceneController : MonoBehaviour
{
    private const string NaturePath = "Models/Quaternius/UltimateStylizedNature/";
    private const string RuinsPath = "Models/Quaternius/UltimateModularRuins/";

    private readonly List<IExpeditionInteractable> _interactables = new();
    private readonly List<ExpeditionEnemy> _enemies = new();
    private readonly Vector3 _mainIslandCenter = Vector3.zero;
    private readonly Vector2 _mainIslandRadii = new(55f, 38f);
    private readonly Vector3 _anomalyCenter = new(0f, 0f, 220f);
    private readonly Vector2 _anomalyRadii = new(29f, 21f);

    private ExpeditionTravelService _travelService;
    private ExpeditionHeroController _hero;
    private ExpeditionCameraController _cameraController;
    private ExpeditionBackpack _backpack;
    private ExpeditionHud _hud;
    private Material _birchBarkMaterial;
    private Material _birchLeavesMaterial;
    private Material _bushMaterial;
    private Material _ruinStoneMaterial;
    private Material _ruinWoodMaterial;
    private Transform _dungeonFloorRoot;
    private System.Random _lootRandom;
    private Vector3 _mainReturnPosition;
    private float _messageTime;
    private string _message = string.Empty;
    private bool _returning;

    public bool IsInAnomaly { get; private set; }
    public bool AnomalyCompleted { get; private set; }
    public int EnemiesRemaining { get; private set; }
    public int DungeonFloor { get; private set; }
    public int DungeonFloorCount { get; private set; }
    public bool IsFinalDungeonFloor => DungeonFloor > 0 && DungeonFloor == DungeonFloorCount;
    public string Message => _messageTime > 0f ? _message : string.Empty;
    public string CurrentPrompt { get; private set; } = string.Empty;
    public ExpeditionBackpack Backpack => _backpack;
    public ExpeditionHeroController Hero => _hero;

    public void Initialize(ExpeditionTravelService travelService, int seed)
    {
        _travelService = travelService;
        _backpack = new ExpeditionBackpack(8, 5, 10);
        _lootRandom = new System.Random(seed ^ 0x5f3759df);
        DungeonFloorCount = new DungeonRunPlan(seed).FloorCount;
        BuildWorld(seed);
    }

    public void RegisterInteractable(IExpeditionInteractable interactable)
    {
        if (interactable != null && !_interactables.Contains(interactable))
        {
            _interactables.Add(interactable);
        }
    }

    public void UnregisterInteractable(IExpeditionInteractable interactable)
    {
        if (interactable != null)
        {
            _interactables.Remove(interactable);
        }
    }

    public void TryInteract(ExpeditionHeroController hero)
    {
        IExpeditionInteractable nearest = FindNearestInteractable(hero, 3.35f, true);
        if (nearest == null)
        {
            ShowMessage("Рядом нет ничего, с чем можно взаимодействовать", 1.3f);
            return;
        }

        nearest.Interact(hero);
    }

    public void EnterAnomaly(ExpeditionHeroController hero)
    {
        if (IsInAnomaly || hero == null)
        {
            return;
        }

        if (DungeonFloor == 0)
        {
            BuildDungeonFloor(1);
        }

        IsInAnomaly = true;
        hero.SetMovementBounds(_anomalyCenter, _anomalyRadii - Vector2.one * 2f);
        hero.Teleport(_anomalyCenter + new Vector3(0f, 0.05f, -14f));
        _cameraController.Snap();
        ShowMessage($"Подземелье: этаж {DungeonFloor}/{DungeonFloorCount}. Очистите зал и найдите спуск.", 4f);
    }

    public void LeaveAnomaly(ExpeditionHeroController hero)
    {
        if (!IsInAnomaly || hero == null)
        {
            return;
        }

        IsInAnomaly = false;
        hero.SetMovementBounds(_mainIslandCenter, _mainIslandRadii - Vector2.one * 2.5f);
        hero.Teleport(_mainReturnPosition);
        _cameraController.Snap();
        ShowMessage(AnomalyCompleted ? "Сердце аномалии очищено." : "Вы покинули аномалию без главной награды.", 3f);
    }

    public void RequestReturnHome(bool defeated)
    {
        if (_returning)
        {
            return;
        }

        _returning = true;
        _travelService.ReturnHome(_backpack, AnomalyCompleted, defeated);
    }

    public void HandleHeroDefeated()
    {
        if (_returning)
        {
            return;
        }

        _backpack.LoseHalf();
        ShowMessage("Герой ранен. Половина незакреплённой добычи потеряна.", 2f);
        RequestReturnHome(true);
    }

    public void NotifyEnemyDefeated(
        ExpeditionEnemy enemy,
        bool countsForDungeon,
        ResourceType lootType,
        int lootAmount,
        Vector3 position)
    {
        SpawnLoot(lootType, lootAmount, position + Vector3.up * 0.3f);
        if (countsForDungeon && enemy != null)
        {
            _enemies.Remove(enemy);
        }

        EnemiesRemaining = Mathf.Max(0, _enemies.Count);
        if (countsForDungeon && EnemiesRemaining == 0)
        {
            ShowMessage(IsFinalDungeonFloor
                ? "Этаж очищен. Откройте последний тайник."
                : "Этаж очищен. Спуск дальше открыт.", 3.5f);
        }
    }

    public void OpenChest(Vector3 position, int floor)
    {
        int iron = 2 + Mathf.Max(1, floor);
        SpawnLoot(ResourceType.OldIron, iron, position);
        SpawnLoot(floor % 2 == 0 ? ResourceType.Herb : ResourceType.Food, 2 + floor / 2, position);
        if (floor == DungeonFloorCount || _lootRandom.NextDouble() > 0.58)
        {
            SpawnLoot(ResourceType.Relic, 1, position);
        }

        ShowMessage("Сундук открыт — добыча выпала на землю", 2.2f);
    }

    public void ResolveDungeonGate()
    {
        if (EnemiesRemaining > 0)
        {
            return;
        }

        if (IsFinalDungeonFloor)
        {
            SpawnLoot(ResourceType.Relic, 2, _anomalyCenter + new Vector3(0f, 0.8f, 11.5f));
            SpawnLoot(ResourceType.SkyGlass, 1, _anomalyCenter + new Vector3(0f, 0.8f, 11.5f));
            MarkAnomalyCompleted();
            ShowMessage("Последний этаж очищен. Заберите добычу и возвращайтесь.", 4f);
            return;
        }

        BuildDungeonFloor(DungeonFloor + 1);
        _hero.Teleport(_anomalyCenter + new Vector3(0f, 0.05f, -13f));
        _cameraController.Snap();
    }

    public void MarkAnomalyCompleted()
    {
        AnomalyCompleted = true;
    }

    public void ShowMessage(string message, float duration)
    {
        _message = message ?? string.Empty;
        _messageTime = Mathf.Max(0.1f, duration);
    }

    public void ShowAttackPulse(Vector3 center, float radius, bool heavy)
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.name = heavy ? "Heavy Attack Arc" : "Attack Arc";
        pulse.transform.position = center;
        pulse.transform.localScale = new Vector3(radius * 1.5f, 0.06f, radius * 1.5f);
        DisableCollider(pulse);
        Material material = CreateMaterial(
            heavy ? new Color(1f, 0.50f, 0.16f, 0.42f) : new Color(1f, 0.82f, 0.35f, 0.34f),
            true);
        pulse.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(pulse, 0.14f);
        Destroy(material, 0.16f);
    }

    private void Update()
    {
        _messageTime = Mathf.Max(0f, _messageTime - Time.deltaTime);
        IExpeditionInteractable nearest = FindNearestInteractable(_hero, 3.35f, true);
        CurrentPrompt = nearest?.Prompt ?? string.Empty;
    }

    private IExpeditionInteractable FindNearestInteractable(
        ExpeditionHeroController hero,
        float maximumDistance,
        bool requireAvailable)
    {
        if (hero == null)
        {
            return null;
        }

        IExpeditionInteractable nearest = null;
        float bestDistance = maximumDistance * maximumDistance;
        for (int index = _interactables.Count - 1; index >= 0; index--)
        {
            IExpeditionInteractable candidate = _interactables[index];
            if (candidate == null)
            {
                _interactables.RemoveAt(index);
                continue;
            }

            if (requireAvailable && !candidate.CanInteract(hero))
            {
                continue;
            }

            float distance = (candidate.InteractionPosition - hero.transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                nearest = candidate;
                bestDistance = distance;
            }
        }

        return nearest;
    }

    private void BuildWorld(int seed)
    {
        Application.targetFrameRate = 60;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 80f;
        RenderSettings.fogEndDistance = 260f;
        RenderSettings.fogColor = new Color(0.48f, 0.63f, 0.72f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.44f, 0.57f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.30f, 0.34f, 0.28f);
        RenderSettings.ambientGroundColor = new Color(0.14f, 0.16f, 0.13f);

        Material grass = CreateMaterial(new Color(0.24f, 0.36f, 0.18f));
        Material cliff = CreateMaterial(new Color(0.18f, 0.17f, 0.15f));
        Material anomalyGround = CreateMaterial(new Color(0.20f, 0.16f, 0.27f));
        Material anomalyEdge = CreateMaterial(new Color(0.10f, 0.08f, 0.14f));
        _birchBarkMaterial = CreateMaterial(new Color(0.64f, 0.60f, 0.50f));
        _birchLeavesMaterial = CreateMaterial(new Color(0.25f, 0.43f, 0.20f));
        _bushMaterial = CreateMaterial(new Color(0.20f, 0.39f, 0.17f));
        _ruinStoneMaterial = CreateMaterial(new Color(0.36f, 0.35f, 0.31f));
        _ruinWoodMaterial = CreateMaterial(new Color(0.31f, 0.19f, 0.10f));
        CreateIsland("Windward Expedition Island", _mainIslandCenter, _mainIslandRadii, seed, grass, cliff);
        CreateIsland("Anomaly Pocket", _anomalyCenter, _anomalyRadii, seed + 91, anomalyGround, anomalyEdge);

        CreateLighting();
        Camera camera = CreateCamera();
        _hero = CreateHero(camera);
        _cameraController = camera.gameObject.AddComponent<ExpeditionCameraController>();
        _cameraController.Initialize(_hero.transform);

        System.Random random = new(seed);
        CreateExplorationTrail();
        CreateBiomeProps(random);
        CreateGatherables(random);
        CreateSurfaceThreats();
        CreateMainPortals();
        CreateAnomalyArena();

        _hud = gameObject.AddComponent<ExpeditionHud>();
        _hud.Initialize(this);
        ShowMessage($"Внешний остров: разведайте ориентиры и найдите вход в подземелье на {DungeonFloorCount} этажей.", 5f);
    }

    private Camera CreateCamera()
    {
        GameObject cameraObject = new("Expedition Camera");
        cameraObject.transform.SetParent(transform);
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 49f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 650f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.allowHDR = true;
        return camera;
    }

    private ExpeditionHeroController CreateHero(Camera camera)
    {
        Material cloth = CreateMaterial(new Color(0.48f, 0.12f, 0.10f));
        Material skin = CreateMaterial(new Color(0.73f, 0.49f, 0.31f));
        Material hair = CreateMaterial(new Color(0.14f, 0.08f, 0.05f));
        Material accent = CreateMaterial(new Color(0.84f, 0.62f, 0.20f));

        GameObject hero = new("Expedition Hero");
        hero.transform.SetParent(transform);
        hero.transform.position = new Vector3(0f, 0.05f, -20f);
        StylizedCharacterBuilder.BuildHuman(hero.transform, cloth, skin, hair, accent, true, 0);

        Transform weaponPivot = new GameObject("Sword Pivot").transform;
        weaponPivot.SetParent(hero.transform, false);
        weaponPivot.localPosition = new Vector3(0.48f, 1.02f, 0.18f);
        weaponPivot.localRotation = Quaternion.Euler(12f, 0f, -18f);

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Sword Handle";
        handle.transform.SetParent(weaponPivot, false);
        handle.transform.localPosition = new Vector3(0f, 0.18f, 0f);
        handle.transform.localScale = new Vector3(0.09f, 0.22f, 0.09f);
        handle.GetComponent<Renderer>().sharedMaterial = hair;
        DisableCollider(handle);

        GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "Sword Guard";
        guard.transform.SetParent(weaponPivot, false);
        guard.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        guard.transform.localScale = new Vector3(0.48f, 0.08f, 0.11f);
        guard.GetComponent<Renderer>().sharedMaterial = accent;
        DisableCollider(guard);

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "Starter Sword Blade";
        blade.transform.SetParent(weaponPivot, false);
        blade.transform.localPosition = new Vector3(0f, 0.93f, 0f);
        blade.transform.localScale = new Vector3(0.13f, 1.0f, 0.07f);
        blade.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.62f, 0.70f, 0.74f));
        DisableCollider(blade);

        CharacterController character = hero.AddComponent<CharacterController>();
        character.center = new Vector3(0f, 0.95f, 0f);
        character.height = 1.9f;
        character.radius = 0.42f;
        character.stepOffset = 0.25f;

        ExpeditionHeroController controller = hero.AddComponent<ExpeditionHeroController>();
        controller.Initialize(camera, this, _backpack, _mainIslandCenter, _mainIslandRadii - Vector2.one * 2.5f, weaponPivot);
        return controller;
    }

    private void CreateLighting()
    {
        GameObject sunObject = new("Expedition Sun");
        sunObject.transform.SetParent(transform);
        sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.83f, 0.62f);
        sun.intensity = 1.05f;
        sun.shadows = LightShadows.Soft;

        GameObject fillObject = new("Sky Fill");
        fillObject.transform.SetParent(transform);
        fillObject.transform.rotation = Quaternion.Euler(62f, 148f, 0f);
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.42f, 0.58f, 0.78f);
        fill.intensity = 0.34f;
        fill.shadows = LightShadows.None;
    }

    private void CreateBiomeProps(System.Random random)
    {
        Transform natureRoot = new GameObject("Seeded Biome Props").transform;
        natureRoot.SetParent(transform);
        Vector3[] groveCenters =
        {
            new(-32f, 0f, 15f),
            new(7f, 0f, 25f),
            new(38f, 0f, -10f)
        };
        int[] treeCounts = { 16, 13, 12 };
        for (int grove = 0; grove < groveCenters.Length; grove++)
        {
            for (int index = 0; index < treeCounts[grove]; index++)
            {
                Vector3 position = RandomClusterPoint(random, groveCenters[grove], new Vector2(10f, 8f));
                string model = index % 3 == 0 ? "BirchTree_3" : "BirchTree_1";
                CreateImportedProp(natureRoot, NaturePath + model, position, NextFloat(random, 0f, 360f), NextFloat(random, 4.8f, 7.2f));
            }
        }

        for (int grove = 0; grove < groveCenters.Length; grove++)
        {
            for (int index = 0; index < 8; index++)
            {
                Vector3 position = RandomClusterPoint(random, groveCenters[grove], new Vector2(13f, 10f));
                string model = index % 2 == 0 ? "Bush" : "Bush_Large";
                CreateImportedProp(natureRoot, NaturePath + model, position, NextFloat(random, 0f, 360f), NextFloat(random, 0.9f, 1.8f));
            }
        }

        (string Model, Vector3 Position, float Yaw, float Height)[] ruinSite =
        {
            ("Wall_ArchRound_Broken", new Vector3(-22f, 0f, -2f), 24f, 3.2f),
            ("Wall_Broken", new Vector3(-27f, 0f, 1f), 90f, 2.8f),
            ("Wall_Broken", new Vector3(-18f, 0f, 3f), 0f, 2.6f),
            ("Bricks", new Vector3(-24f, 0f, 5f), 18f, 1.1f),
            ("Crate", new Vector3(-19f, 0f, -1f), 40f, 0.9f),
            ("DeadTree_1", new Vector3(-30f, 0f, 6f), 0f, 3.8f)
        };
        foreach ((string model, Vector3 position, float yaw, float height) in ruinSite)
        {
            CreateImportedProp(natureRoot, RuinsPath + model, position, yaw, height);
        }
    }

    private void CreateGatherables(System.Random random)
    {
        // A small cache beside the landing point teaches interaction immediately,
        // before the player commits to exploring the island.
        CreateGatherable("Оставленные припасы", new Vector3(2.1f, 0f, -19.4f), ResourceType.Food, 3, new Color(0.62f, 0.32f, 0.18f), PrimitiveType.Cube);

        Vector3[] timberPositions =
        {
            new(-38f, 0f, 9f), new(-27f, 0f, 21f), new(3f, 0f, 19f), new(15f, 0f, 29f)
        };
        foreach (Vector3 position in timberPositions)
            CreateGatherable("Ветровальное дерево", position, ResourceType.Timber, 5, new Color(0.42f, 0.24f, 0.10f), PrimitiveType.Cylinder);

        Vector3[] stonePositions =
        {
            new(-16f, 0f, -5f), new(-28f, 0f, -5f), new(31f, 0f, 16f)
        };
        foreach (Vector3 position in stonePositions)
            CreateGatherable("Небесный камень", position, ResourceType.Stone, 4, new Color(0.38f, 0.43f, 0.46f), PrimitiveType.Sphere);

        Vector3[] herbPositions =
        {
            new(8f, 0f, -6f), new(13f, 0f, -2f), new(18f, 0f, 2f)
        };
        foreach (Vector3 position in herbPositions)
            CreateGatherable("Островные травы", position, ResourceType.Herb, 3, new Color(0.34f, 0.66f, 0.24f), PrimitiveType.Capsule);

        CreateGatherable("Обломки старого механизма", new Vector3(-23f, 0f, 11f), ResourceType.OldIron, 6, new Color(0.36f, 0.30f, 0.27f), PrimitiveType.Cube);
        CreateGatherable("Ягоды ветровика", new Vector3(15f, 0f, -5f), ResourceType.Food, 5, new Color(0.54f, 0.18f, 0.22f), PrimitiveType.Sphere);
    }

    private void CreateExplorationTrail()
    {
        Material trail = CreateMaterial(new Color(0.30f, 0.25f, 0.18f));
        Vector3[] points =
        {
            new(0f, 0f, -22f),
            new(-10f, 0f, -12f),
            new(-22f, 0f, -2f),
            new(1f, 0f, 2f),
            new(24f, 0f, 10f)
        };
        for (int index = 0; index < points.Length - 1; index++)
        {
            Vector3 start = points[index];
            Vector3 end = points[index + 1];
            Vector3 direction = end - start;
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = "Readable Expedition Trail";
            segment.transform.SetParent(transform);
            segment.transform.position = (start + end) * 0.5f + Vector3.up * 0.025f;
            segment.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            segment.transform.localScale = new Vector3(2.2f, 0.05f, direction.magnitude + 0.8f);
            segment.GetComponent<Renderer>().sharedMaterial = trail;
            DisableCollider(segment);
        }
    }

    private void CreateGatherable(
        string displayName,
        Vector3 position,
        ResourceType resourceType,
        int amount,
        Color color,
        PrimitiveType primitive)
    {
        GameObject root = new(displayName);
        root.transform.SetParent(transform);
        root.transform.position = position;
        Material material = CreateMaterial(color);
        for (int index = 0; index < 3; index++)
        {
            GameObject piece = GameObject.CreatePrimitive(primitive);
            piece.transform.SetParent(root.transform, false);
            float angle = index * Mathf.PI * 2f / 3f;
            piece.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.48f, 0.45f + index * 0.12f, Mathf.Sin(angle) * 0.48f);
            piece.transform.localScale = primitive == PrimitiveType.Capsule
                ? new Vector3(0.22f, 0.58f, 0.22f)
                : new Vector3(0.65f, 0.62f, 0.65f);
            piece.GetComponent<Renderer>().sharedMaterial = material;
            DisableCollider(piece);
        }

        ExpeditionGatherable gatherable = root.AddComponent<ExpeditionGatherable>();
        gatherable.Configure(this, resourceType, amount, displayName);
    }

    private void CreateMainPortals()
    {
        GameObject returnPortal = CreatePortalVisual("Небесный челн", new Vector3(0f, 0f, -25f), new Color(0.30f, 0.74f, 1f));
        returnPortal.AddComponent<ExpeditionPortal>().Configure(this, ExpeditionPortalKind.ReturnHome);

        Vector3 anomalyPosition = new(24f, 0f, 10f);
        GameObject anomalyPortal = CreatePortalVisual("Нестабильная аномалия", anomalyPosition, new Color(0.72f, 0.24f, 1f));
        anomalyPortal.AddComponent<ExpeditionPortal>().Configure(this, ExpeditionPortalKind.EnterAnomaly);
        _mainReturnPosition = anomalyPosition + new Vector3(-4.2f, 0.05f, -2f);
    }

    private void CreateAnomalyArena()
    {
        DungeonFloor = 0;
        EnemiesRemaining = 0;
    }

    private void BuildDungeonFloor(int floor)
    {
        if (_dungeonFloorRoot != null)
        {
            _dungeonFloorRoot.gameObject.SetActive(false);
            Destroy(_dungeonFloorRoot.gameObject);
        }

        _enemies.Clear();
        DungeonFloor = Mathf.Clamp(floor, 1, DungeonFloorCount);
        _dungeonFloorRoot = new GameObject($"Dungeon Floor {DungeonFloor}").transform;
        _dungeonFloorRoot.SetParent(transform);

        GameObject exit = CreatePortalVisual("Разлом обратно", _anomalyCenter + new Vector3(0f, 0f, -16f), new Color(0.32f, 0.62f, 1f));
        exit.transform.SetParent(_dungeonFloorRoot, true);
        exit.AddComponent<ExpeditionPortal>().Configure(this, ExpeditionPortalKind.LeaveAnomaly);

        GameObject gate = CreatePortalVisual(
            IsFinalDungeonFloor ? "Сердце подземелья" : "Спуск глубже",
            _anomalyCenter + new Vector3(0f, 0f, 11.5f),
            IsFinalDungeonFloor ? new Color(1f, 0.30f, 0.76f) : new Color(0.58f, 0.28f, 1f));
        gate.transform.SetParent(_dungeonFloorRoot, true);
        gate.transform.localScale *= 0.72f;
        gate.AddComponent<ExpeditionRewardShrine>().Configure(this);

        CreateDungeonLandmarks(DungeonFloor);
        CreateChest(_anomalyCenter + new Vector3(DungeonFloor % 2 == 0 ? -9f : 9f, 0f, 5f), DungeonFloor);

        Vector3[] positions =
        {
            _anomalyCenter + new Vector3(-8f, 0f, -5f),
            _anomalyCenter + new Vector3(8f, 0f, -2f),
            _anomalyCenter + new Vector3(-6f, 0f, 6f),
            _anomalyCenter + new Vector3(5f, 0f, 8f),
            _anomalyCenter + new Vector3(0f, 0f, 2f),
            _anomalyCenter + new Vector3(9f, 0f, 7f)
        };
        int enemyCount = Mathf.Min(positions.Length, 3 + DungeonFloor);
        for (int index = 0; index < enemyCount; index++)
        {
            _enemies.Add(CreateEnemy(
                positions[index],
                false,
                true,
                ResourceType.OldIron,
                1 + DungeonFloor / 2,
                false));
        }

        EnemiesRemaining = _enemies.Count;
        ShowMessage($"Этаж {DungeonFloor}/{DungeonFloorCount}: сначала очистите центральный зал.", 3f);
    }

    private void CreateDungeonLandmarks(int floor)
    {
        Vector3 center = _anomalyCenter;
        CreateImportedProp(_dungeonFloorRoot, RuinsPath + "Wall_ArchRound_Broken", center + new Vector3(0f, 0f, -7f), 0f, 3.4f);
        CreateImportedProp(_dungeonFloorRoot, RuinsPath + "Wall_Broken", center + new Vector3(-10f, 0f, 0f), 90f, 3.0f);
        CreateImportedProp(_dungeonFloorRoot, RuinsPath + "Wall_Broken", center + new Vector3(10f, 0f, 1f), 90f, 3.0f);
        CreateImportedProp(_dungeonFloorRoot, RuinsPath + "Bricks", center + new Vector3(floor % 2 == 0 ? 7f : -7f, 0f, 7f), floor * 31f, 1.2f);
    }

    private ExpeditionEnemy CreateEnemy(
        Vector3 position,
        bool activeOutsideAnomaly,
        bool countsForDungeon,
        ResourceType lootType,
        int lootAmount,
        bool animal)
    {
        Material body = CreateMaterial(new Color(0.32f, 0.06f, 0.16f));
        Material eyes = CreateMaterial(new Color(1f, 0.30f, 0.12f));
        GameObject enemy = new(animal ? "Aggressive Sky Beast" : "Anomaly Fiend");
        enemy.transform.SetParent(transform);
        enemy.transform.position = position;
        if (animal)
        {
            CreateAnimalVisual(enemy.transform, body, eyes);
        }
        else
        {
            StylizedCharacterBuilder.BuildEnemy(enemy.transform, body, eyes);
        }
        CapsuleCollider collider = enemy.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, animal ? 0.65f : 0.9f, 0f);
        collider.height = animal ? 1.2f : 1.9f;
        collider.radius = animal ? 0.7f : 0.55f;
        collider.isTrigger = true;
        ExpeditionEnemy controller = enemy.AddComponent<ExpeditionEnemy>();
        controller.Initialize(
            _hero,
            this,
            animal ? 54 : 70 + DungeonFloor * 12,
            activeOutsideAnomaly,
            countsForDungeon,
            lootType,
            lootAmount);
        return controller;
    }

    private void CreateSurfaceThreats()
    {
        CreateEnemy(new Vector3(-18f, 0f, 15f), true, false, ResourceType.Hide, 2, true);
        CreateEnemy(new Vector3(34f, 0f, -8f), true, false, ResourceType.Food, 3, true);
        CreateEnemy(new Vector3(-34f, 0f, -4f), true, false, ResourceType.Hide, 2, true);
    }

    private void CreateAnimalVisual(Transform root, Material body, Material eyes)
    {
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.transform.SetParent(root, false);
        torso.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        torso.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        torso.transform.localScale = new Vector3(0.62f, 0.82f, 0.62f);
        torso.GetComponent<Renderer>().sharedMaterial = body;
        DisableCollider(torso);
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(root, false);
        head.transform.localPosition = new Vector3(0f, 0.82f, 0.92f);
        head.transform.localScale = new Vector3(0.72f, 0.62f, 0.74f);
        head.GetComponent<Renderer>().sharedMaterial = body;
        DisableCollider(head);
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.transform.SetParent(root, false);
            eye.transform.localPosition = new Vector3(side * 0.18f, 0.94f, 1.25f);
            eye.transform.localScale = Vector3.one * 0.10f;
            eye.GetComponent<Renderer>().sharedMaterial = eyes;
            DisableCollider(eye);
        }
    }

    private void CreateChest(Vector3 position, int floor)
    {
        Material wood = CreateMaterial(new Color(0.34f, 0.18f, 0.08f));
        Material metal = CreateMaterial(new Color(0.44f, 0.38f, 0.24f));
        GameObject chest = new($"Dungeon Chest {floor}");
        chest.transform.SetParent(_dungeonFloorRoot);
        chest.transform.position = position;
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.SetParent(chest.transform, false);
        box.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        box.transform.localScale = new Vector3(1.5f, 0.72f, 1.0f);
        box.GetComponent<Renderer>().sharedMaterial = wood;
        DisableCollider(box);
        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lid.transform.SetParent(chest.transform, false);
        lid.transform.localPosition = new Vector3(0f, 0.88f, -0.42f);
        lid.transform.localScale = new Vector3(1.56f, 0.22f, 1.05f);
        lid.GetComponent<Renderer>().sharedMaterial = metal;
        DisableCollider(lid);
        chest.AddComponent<ExpeditionChest>().Configure(this, lid.transform, floor);
    }

    private void SpawnLoot(ResourceType type, int amount, Vector3 origin)
    {
        GameObject loot = GameObject.CreatePrimitive(type == ResourceType.OldIron ? PrimitiveType.Cube : PrimitiveType.Sphere);
        loot.name = $"Loot {ResourceNames.GetShort(type)}";
        loot.transform.SetParent(transform);
        loot.transform.position = origin;
        loot.transform.localScale = type == ResourceType.Relic ? Vector3.one * 0.42f : Vector3.one * 0.34f;
        loot.GetComponent<Renderer>().sharedMaterial = CreateMaterial(GetLootColor(type));
        DisableCollider(loot);
        float angle = NextFloat(_lootRandom, 0f, Mathf.PI * 2f);
        float distance = NextFloat(_lootRandom, 0.8f, 1.8f);
        Vector3 offset = new(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        loot.AddComponent<ExpeditionLootPickup>().Configure(this, type, amount, offset);
    }

    private static Color GetLootColor(ResourceType type)
    {
        return type switch
        {
            ResourceType.OldIron => new Color(0.32f, 0.38f, 0.42f),
            ResourceType.Relic => new Color(0.82f, 0.34f, 0.92f),
            ResourceType.SkyGlass => new Color(0.24f, 0.72f, 0.95f),
            ResourceType.Hide => new Color(0.48f, 0.28f, 0.14f),
            ResourceType.Food => new Color(0.65f, 0.20f, 0.16f),
            _ => new Color(0.42f, 0.66f, 0.30f)
        };
    }

    private GameObject CreatePortalVisual(string name, Vector3 position, Color color)
    {
        GameObject root = new(name);
        root.transform.SetParent(transform);
        root.transform.position = position;
        Material material = CreateMaterial(color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 1.4f);
        for (int index = 0; index < 10; index++)
        {
            float angle = index * Mathf.PI * 2f / 10f;
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.transform.SetParent(root.transform, false);
            segment.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.45f, 1.8f + Mathf.Sin(angle) * 1.45f, 0f);
            segment.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
            segment.transform.localScale = new Vector3(0.28f, 0.62f, 0.28f);
            segment.GetComponent<Renderer>().sharedMaterial = material;
            DisableCollider(segment);
        }

        GameObject lightObject = new("Portal Light");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = Vector3.up * 1.8f;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = 8f;
        light.intensity = 2.1f;
        return root;
    }

    private void CreateIsland(
        string name,
        Vector3 center,
        Vector2 radii,
        int seed,
        Material topMaterial,
        Material edgeMaterial)
    {
        const int segments = 28;
        System.Random random = new(seed);
        List<Vector3> vertices = new(1 + segments * 2) { center };
        for (int index = 0; index < segments; index++)
        {
            float angle = index * Mathf.PI * 2f / segments;
            float noise = NextFloat(random, 0.91f, 1.05f);
            vertices.Add(center + new Vector3(Mathf.Cos(angle) * radii.x * noise, 0f, Mathf.Sin(angle) * radii.y * noise));
        }

        for (int index = 0; index < segments; index++)
        {
            Vector3 top = vertices[index + 1];
            Vector3 direction = (top - center).normalized;
            vertices.Add(top + direction * -4f + Vector3.down * NextFloat(random, 10f, 16f));
        }

        List<int> topTriangles = new(segments * 3);
        List<int> edgeTriangles = new(segments * 6);
        for (int index = 0; index < segments; index++)
        {
            int next = (index + 1) % segments;
            int topCurrent = index + 1;
            int topNext = next + 1;
            int bottomCurrent = segments + index + 1;
            int bottomNext = segments + next + 1;
            topTriangles.Add(0);
            topTriangles.Add(topNext);
            topTriangles.Add(topCurrent);
            edgeTriangles.Add(topCurrent);
            edgeTriangles.Add(topNext);
            edgeTriangles.Add(bottomCurrent);
            edgeTriangles.Add(topNext);
            edgeTriangles.Add(bottomNext);
            edgeTriangles.Add(bottomCurrent);
        }

        Mesh mesh = new() { name = name + " Mesh", hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(topTriangles, 0);
        mesh.SetTriangles(edgeTriangles, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject island = new(name);
        island.transform.SetParent(transform);
        island.AddComponent<MeshFilter>().sharedMesh = mesh;
        island.AddComponent<MeshRenderer>().sharedMaterials = new[] { topMaterial, edgeMaterial };
        MeshCollider collider = island.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }

    private static Vector3 RandomPoint(System.Random random, Vector3 center, Vector2 radii, float scale)
    {
        float angle = NextFloat(random, 0f, Mathf.PI * 2f);
        float radius = Mathf.Sqrt(NextFloat(random, 0.08f, 1f)) * scale;
        return center + new Vector3(Mathf.Cos(angle) * radii.x * radius, 0f, Mathf.Sin(angle) * radii.y * radius);
    }

    private static Vector3 RandomClusterPoint(System.Random random, Vector3 center, Vector2 radii)
    {
        float angle = NextFloat(random, 0f, Mathf.PI * 2f);
        float radius = Mathf.Sqrt(NextFloat(random, 0.08f, 1f));
        return center + new Vector3(Mathf.Cos(angle) * radii.x * radius, 0f, Mathf.Sin(angle) * radii.y * radius);
    }

    private void CreateImportedProp(Transform parent, string path, Vector3 groundPosition, float yaw, float targetHeight)
    {
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            return;
        }

        GameObject wrapper = new(prefab.name);
        wrapper.transform.SetParent(parent);
        GameObject model = Instantiate(prefab, wrapper.transform);
        Vector3 importedScale = model.transform.localScale;
        Quaternion importedRotation = model.transform.localRotation;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * importedRotation;
        foreach (Collider collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (!TryGetBounds(renderers, out Bounds initialBounds))
        {
            Destroy(wrapper);
            return;
        }

        ApplyImportedPalette(path, renderers);

        model.transform.localScale = importedScale * (targetHeight / Mathf.Max(0.01f, initialBounds.size.y));
        if (!TryGetBounds(renderers, out Bounds scaledBounds))
        {
            Destroy(wrapper);
            return;
        }

        wrapper.transform.position += new Vector3(
            groundPosition.x - scaledBounds.center.x,
            groundPosition.y - scaledBounds.min.y,
            groundPosition.z - scaledBounds.center.z);
    }

    private void ApplyImportedPalette(string path, Renderer[] renderers)
    {
        bool birch = path.Contains("BirchTree");
        bool bush = path.Contains("Bush");
        bool woodenRuin = path.Contains("DeadTree") || path.Contains("Crate") || path.Contains("Barrel") || path.Contains("Cart");

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                if (birch)
                {
                    string slotName = ((materials[index] != null ? materials[index].name : string.Empty) + " " + renderer.gameObject.name).ToLowerInvariant();
                    materials[index] = slotName.Contains("leaf") || slotName.Contains("leaves")
                        ? _birchLeavesMaterial
                        : _birchBarkMaterial;
                }
                else if (bush)
                {
                    materials[index] = _bushMaterial;
                }
                else
                {
                    materials[index] = woodenRuin ? _ruinWoodMaterial : _ruinStoneMaterial;
                }
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static bool TryGetBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled) continue;
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

    private static Material CreateMaterial(Color color, bool transparent = false)
    {
        Material material = new(Shader.Find("Standard"))
        {
            color = color,
            hideFlags = HideFlags.DontSave
        };
        material.SetFloat("_Glossiness", 0.12f);
        if (transparent)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
        }

        return material;
    }

    private static float NextFloat(System.Random random, float minimum, float maximum)
    {
        return minimum + (float)random.NextDouble() * (maximum - minimum);
    }

    private static void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
    }
}

public sealed class ExpeditionHud : MonoBehaviour
{
    private ExpeditionSceneController _sceneController;
    private GUIStyle _panel;
    private GUIStyle _title;
    private GUIStyle _body;
    private GUIStyle _center;
    private Texture2D _panelTexture;
    private bool _showBackpack;

    public void Initialize(ExpeditionSceneController sceneController)
    {
        _sceneController = sceneController;
    }

    private void Update()
    {
        if (Hollowwest.Controls.GameInputRouter.Instance?.HeroBackpack.WasPressedThisFrame() == true)
        {
            _showBackpack = !_showBackpack;
        }
    }

    private void OnGUI()
    {
        if (_sceneController == null || _sceneController.Hero == null || _sceneController.Backpack == null)
        {
            return;
        }

        EnsureStyles();
        float scale = Mathf.Clamp(Screen.height / 900f, 0.76f, 1.15f);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        float height = Screen.height / scale;

        Rect header = new(18f, 18f, 440f, 82f);
        GUI.Box(header, GUIContent.none, _panel);
        string locationTitle = _sceneController.IsInAnomaly
            ? $"ПОДЗЕМЕЛЬЕ • ЭТАЖ {_sceneController.DungeonFloor}/{_sceneController.DungeonFloorCount}"
            : "ВНЕШНИЙ ОСТРОВ • ВЕТРОЛЕСЬЕ";
        GUI.Label(new Rect(header.x + 14f, header.y + 8f, 410f, 28f), locationTitle, _title);
        GUI.Label(new Rect(header.x + 14f, header.y + 42f, 80f, 20f), "Здоровье", _body);
        Rect health = new(header.x + 98f, header.y + 46f, 220f, 13f);
        GUI.Box(health, GUIContent.none);
        Color previousColor = GUI.color;
        GUI.color = new Color(0.68f, 0.18f, 0.14f);
        float healthRatio = (float)_sceneController.Hero.Health / _sceneController.Hero.MaxHealth;
        GUI.Box(new Rect(health.x, health.y, health.width * healthRatio, health.height), GUIContent.none);
        GUI.color = previousColor;
        GUI.Label(new Rect(header.x + 328f, header.y + 41f, 92f, 22f), $"{_sceneController.Hero.Health}/{_sceneController.Hero.MaxHealth}", _body);

        if (_showBackpack)
        {
            const float cellSize = 52f;
            float gridWidth = _sceneController.Backpack.Width * cellSize;
            float gridHeight = _sceneController.Backpack.Height * cellSize;
            Rect backpack = new(width - gridWidth - 46f, 112f, gridWidth + 28f, gridHeight + 58f);
            GUI.Box(backpack, GUIContent.none, _panel);
            GUI.Label(new Rect(backpack.x + 14f, backpack.y + 8f, backpack.width - 28f, 30f), $"РЮКЗАК  {_sceneController.Backpack.SlotsUsed}/{_sceneController.Backpack.SlotCapacity}", _title);
            for (int y = 0; y < _sceneController.Backpack.Height; y++)
            {
                for (int x = 0; x < _sceneController.Backpack.Width; x++)
                {
                    Rect cell = new(backpack.x + 14f + x * cellSize, backpack.y + 42f + y * cellSize, cellSize - 3f, cellSize - 3f);
                    GUI.Box(cell, GUIContent.none);
                    DrawBackpackSlot(cell, x, y);
                }
            }
        }
        else
        {
            Rect compact = new(width - 242f, 18f, 224f, 34f);
            GUI.Box(compact, GUIContent.none, _panel);
            GUI.Label(compact, $"TAB • РЮКЗАК {_sceneController.Backpack.SlotsUsed}/{_sceneController.Backpack.SlotCapacity}", _center);
        }

        if (!string.IsNullOrEmpty(_sceneController.CurrentPrompt))
        {
            Rect prompt = new(width * 0.5f - 250f, height - 126f, 500f, 38f);
            GUI.Box(prompt, GUIContent.none, _panel);
            if (GUI.Button(prompt, _sceneController.CurrentPrompt, _center))
            {
                _sceneController.TryInteract(_sceneController.Hero);
            }
        }

        if (!string.IsNullOrEmpty(_sceneController.Message))
        {
            Rect message = new(width * 0.5f - 310f, 24f, 620f, 42f);
            GUI.Box(message, GUIContent.none, _panel);
            GUI.Label(message, _sceneController.Message, _center);
        }

        GUI.Label(new Rect(18f, height - 52f, width - 36f, 30f), "WASD — движение   •   мышь — направление   •   ЛКМ/ПКМ — атаки   •   Space — рывок   •   E — действие   •   Tab — рюкзак", _center);
        if (_sceneController.IsInAnomaly)
        {
            GUI.Label(new Rect(width - 260f, height - 84f, 240f, 24f), $"Твари: {_sceneController.EnemiesRemaining}", _title);
        }

        GUI.matrix = previous;
    }

    private void DrawBackpackSlot(Rect cell, int x, int y)
    {
        foreach (ExpeditionBackpackSlot slot in _sceneController.Backpack.Slots)
        {
            if (slot.X != x || slot.Y != y)
            {
                continue;
            }

            Color previous = GUI.color;
            GUI.color = GetResourceColor(slot.Type);
            GUI.Box(new Rect(cell.x + 5f, cell.y + 5f, cell.width - 10f, cell.height - 10f), GUIContent.none);
            GUI.color = previous;
            GUI.Label(new Rect(cell.x + 4f, cell.y + 3f, cell.width - 8f, 22f), ResourceNames.GetShort(slot.Type), _center);
            GUI.Label(new Rect(cell.x + 5f, cell.yMax - 22f, cell.width - 10f, 20f), slot.Amount.ToString(), _center);
            return;
        }
    }

    private static Color GetResourceColor(ResourceType type)
    {
        return type switch
        {
            ResourceType.Timber => new Color(0.46f, 0.27f, 0.11f),
            ResourceType.Stone => new Color(0.38f, 0.43f, 0.46f),
            ResourceType.Food => new Color(0.58f, 0.22f, 0.18f),
            ResourceType.Herb => new Color(0.25f, 0.53f, 0.21f),
            ResourceType.OldIron => new Color(0.30f, 0.36f, 0.40f),
            ResourceType.Relic => new Color(0.62f, 0.28f, 0.76f),
            ResourceType.SkyGlass => new Color(0.20f, 0.66f, 0.82f),
            _ => new Color(0.50f, 0.38f, 0.20f)
        };
    }

    private void OnDestroy()
    {
        if (_panelTexture != null)
        {
            Destroy(_panelTexture);
        }
    }

    private void EnsureStyles()
    {
        if (_panel != null)
        {
            return;
        }

        _panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "Expedition HUD Panel"
        };
        _panelTexture.SetPixel(0, 0, new Color(0.025f, 0.035f, 0.03f, 0.88f));
        _panelTexture.Apply(false, true);

        _panel = new GUIStyle(GUI.skin.box);
        _panel.normal.background = _panelTexture;
        _panel.normal.textColor = Color.white;
        _title = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.83f, 0.48f) } };
        _body = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = new Color(0.90f, 0.92f, 0.86f) } };
        _center = new GUIStyle(_body) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
    }
}

}
