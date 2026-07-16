using System.Collections.Generic;
using Hollowwest.Economy;
using Hollowwest.Gameplay;
using Hollowwest.Selection;
using UnityEngine;

namespace Hollowwest.Presentation
{

public sealed class PrototypeHud : MonoBehaviour
{
    private const float BuildButtonWidth = 304f;
    private const float BuildButtonHeight = 72f;
    private const float GatherButtonWidth = 264f;
    private const float ResourceBarHeight = 76f;
    private const float StatusPanelWidth = 840f;
    private const float BuildingPanelWidth = 400f;
    private const float BuildingPanelHeight = 280f;

    private static readonly ResourceType[] AllResources =
    {
        ResourceType.Timber,
        ResourceType.Stone,
        ResourceType.Clay,
        ResourceType.Food,
        ResourceType.Herb,
        ResourceType.Hide,
        ResourceType.Plank,
        ResourceType.Brick,
        ResourceType.Tool,
        ResourceType.Leather,
        ResourceType.Grain,
        ResourceType.Provisions,
        ResourceType.Medicine,
        ResourceType.OldIron,
        ResourceType.Relic,
        ResourceType.WardStone,
        ResourceType.SkyGlass
    };

    private static readonly string[] ResourceIconPaths =
    {
        "UI/Icons/timber",
        "UI/Icons/stone",
        "UI/Icons/clay",
        "UI/Icons/food",
        "UI/Icons/herb",
        "UI/Icons/hide",
        "UI/Icons/plank",
        "UI/Icons/brick",
        "UI/Icons/tool",
        "UI/Icons/leather",
        "UI/Icons/grain",
        "UI/Icons/provisions",
        "UI/Icons/medicine",
        "UI/Icons/old_iron",
        "UI/Icons/relic",
        "UI/Icons/ward_stone",
        "UI/Icons/sky_glass"
    };

    private static readonly BuildingCategory[] CategoryOrder =
    {
        BuildingCategory.Settlement,
        BuildingCategory.Gathering,
        BuildingCategory.Craft,
        BuildingCategory.Defense,
        BuildingCategory.Sacred
    };

    private static readonly string[] CategoryLabels =
    {
        "ПОСЕЛЕНИЕ",
        "ДОБЫЧА",
        "РЕМЕСЛО",
        "ЗАЩИТА",
        "САКРАЛЬНОЕ"
    };

    private static bool BuildMenuOpen;
    private static bool ControlsOpen;
    private static bool HasSelectedBuilding;
    private static bool GatherMenuOpen;
    private static int ActiveResourceDropdownIndex = -1;

    private SelectionController _selection;
    private ResourceStockpile _stockpile;
    private GameSession _session;
    private SettlementState _settlement;
    private SettlementEconomy _economy;
    private ExpeditionSystem _expedition;
    private BuildingPlacementController _placement;
    private RoadPlacementController _roadPlacement;
    private GatheringAreaController _gathering;
    private IReadOnlyList<BuildingDefinition> _buildingCatalog;
    private BuildingCategory _activeCategory = BuildingCategory.Settlement;
    private BuildingDefinition _variantDefinition;
    private bool _showControls;
    private bool _showBuildMenu;
    private bool _showGatherMenu;
    private int _resourceDropdownIndex = -1;
    private readonly ResourceType[] _visibleResources =
    {
        ResourceType.Timber,
        ResourceType.Stone,
        ResourceType.Food,
        ResourceType.Plank,
        ResourceType.Brick,
        ResourceType.Tool,
        ResourceType.OldIron
    };
    private GUIStyle _panelStyle;
    private GUIStyle _buildDockStyle;
    private GUIStyle _buildButtonStyle;
    private GUIStyle _buildTileStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _phaseStyle;
    private GUIStyle _bodyStyle;
    private GUIStyle _hintStyle;
    private GUIStyle _centeredHintStyle;
    private GUIStyle _cardTitleStyle;
    private GUIStyle _cardDescriptionStyle;
    private GUIStyle _barBackgroundStyle;
    private GUIStyle _barFillStyle;
    private Texture2D _buildIcon;
    private Texture2D _gatherIcon;
    private Texture2D[] _resourceIcons;

    public static bool BlocksWorldInput(Vector2 screenPosition)
    {
        if (DialogueController.IsBlockingWorldInput)
        {
            return true;
        }

        Vector2 guiPosition = new(screenPosition.x, Screen.height - screenPosition.y);
        Rect resourceBar = GetResourceBarRect();
        Rect controlsPanel = GetControlsPanelRect();
        Rect buildingPanel = GetBuildingPanelRect();
        Rect buildButton = GetBuildButtonRect();
        Rect buildMenu = GetBuildMenuRect();
        Rect gatherButton = GetGatherButtonRect();
        Rect gatherMenu = GetGatherMenuRect();
        Rect dayNight = GetDayNightRect();
        Rect expedition = GetExpeditionButtonRect();
        Rect resourceDropdown = GetResourceDropdownRect();
        Rect placementStatus = GetPlacementStatusRect();
        return resourceBar.Contains(guiPosition) ||
               expedition.Contains(guiPosition) ||
               dayNight.Contains(guiPosition) ||
               (ControlsOpen && controlsPanel.Contains(guiPosition)) ||
               (HasSelectedBuilding && !BuildMenuOpen && buildingPanel.Contains(guiPosition)) ||
               buildButton.Contains(guiPosition) ||
               (BuildMenuOpen && buildMenu.Contains(guiPosition)) ||
               gatherButton.Contains(guiPosition) ||
               (GatherMenuOpen && gatherMenu.Contains(guiPosition)) ||
               resourceDropdown.Contains(guiPosition) ||
               ((BuildingPlacementController.IsAnyPlacementActive ||
                 RoadPlacementController.IsAnyPlacementActive ||
                 GatheringAreaController.IsAnyGatheringActive) &&
                placementStatus.Contains(guiPosition));
    }

    public void Initialize(
        SelectionController selection,
        ResourceStockpile stockpile,
        GameSession session,
        BuildingPlacementController placement,
        RoadPlacementController roadPlacement,
        GatheringAreaController gathering,
        IReadOnlyList<BuildingDefinition> buildingCatalog,
        SettlementState settlement,
        SettlementEconomy economy,
        ExpeditionSystem expedition)
    {
        _selection = selection;
        _stockpile = stockpile;
        _session = session;
        _placement = placement;
        _roadPlacement = roadPlacement;
        _gathering = gathering;
        _buildingCatalog = buildingCatalog;
        _settlement = settlement;
        _economy = economy;
        _expedition = expedition;
        ControlsOpen = _showControls;
        BuildMenuOpen = _showBuildMenu;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.F1))
        {
            _showControls = !_showControls;
            ControlsOpen = _showControls;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildInterface();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _showBuildMenu)
        {
            SetBuildMenuOpen(false);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _showGatherMenu = false;
            GatherMenuOpen = false;
            _resourceDropdownIndex = -1;
        }

        HasSelectedBuilding = _selection != null && _selection.SelectedBuilding != null;
    }

    private void OnGUI()
    {
        if (DialogueController.IsBlockingWorldInput)
        {
            return;
        }

        EnsureStyles();
        DrawResourceBar();
        DrawCompactExpedition();
        DrawDayNightButtons();
        if (!_showBuildMenu &&
            (_placement == null || !_placement.IsPlacing) &&
            (_roadPlacement == null || !_roadPlacement.IsPlacing))
        {
            DrawBuildingPanel();
        }

        DrawControlsPanel();
        DrawBuildMenu();
        DrawPlacementStatus();
        DrawGatheringStatus();
        DrawGatherMenu();
        DrawGatherButton();
        DrawBuildButton();
    }

    private void DrawResourceBar()
    {
        Rect bar = GetResourceBarRect();
        GUI.Box(bar, GUIContent.none, _buildDockStyle);
        const float padding = 7f;
        float slotWidth = (bar.width - padding * 2f) / _visibleResources.Length;
        for (int index = 0; index < _visibleResources.Length; index++)
        {
            ResourceType resource = _visibleResources[index];
            Rect slot = new(bar.x + padding + index * slotWidth, bar.y + 5f, slotWidth - 3f, bar.height - 10f);
            if (GUI.Button(slot, GUIContent.none, _buildTileStyle))
            {
                _resourceDropdownIndex = _resourceDropdownIndex == index ? -1 : index;
                ActiveResourceDropdownIndex = _resourceDropdownIndex;
            }

            DrawResourceIcon(resource, new Rect(slot.x + 8f, slot.y + 11f, 42f, 42f));
            GUI.Label(new Rect(slot.x + 57f, slot.y + 7f, slot.width - 72f, 27f), (_stockpile?.Get(resource) ?? 0).ToString(), _bodyStyle);
            GUI.Label(new Rect(slot.x + 57f, slot.y + 32f, slot.width - 72f, 23f), ResourceNames.GetShort(resource), _hintStyle);
            GUI.Label(new Rect(slot.xMax - 18f, slot.y + 22f, 14f, 20f), "▾", _hintStyle);
        }

        DrawResourceDropdown();
    }

    private void DrawResourceDropdown()
    {
        if (_resourceDropdownIndex < 0 || _resourceDropdownIndex >= _visibleResources.Length)
        {
            return;
        }

        Rect panel = GetResourceDropdownRect();
        GUI.Box(panel, GUIContent.none, _buildDockStyle);
        const int columns = 2;
        int rows = Mathf.CeilToInt(AllResources.Length / (float)columns);
        float itemWidth = (panel.width - 16f) / columns;
        for (int index = 0; index < AllResources.Length; index++)
        {
            int column = index / rows;
            int row = index % rows;
            ResourceType resource = AllResources[index];
            Rect item = new(panel.x + 8f + column * itemWidth, panel.y + 8f + row * 34f, itemWidth - 4f, 31f);
            if (GUI.Button(item, GUIContent.none, _buildTileStyle))
            {
                _visibleResources[_resourceDropdownIndex] = resource;
                _resourceDropdownIndex = -1;
                ActiveResourceDropdownIndex = -1;
            }

            DrawResourceIcon(resource, new Rect(item.x + 5f, item.y + 3f, 25f, 25f));
            GUI.Label(new Rect(item.x + 36f, item.y + 4f, item.width - 42f, 23f), ResourceNames.GetShort(resource), _hintStyle);
        }
    }

    private void DrawDayNightButtons()
    {
        Rect panel = GetDayNightRect();
        float half = (panel.width - 5f) * 0.5f;
        Color previous = GUI.color;
        GUI.color = _session != null && _session.Phase == GamePhase.Day ? new Color(1f, 0.83f, 0.45f) : Color.white;
        if (GUI.Button(new Rect(panel.x, panel.y, half, panel.height), "DAY") && _session != null)
        {
            _session.SetDayForDevelopment();
        }

        GUI.color = _session != null && _session.Phase == GamePhase.Night ? new Color(0.55f, 0.70f, 1f) : Color.white;
        if (GUI.Button(new Rect(panel.x + half + 5f, panel.y, half, panel.height), "NIGHT") && _session != null)
        {
            _session.SetNightForDevelopment();
        }
        GUI.color = previous;
    }

    private void DrawCompactExpedition()
    {
        if (_expedition == null)
        {
            return;
        }

        Rect button = GetExpeditionButtonRect();
        string label = _expedition.IsActive
            ? $"ПЕРЕХОД {Mathf.RoundToInt(_expedition.Progress * 100f)}%"
            : "ИССЛЕДОВАТЬ ОСТРОВ";
        bool previousEnabled = GUI.enabled;
        GUI.enabled = !_expedition.IsActive;
        if (GUI.Button(button, label))
        {
            _expedition.TryStart(out _);
        }
        GUI.enabled = previousEnabled;
    }

    private void DrawGatherButton()
    {
        Rect button = GetGatherButtonRect();
        if (GUI.Button(button, GUIContent.none, _buildButtonStyle))
        {
            if (_gathering != null && _gathering.IsSelecting)
            {
                _gathering.CancelSelection();
            }
            _showGatherMenu = !_showGatherMenu;
            GatherMenuOpen = _showGatherMenu;
            SetBuildMenuOpen(false);
        }

        GUI.DrawTexture(new Rect(button.x + 15f, button.y + 10f, 52f, 52f), _gatherIcon, ScaleMode.ScaleToFit, true);
        GUI.Label(new Rect(button.x + 78f, button.y + 12f, button.width - 90f, 25f), "СБОР", _phaseStyle);
        GUI.Label(new Rect(button.x + 78f, button.y + 38f, button.width - 90f, 22f), _showGatherMenu ? "выберите фильтр" : "выделить область", _hintStyle);
    }

    private void DrawGatherMenu()
    {
        if (!_showGatherMenu)
        {
            return;
        }

        Rect panel = GetGatherMenuRect();
        GUI.Box(panel, GUIContent.none, _buildDockStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 9f, panel.width - 28f, 25f), "ЧТО СОБИРАТЬ В ОБЛАСТИ", _phaseStyle);
        ResourceType?[] filters =
        {
            null,
            ResourceType.Timber,
            ResourceType.Stone,
            ResourceType.Clay,
            ResourceType.Herb,
            ResourceType.Food
        };
        string[] labels = { "ВСЁ", "ДЕРЕВО", "КАМЕНЬ", "ГЛИНА", "ТРАВЫ", "ЕДА" };
        float buttonWidth = (panel.width - 28f - 5f * (filters.Length - 1)) / filters.Length;
        for (int index = 0; index < filters.Length; index++)
        {
            Rect filterButton = new(panel.x + 14f + index * (buttonWidth + 5f), panel.y + 42f, buttonWidth, 38f);
            if (GUI.Button(filterButton, labels[index]))
            {
                _gathering?.BeginSelection(filters[index]);
                _showGatherMenu = false;
                GatherMenuOpen = false;
            }
        }
    }

    private void DrawGatheringStatus()
    {
        if (_gathering == null || !_gathering.IsSelecting)
        {
            return;
        }

        Rect panel = GetPlacementStatusRect();
        GUI.Box(panel, GUIContent.none, _buildDockStyle);
        string filter = _gathering.Filter.HasValue ? ResourceNames.GetShort(_gathering.Filter.Value) : "все ресурсы";
        GUI.Label(new Rect(panel.x + 14f, panel.y + 7f, panel.width - 28f, 20f), $"СБОР: {filter}", _phaseStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 27f, panel.width - 28f, 18f), "Зажмите ЛКМ и выделите область • ПКМ отмена", _centeredHintStyle);
    }

    private void DrawPhasePanel()
    {
        Rect panel = GetPhasePanelRect();
        GUI.Box(panel, GUIContent.none, _panelStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 6f, panel.width - 28f, 24f), "ZASTAVA / TOWN DEV", _titleStyle);

        string phaseText = _session == null
            ? "Preparing the village"
            : BuildPhaseText();

        _phaseStyle.normal.textColor = GetPhaseColor();
        GUI.Label(new Rect(panel.x + 14f, panel.y + 30f, panel.width - 28f, 22f), phaseText, _phaseStyle);

        if (GUI.Button(new Rect(panel.x + 14f, panel.y + 54f, 104f, 24f), "DAY") && _session != null)
        {
            _session.SetDayForDevelopment();
        }

        if (GUI.Button(new Rect(panel.x + 128f, panel.y + 54f, 138f, 24f), "NIGHT + WAVE") && _session != null)
        {
            _session.SetNightForDevelopment();
        }

        if (_settlement == null)
        {
            return;
        }

        GUI.Label(
            new Rect(panel.x + 14f, panel.y + 84f, panel.width - 28f, 18f),
            $"Уровень: {SettlementState.GetTierName(_settlement.CurrentTier)}",
            _bodyStyle);
        bool previousEnabled = GUI.enabled;
        GUI.enabled = _settlement.CanAdvanceTier(_stockpile, out string advanceReason);
        if (GUI.Button(new Rect(panel.x + 14f, panel.y + 103f, panel.width - 28f, 24f), advanceReason))
        {
            _settlement.TryAdvanceTier(_stockpile);
        }
        GUI.enabled = previousEnabled;
    }

    private void DrawStatusPanel()
    {
        Rect panel = GetStatusPanelRect();
        float width = panel.width;
        float x = panel.x;
        GUI.Box(panel, GUIContent.none, _panelStyle);

        int selected = _selection == null ? 0 : _selection.SelectedCount;
        int enemies = EnemyUnit.ActiveEnemies.Count;
        int residents = _settlement == null ? 3 : _settlement.CurrentResidents;
        int housing = _settlement == null ? 3 : _settlement.HousingCapacity;
        int availableResidents = TownResident.AvailableCount;
        GUI.Label(
            new Rect(x + 14f, panel.y + 7f, width - 28f, 20f),
            $"Герой {selected}   Жители {residents}/{housing}   Свободны {availableResidents}   Дух {_settlement?.CurrentMorale ?? 0}   Враги {enemies}",
            _bodyStyle);

        int health = _session == null || _session.CampCore == null ? 0 : _session.CampCore.Health;
        int maxHealth = _session == null || _session.CampCore == null ? 100 : _session.CampCore.MaxHealth;
        float healthRatio = maxHealth <= 0 ? 0f : Mathf.Clamp01((float)health / maxHealth);

        GUI.Label(new Rect(x + 14f, panel.y + 29f, 70f, 18f), "Ратуша", _hintStyle);
        Rect bar = new(x + 76f, panel.y + 33f, 104f, 10f);
        GUI.Box(bar, GUIContent.none, _barBackgroundStyle);
        GUI.Box(new Rect(bar.x, bar.y, bar.width * healthRatio, bar.height), GUIContent.none, _barFillStyle);
        GUI.Label(new Rect(x + 185f, panel.y + 27f, 45f, 20f), $"{health}", _hintStyle);

        if (_stockpile != null)
        {
            GUI.Label(new Rect(x + 238f, panel.y + 29f, width - 250f, 18f), BuildBasicResourceSummary(), _hintStyle);
            GUI.Label(
                new Rect(x + 14f, panel.y + 51f, width - 210f, 18f),
                BuildAdvancedResourceSummary() + $"   Склад {_stockpile.UsedCapacity}/{_stockpile.StorageCapacity}",
                _hintStyle);
        }

        DrawExpeditionControl(panel);
    }

    private void DrawExpeditionControl(Rect panel)
    {
        if (_expedition == null)
        {
            return;
        }

        float buttonWidth = Mathf.Min(245f, panel.width * 0.42f);
        Rect buttonRect = new(
            panel.xMax - buttonWidth - 14f,
            panel.y + 74f,
            buttonWidth,
            24f);
        Rect reportRect = new(
            panel.x + 14f,
            panel.y + 74f,
            panel.width - buttonWidth - 38f,
            24f);

        if (_expedition.IsActive)
        {
            GUI.Label(reportRect, $"Переход на остров #{_expedition.CompletedExpeditions + 1}", _hintStyle);
            GUI.Box(buttonRect, GUIContent.none, _barBackgroundStyle);
            GUI.Box(
                new Rect(buttonRect.x, buttonRect.y, buttonRect.width * _expedition.Progress, buttonRect.height),
                GUIContent.none,
                _barFillStyle);
            GUI.Label(buttonRect, $"ЗАГРУЗКА {Mathf.RoundToInt(_expedition.Progress * 100f)}%", _centeredHintStyle);
            return;
        }

        GUI.Label(reportRect, _expedition.LastReport, _hintStyle);
        if (GUI.Button(buttonRect, "ЛИЧНАЯ ВЫЛАЗКА • " + _expedition.GetCostSummary()))
        {
            _expedition.TryStart(out _);
        }
    }

    private string BuildBasicResourceSummary()
    {
        return $"Дерево {_stockpile.Get(ResourceType.Timber)}   Камень {_stockpile.Get(ResourceType.Stone)}   " +
               $"Еда {_stockpile.Get(ResourceType.Food)}   Травы {_stockpile.Get(ResourceType.Herb)}   " +
               $"Глина {_stockpile.Get(ResourceType.Clay)}";
    }

    private string BuildAdvancedResourceSummary()
    {
        return $"Доски {_stockpile.Get(ResourceType.Plank)}   Кирпич {_stockpile.Get(ResourceType.Brick)}   " +
               $"Инстр. {_stockpile.Get(ResourceType.Tool)}   Железо {_stockpile.Get(ResourceType.OldIron)}   " +
               $"Провиант {_stockpile.Get(ResourceType.Provisions)}";
    }

    private void DrawBuildingPanel()
    {
        TownBuilding building = _selection == null ? null : _selection.SelectedBuilding;
        if (building == null)
        {
            return;
        }

        Rect panel = GetBuildingPanelRect();
        float width = panel.width;
        float x = panel.x;
        float y = panel.y;
        GUI.Box(panel, GUIContent.none, _panelStyle);
        GUI.Label(new Rect(x + 14f, y + 10f, width - 28f, 24f), building.DisplayName, _titleStyle);

        if (building.IsUnderConstruction)
        {
            DrawConstructionState(building, x, y, width);
        }
        else
        {
            _phaseStyle.normal.textColor = building.IsRuined
                ? new Color(1f, 0.50f, 0.28f)
                : new Color(0.48f, 0.92f, 0.55f);
            GUI.Label(
                new Rect(x + 14f, y + 41f, width - 28f, 22f),
                building.IsRuined ? "РУИНЫ" : "ГОТОВО",
                _phaseStyle);
            string buildingEffect = building.Definition == null
                ? "Главное здание поселения"
                : building.Definition.EffectSummary;
            GUI.Label(new Rect(x + 14f, y + 67f, width - 28f, 20f), buildingEffect, _bodyStyle);

            if (building.ProductionWorkerCapacity > 0)
            {
                GUI.Label(
                    new Rect(x + 14f, y + 91f, width - 28f, 18f),
                    $"Работники: {building.ProductionWorkerCount}/{building.ProductionWorkerCapacity}   {building.ProductionStatus}",
                    _hintStyle);

                bool previousEnabled = GUI.enabled;
                GUI.enabled = building.ProductionWorkerCount > 0;
                if (GUI.Button(new Rect(x + 14f, y + 113f, 42f, 25f), "−"))
                {
                    building.ReleaseProductionWorker();
                }

                GUI.enabled = building.ProductionWorkerCount < building.ProductionWorkerCapacity && TownResident.AvailableCount > 0;
                if (GUI.Button(new Rect(x + 62f, y + 113f, width - 76f, 25f), "НАЗНАЧИТЬ ЖИТЕЛЯ"))
                {
                    building.TryAssignProductionWorker();
                }
                GUI.enabled = previousEnabled;

                if (building.ProductionRecipe != null)
                {
                    Rect productionBar = new(x + 14f, y + 145f, width - 28f, 10f);
                    GUI.Box(productionBar, GUIContent.none, _barBackgroundStyle);
                    GUI.Box(
                        new Rect(productionBar.x, productionBar.y, productionBar.width * building.ProductionProgress, productionBar.height),
                        GUIContent.none,
                        _barFillStyle);
                    GUI.Label(
                        new Rect(x + 14f, y + 158f, width - 28f, 18f),
                        building.ProductionRecipe.DisplayName,
                        _hintStyle);
                }
            }
            else
            {
                GUI.Label(
                    new Rect(x + 14f, y + 91f, width - 28f, 20f),
                    building.CanDemolish ? "Пассивный эффект поселения" : "Главное здание нельзя снести",
                    _hintStyle);
            }
        }

        if (!building.CanDemolish)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 0.58f, 0.50f, 1f);
        string action = building.IsUnderConstruction ? "ОТМЕНИТЬ СТРОЙКУ" : "СНЕСТИ ЗДАНИЕ";
        if (GUI.Button(new Rect(x + 14f, y + 188f, width - 28f, 28f), action))
        {
            building.Demolish();
        }
        GUI.color = previousColor;
    }

    private void DrawConstructionState(TownBuilding building, float x, float y, float width)
    {
        int percent = Mathf.RoundToInt(building.ConstructionProgress * 100f);
        bool waiting = building.AssignedWorkerCount == 0;
        _phaseStyle.normal.textColor = waiting
            ? new Color(0.42f, 0.76f, 0.96f)
            : new Color(1f, 0.76f, 0.30f);
        GUI.Label(
            new Rect(x + 14f, y + 41f, width - 28f, 22f),
            waiting ? "ЧЕРТЁЖ • ОЖИДАЕТ СТРОИТЕЛЕЙ" : $"СТРОИТСЯ • {percent}%",
            _phaseStyle);

        Rect progressBar = new(x + 14f, y + 68f, width - 28f, 12f);
        GUI.Box(progressBar, GUIContent.none, _barBackgroundStyle);
        GUI.Box(
            new Rect(progressBar.x, progressBar.y, progressBar.width * building.ConstructionProgress, progressBar.height),
            GUIContent.none,
            _barFillStyle);
        GUI.Label(
            new Rect(x + 14f, y + 88f, width - 28f, 20f),
            $"Строителей: {building.AssignedWorkerCount}   Свободных жителей: {TownResident.AvailableCount}",
            _bodyStyle);
        GUI.Label(
            new Rect(x + 14f, y + 110f, width - 28f, 18f),
            "Свободные жители назначаются автоматически.",
            _hintStyle);
    }

    private void DrawControlsPanel()
    {
        if (!_showControls)
        {
            return;
        }

        Rect panel = GetControlsPanelRect();
        GUI.Box(panel, GUIContent.none, _panelStyle);
        GUI.Label(
            new Rect(panel.x + 14f, panel.y + 8f, panel.width - 28f, 22f),
            "LMB select   RMB move   MMB orbit   WASD pan",
            _bodyStyle);
        GUI.Label(
            new Rect(panel.x + 14f, panel.y + 31f, panel.width - 28f, 20f),
            "B build   Wheel zoom   H hide help",
            _hintStyle);
    }

    private void DrawBuildButton()
    {
        Rect button = GetBuildButtonRect();
        if (GUI.Button(button, GUIContent.none, _buildButtonStyle))
        {
            ToggleBuildInterface();
        }

        Rect iconRect = new(button.x + 16f, button.y + 10f, 52f, 52f);
        GUI.DrawTexture(iconRect, _buildIcon, ScaleMode.ScaleToFit, true);
        GUI.Label(
            new Rect(button.x + 80f, button.y + 12f, button.width - 94f, 25f),
            "СТРОИТЕЛЬСТВО",
            _phaseStyle);
        GUI.Label(
            new Rect(button.x + 80f, button.y + 38f, button.width - 94f, 22f),
            _roadPlacement != null && _roadPlacement.IsPlacing
                ? "ЛКМ точки   T тип   ПКМ готово"
                : _placement != null && _placement.IsPlacing
                    ? "R поворот   G сетка"
                    : _showBuildMenu ? "B  закрыть" : "B  открыть",
            _hintStyle);
    }

    private void DrawBuildMenu()
    {
        if (!_showBuildMenu)
        {
            return;
        }

        Rect panel = GetBuildMenuRect();
        GUI.Box(panel, GUIContent.none, _buildDockStyle);
        if (_variantDefinition != null)
        {
            DrawBuildingVariantPicker(panel);
            return;
        }

        GUI.Label(new Rect(panel.x + 20f, panel.y + 13f, panel.width - 210f, 30f), "ЧЕРТЕЖИ ПОСЕЛЕНИЯ", _titleStyle);
        GUI.Label(
            new Rect(panel.x + 20f, panel.y + 43f, panel.width - 210f, 22f),
            $"{_buildingCatalog?.Count ?? 0} зданий • уровень {SettlementState.GetTierName(_settlement.CurrentTier)}",
            _hintStyle);

        Rect roadButton = new(panel.x + panel.width - 190f, panel.y + 14f, 170f, 46f);
        if (GUI.Button(roadButton, "ДОРОГА  •  РАБОТА") &&
            _roadPlacement != null &&
            _roadPlacement.BeginPlacement())
        {
            SetBuildMenuOpen(false);
        }

        const float tabGap = 6f;
        float tabWidth = (panel.width - 40f - tabGap * (CategoryOrder.Length - 1)) / CategoryOrder.Length;
        float tabY = panel.y + 70f;
        for (int index = 0; index < CategoryOrder.Length; index++)
        {
            Rect tab = new(panel.x + 20f + index * (tabWidth + tabGap), tabY, tabWidth, 34f);
            Color previousColor = GUI.color;
            GUI.color = _activeCategory == CategoryOrder[index]
                ? new Color(1f, 0.82f, 0.43f, 1f)
                : new Color(0.72f, 0.74f, 0.69f, 1f);
            if (GUI.Button(tab, CategoryLabels[index]))
            {
                _activeCategory = CategoryOrder[index];
            }
            GUI.color = previousColor;
        }

        DrawBuildingCards(panel);
    }

    private void DrawBuildingCards(Rect panel)
    {
        if (_buildingCatalog == null)
        {
            return;
        }

        const float gap = 10f;
        const int cardsPerCategory = 5;
        float cardWidth = (panel.width - 40f - gap * (cardsPerCategory - 1)) / cardsPerCategory;
        float cardY = panel.y + 112f;
        int visibleIndex = 0;

        foreach (BuildingDefinition definition in _buildingCatalog)
        {
            if (definition == null || definition.Category != _activeCategory)
            {
                continue;
            }

            Rect card = new(
                panel.x + 20f + visibleIndex * (cardWidth + gap),
                cardY,
                cardWidth,
                150f);
            Color previousColor = GUI.color;
            bool unlocked = _settlement != null && _settlement.IsBuildingUnlocked(definition);
            bool canAfford = _stockpile != null && _stockpile.Has(definition.ConstructionCosts);
            GUI.color = unlocked
                ? canAfford ? Color.white : new Color(0.78f, 0.74f, 0.66f, 1f)
                : new Color(0.48f, 0.48f, 0.48f, 1f);
            bool clicked = GUI.Button(card, GUIContent.none, _buildTileStyle);
            GUI.color = previousColor;

            GUI.Label(new Rect(card.x + 8f, card.y + 8f, card.width - 16f, 36f), definition.DisplayName, _cardTitleStyle);
            string costText = unlocked
                ? definition.CostSummary
                : _settlement.GetBuildingLockReason(definition);
            GUI.Label(new Rect(card.x + 9f, card.y + 44f, card.width - 18f, 23f), costText, _phaseStyle);
            GUI.Label(new Rect(card.x + 9f, card.y + 68f, card.width - 18f, 22f), definition.EffectSummary, _hintStyle);
            GUI.Label(new Rect(card.x + 9f, card.y + 92f, card.width - 18f, 50f), definition.Description, _cardDescriptionStyle);

            if (clicked && unlocked && definition.HasVisualVariants)
            {
                _variantDefinition = definition;
            }
            else if (clicked && unlocked && _placement != null && _placement.BeginPlacement(definition))
            {
                SetBuildMenuOpen(false);
            }

            visibleIndex++;
            if (visibleIndex >= cardsPerCategory)
            {
                break;
            }
        }
    }

    private void DrawBuildingVariantPicker(Rect panel)
    {
        GUI.Label(new Rect(panel.x + 20f, panel.y + 13f, panel.width - 220f, 30f), "ВЫБЕРИТЕ ОБЛИК ИЗБЫ", _titleStyle);
        GUI.Label(
            new Rect(panel.x + 20f, panel.y + 43f, panel.width - 220f, 22f),
            $"Одна постройка • {_variantDefinition.CostSummary} • {_variantDefinition.EffectSummary}",
            _hintStyle);

        if (GUI.Button(new Rect(panel.x + panel.width - 190f, panel.y + 14f, 170f, 46f), "НАЗАД К ЧЕРТЕЖАМ"))
        {
            _variantDefinition = null;
            return;
        }

        IReadOnlyList<BuildingVisualVariant> variants = _variantDefinition.VisualVariants;
        const float gap = 12f;
        float cardWidth = (panel.width - 40f - gap * Mathf.Max(0, variants.Count - 1)) / Mathf.Max(1, variants.Count);
        float cardY = panel.y + 76f;

        for (int index = 0; index < variants.Count; index++)
        {
            BuildingVisualVariant variant = variants[index];
            Rect card = new(
                panel.x + 20f + index * (cardWidth + gap),
                cardY,
                cardWidth,
                186f);
            bool clicked = GUI.Button(card, GUIContent.none, _buildTileStyle);
            GUI.Label(new Rect(card.x + 12f, card.y + 10f, card.width - 24f, 30f), variant.DisplayName, _cardTitleStyle);
            GUI.Label(new Rect(card.x + 12f, card.y + 45f, card.width - 24f, 22f), "ВНЕШНИЙ ВАРИАНТ", _phaseStyle);
            GUI.Label(new Rect(card.x + 12f, card.y + 73f, card.width - 24f, 74f), variant.Description, _cardDescriptionStyle);
            GUI.Label(new Rect(card.x + 12f, card.y + 153f, card.width - 24f, 22f), "ВЫБРАТЬ И РАЗМЕСТИТЬ", _hintStyle);

            if (clicked && _placement != null && _placement.BeginPlacement(_variantDefinition, variant))
            {
                SetBuildMenuOpen(false);
                return;
            }
        }
    }

    private void DrawPlacementStatus()
    {
        bool roadPlacementActive = _roadPlacement != null && _roadPlacement.IsPlacing;
        bool buildingPlacementActive = _placement != null && _placement.IsPlacing && _placement.ActiveDefinition != null;
        if (!roadPlacementActive && !buildingPlacementActive)
        {
            return;
        }

        Rect panel = GetPlacementStatusRect();
        GUI.Box(panel, GUIContent.none, _buildDockStyle);
        bool canPlace = roadPlacementActive ? _roadPlacement.CanPlace : _placement.CanPlace;
        _phaseStyle.normal.textColor = canPlace
            ? new Color(0.48f, 0.92f, 0.55f)
            : new Color(1f, 0.46f, 0.30f);
        string title = roadPlacementActive
            ? $"{_roadPlacement.GradeName}  •  СТРОИТ ЖИТЕЛЬ  •  T СМЕНИТЬ ТИП"
            : _placement.ActiveVisualVariant == null
                ? $"{_placement.ActiveDefinition.DisplayName}  •  {_placement.ActiveDefinition.CostSummary}"
                : $"{_placement.ActiveDefinition.DisplayName} • {_placement.ActiveVisualVariant.DisplayName}  •  {_placement.ActiveDefinition.CostSummary}";
        GUI.Label(
            new Rect(panel.x + 14f, panel.y + 7f, panel.width - 28f, 20f),
            title,
            _phaseStyle);
        GUI.Label(
            new Rect(panel.x + 14f, panel.y + 27f, panel.width - 28f, 18f),
            roadPlacementActive ? _roadPlacement.ValidationMessage : _placement.ValidationMessage,
            _centeredHintStyle);
    }

    private void ToggleBuildInterface()
    {
        if (_placement != null && _placement.IsPlacing)
        {
            _placement.CancelPlacement();
            SetBuildMenuOpen(true);
            return;
        }

        if (_roadPlacement != null && _roadPlacement.IsPlacing)
        {
            _roadPlacement.CancelPlacement();
            SetBuildMenuOpen(true);
            return;
        }

        SetBuildMenuOpen(!_showBuildMenu);
    }

    private void SetBuildMenuOpen(bool open)
    {
        _showBuildMenu = open;
        BuildMenuOpen = open;
        if (!open)
        {
            _variantDefinition = null;
        }
    }

    private static Rect GetBuildButtonRect()
    {
        return new Rect(
            (Screen.width - BuildButtonWidth) * 0.5f,
            Screen.height - BuildButtonHeight - 14f,
            BuildButtonWidth,
            BuildButtonHeight);
    }

    private static Rect GetGatherButtonRect()
    {
        Rect build = GetBuildButtonRect();
        return new Rect(
            build.x - GatherButtonWidth - 10f,
            build.y,
            GatherButtonWidth,
            BuildButtonHeight);
    }

    private static Rect GetDayNightRect()
    {
        return new Rect(Screen.width - 210f, Screen.height - 54f, 196f, 40f);
    }

    private static Rect GetResourceBarRect()
    {
        float width = Mathf.Min(1040f, Mathf.Max(700f, Screen.width - 260f));
        return new Rect((Screen.width - width) * 0.5f, 10f, width, ResourceBarHeight);
    }

    private static Rect GetResourceDropdownRect()
    {
        if (ActiveResourceDropdownIndex < 0)
        {
            return Rect.zero;
        }

        Rect bar = GetResourceBarRect();
        float slotWidth = (bar.width - 10f) / 7f;
        const float width = 420f;
        const float height = 322f;
        float center = bar.x + 5f + (ActiveResourceDropdownIndex + 0.5f) * slotWidth;
        return new Rect(Mathf.Clamp(center - width * 0.5f, 8f, Screen.width - width - 8f), bar.yMax + 5f, width, height);
    }

    private static Rect GetGatherMenuRect()
    {
        const float width = 690f;
        const float height = 94f;
        Rect gather = GetGatherButtonRect();
        return new Rect(Mathf.Max(8f, gather.x - 40f), gather.y - height - 10f, Mathf.Min(width, Screen.width - 16f), height);
    }

    private static Rect GetExpeditionButtonRect()
    {
        return new Rect(Screen.width - 182f, 16f, 166f, 48f);
    }

    private static Rect GetPhasePanelRect()
    {
        return new Rect(14f, 12f, 280f, 132f);
    }

    private static Rect GetStatusPanelRect()
    {
        float width = Mathf.Min(StatusPanelWidth, Mathf.Max(420f, Screen.width - 328f));
        return new Rect(Mathf.Max(14f, Screen.width - width - 14f), 12f, width, 105f);
    }

    private static Rect GetBuildMenuRect()
    {
        float width = Mathf.Min(1120f, Screen.width - 28f);
        const float height = 282f;
        return new Rect((Screen.width - width) * 0.5f, Screen.height - height - BuildButtonHeight - 26f, width, height);
    }

    private static Rect GetPlacementStatusRect()
    {
        const float width = 620f;
        const float height = 62f;
        Rect button = GetBuildButtonRect();
        return new Rect((Screen.width - width) * 0.5f, button.y - height - 10f, width, height);
    }

    private static Rect GetControlsPanelRect()
    {
        float width = Mathf.Min(380f, Screen.width - 28f);
        float y = Screen.width < 900f ? Screen.height - 126f : Screen.height - 70f;
        return new Rect(14f, y, width, 56f);
    }

    private static Rect GetBuildingPanelRect()
    {
        return new Rect(
            Mathf.Max(14f, Screen.width - BuildingPanelWidth - 14f),
            Screen.height - BuildingPanelHeight - 58f,
            BuildingPanelWidth,
            BuildingPanelHeight);
    }

    private string BuildPhaseText()
    {
        if (_session.Phase == GamePhase.Day)
        {
            return "DAY  -  no automatic timer";
        }

        if (_session.Phase == GamePhase.Night)
        {
            return "NIGHT  -  defend the shrine";
        }

        return _session.Phase == GamePhase.Victory
            ? "DAWN  -  the village survived"
            : "DEFEAT  -  the shrine fell";
    }

    private Color GetPhaseColor()
    {
        if (_session == null || _session.Phase == GamePhase.Day)
        {
            return new Color(0.96f, 0.77f, 0.34f);
        }

        if (_session.Phase == GamePhase.Night)
        {
            return new Color(0.56f, 0.70f, 1f);
        }

        return _session.Phase == GamePhase.Victory
            ? new Color(0.48f, 0.92f, 0.55f)
            : new Color(1f, 0.38f, 0.34f);
    }

    private void EnsureStyles()
    {
        if (_panelStyle != null &&
            _buildDockStyle != null &&
            _buildButtonStyle != null &&
            _buildTileStyle != null &&
            _cardTitleStyle != null &&
            _cardDescriptionStyle != null &&
            _buildIcon != null &&
            _gatherIcon != null &&
            _resourceIcons != null)
        {
            return;
        }

        GUI.skin.button.fontSize = 14;
        GUI.skin.button.fontStyle = FontStyle.Bold;

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = CreateTexture(new Color(0.035f, 0.04f, 0.045f, 0.88f)) },
            border = new RectOffset(1, 1, 1, 1)
        };

        _buildDockStyle = new GUIStyle(GUI.skin.box)
        {
            normal =
            {
                background = CreateRoundedTexture(
                    new Color(0.035f, 0.045f, 0.045f, 0.91f),
                    new Color(0.48f, 0.42f, 0.27f, 0.92f))
            },
            border = new RectOffset(18, 18, 18, 18)
        };

        _buildButtonStyle = new GUIStyle(GUI.skin.button)
        {
            normal =
            {
                background = CreateRoundedTexture(
                    new Color(0.10f, 0.12f, 0.115f, 0.94f),
                    new Color(0.72f, 0.58f, 0.29f, 1f))
            },
            hover =
            {
                background = CreateRoundedTexture(
                    new Color(0.16f, 0.18f, 0.16f, 0.97f),
                    new Color(0.92f, 0.74f, 0.36f, 1f))
            },
            active =
            {
                background = CreateRoundedTexture(
                    new Color(0.21f, 0.20f, 0.14f, 0.98f),
                    new Color(1f, 0.80f, 0.38f, 1f))
            },
            border = new RectOffset(18, 18, 18, 18)
        };

        _buildTileStyle = new GUIStyle(GUI.skin.box)
        {
            normal =
            {
                background = CreateRoundedTexture(
                    new Color(0.11f, 0.125f, 0.12f, 0.86f),
                    new Color(0.27f, 0.30f, 0.27f, 0.95f))
            },
            hover =
            {
                background = CreateRoundedTexture(
                    new Color(0.17f, 0.19f, 0.17f, 0.94f),
                    new Color(0.76f, 0.62f, 0.32f, 1f))
            },
            active =
            {
                background = CreateRoundedTexture(
                    new Color(0.22f, 0.21f, 0.15f, 0.96f),
                    new Color(0.96f, 0.77f, 0.34f, 1f))
            },
            border = new RectOffset(18, 18, 18, 18)
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.94f, 0.84f, 0.58f) }
        };

        _phaseStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = new Color(0.92f, 0.93f, 0.91f) }
        };

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = new Color(0.67f, 0.71f, 0.70f) }
        };

        _centeredHintStyle = new GUIStyle(_hintStyle)
        {
            alignment = TextAnchor.MiddleCenter
        };

        _cardTitleStyle = new GUIStyle(_bodyStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };

        _cardDescriptionStyle = new GUIStyle(_hintStyle)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 12,
            wordWrap = true
        };

        _barBackgroundStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = CreateTexture(new Color(0.10f, 0.12f, 0.12f, 1f)) }
        };

        _barFillStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = CreateTexture(new Color(0.30f, 0.72f, 0.40f, 1f)) }
        };

        _buildIcon = LoadUiIcon("UI/Icons/build", CreateBuildIcon);
        _gatherIcon = LoadUiIcon("UI/Icons/gather", CreateGatherIcon);
        _resourceIcons = new Texture2D[AllResources.Length];
        foreach (ResourceType resource in AllResources)
        {
            _resourceIcons[(int)resource] = LoadUiIcon(
                ResourceIconPaths[(int)resource],
                () => CreateResourceIcon(resource));
        }
    }

    private static Texture2D LoadUiIcon(string path, System.Func<Texture2D> fallbackFactory)
    {
        Texture2D icon = Resources.Load<Texture2D>(path);
        if (icon == null)
        {
            return fallbackFactory();
        }

        icon.filterMode = FilterMode.Bilinear;
        icon.wrapMode = TextureWrapMode.Clamp;
        return icon;
    }

    private void DrawResourceIcon(ResourceType resource, Rect rect)
    {
        Color previousColor = GUI.color;
        GUI.color = GetResourceIconColor(resource);
        GUI.DrawTexture(rect, _resourceIcons[(int)resource], ScaleMode.ScaleToFit, true);
        GUI.color = previousColor;
    }

    private static Texture2D CreateRoundedTexture(Color fill, Color border)
    {
        const int size = 64;
        const int radius = 14;
        const int borderWidth = 2;
        Texture2D texture = new(size, size)
        {
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = new(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool insideOuter = IsInsideRoundedRect(x, y, size, radius);
                bool insideInner = IsInsideRoundedRect(
                    x - borderWidth,
                    y - borderWidth,
                    size - borderWidth * 2,
                    radius - borderWidth);
                texture.SetPixel(x, y, !insideOuter ? clear : insideInner ? fill : border);
            }
        }

        texture.Apply();
        return texture;
    }

    private static bool IsInsideRoundedRect(int x, int y, int size, int radius)
    {
        if (x < 0 || y < 0 || x >= size || y >= size)
        {
            return false;
        }

        float closestX = Mathf.Clamp(x, radius, size - 1 - radius);
        float closestY = Mathf.Clamp(y, radius, size - 1 - radius);
        float deltaX = x - closestX;
        float deltaY = y - closestY;
        return deltaX * deltaX + deltaY * deltaY <= radius * radius;
    }

    private static Texture2D CreateBuildIcon()
    {
        const int size = 32;
        Texture2D texture = new(size, size)
        {
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color clear = new(0f, 0f, 0f, 0f);
        Color handle = new(0.49f, 0.29f, 0.13f, 1f);
        Color metal = new(0.93f, 0.78f, 0.42f, 1f);

        Color[] pixels = new Color[size * size];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = clear;
        }

        texture.SetPixels(pixels);
        for (int step = 0; step < 19; step++)
        {
            int x = 7 + Mathf.RoundToInt(step * 0.72f);
            int y = 5 + Mathf.RoundToInt(step * 0.72f);
            PaintCircle(texture, x, y, 2, handle);
        }

        for (int y = 20; y <= 26; y++)
        {
            for (int x = 16; x <= 29; x++)
            {
                texture.SetPixel(x, y, metal);
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D CreateGatherIcon()
    {
        Texture2D texture = CreateBlankIcon(32);
        Color stem = new(0.50f, 0.31f, 0.13f, 1f);
        Color leaf = new(0.40f, 0.74f, 0.31f, 1f);
        for (int step = 0; step < 20; step++)
        {
            PaintCircle(texture, 7 + step, 5 + step, 1, stem);
        }
        PaintCircle(texture, 11, 18, 5, leaf);
        PaintCircle(texture, 20, 11, 5, leaf);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateResourceIcon(ResourceType resource)
    {
        const int size = 28;
        Texture2D texture = CreateBlankIcon(size);
        Color color = GetResourceIconColor(resource);
        Color shadow = Color.Lerp(color, Color.black, 0.35f);
        int shape = (int)resource % 5;

        PaintCircle(texture, 14, 14, 11, new Color(0.07f, 0.08f, 0.075f, 0.88f));
        if (shape == 0)
        {
            for (int x = 8; x <= 19; x += 5)
            {
                for (int y = 7; y <= 21; y++)
                {
                    texture.SetPixel(x, y, y < 10 ? color : shadow);
                    texture.SetPixel(x + 1, y, color);
                }
            }
        }
        else if (shape == 1)
        {
            PaintCircle(texture, 10, 16, 5, shadow);
            PaintCircle(texture, 17, 14, 7, color);
        }
        else if (shape == 2)
        {
            for (int y = 7; y <= 21; y++)
            {
                int halfWidth = Mathf.Max(1, 7 - Mathf.Abs(14 - y) / 2);
                for (int x = 14 - halfWidth; x <= 14 + halfWidth; x++)
                {
                    texture.SetPixel(x, y, x < 14 ? shadow : color);
                }
            }
        }
        else if (shape == 3)
        {
            PaintCircle(texture, 14, 15, 7, color);
            for (int y = 5; y <= 10; y++)
            {
                texture.SetPixel(15, y, shadow);
                texture.SetPixel(16, y, shadow);
            }
        }
        else
        {
            for (int step = 0; step < 16; step++)
            {
                PaintCircle(texture, 6 + step, 7 + step, 2, step < 8 ? shadow : color);
                PaintCircle(texture, 21 - step, 7 + step, 1, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D CreateBlankIcon(int size)
    {
        Texture2D texture = new(size, size)
        {
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = Color.clear;
        }
        texture.SetPixels(pixels);
        return texture;
    }

    private static Color GetResourceIconColor(ResourceType resource)
    {
        return resource switch
        {
            ResourceType.Timber => new Color(0.69f, 0.43f, 0.20f),
            ResourceType.Stone => new Color(0.62f, 0.66f, 0.68f),
            ResourceType.Clay => new Color(0.73f, 0.38f, 0.24f),
            ResourceType.Food => new Color(0.76f, 0.30f, 0.22f),
            ResourceType.Herb => new Color(0.36f, 0.72f, 0.30f),
            ResourceType.Hide => new Color(0.58f, 0.38f, 0.24f),
            ResourceType.Plank => new Color(0.82f, 0.60f, 0.29f),
            ResourceType.Brick => new Color(0.71f, 0.30f, 0.22f),
            ResourceType.Tool => new Color(0.72f, 0.75f, 0.72f),
            ResourceType.Leather => new Color(0.49f, 0.27f, 0.15f),
            ResourceType.Grain => new Color(0.88f, 0.72f, 0.28f),
            ResourceType.Provisions => new Color(0.77f, 0.52f, 0.25f),
            ResourceType.Medicine => new Color(0.42f, 0.76f, 0.62f),
            ResourceType.OldIron => new Color(0.44f, 0.50f, 0.53f),
            ResourceType.Relic => new Color(0.74f, 0.56f, 0.86f),
            ResourceType.WardStone => new Color(0.39f, 0.67f, 0.86f),
            ResourceType.SkyGlass => new Color(0.46f, 0.85f, 0.94f),
            _ => Color.white
        };
    }

    private static void PaintCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int pixelX = centerX + x;
                int pixelY = centerY + y;
                if (x * x + y * y <= radius * radius &&
                    pixelX >= 0 && pixelX < texture.width &&
                    pixelY >= 0 && pixelY < texture.height)
                {
                    texture.SetPixel(pixelX, pixelY, color);
                }
            }
        }
    }

    private static Texture2D CreateTexture(Color color)
    {
        Texture2D texture = new(1, 1)
        {
            hideFlags = HideFlags.DontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        BuildMenuOpen = false;
        GatherMenuOpen = false;
        HasSelectedBuilding = false;
        ActiveResourceDropdownIndex = -1;

        if (_buildingCatalog == null)
        {
            return;
        }

        foreach (BuildingDefinition definition in _buildingCatalog)
        {
            if (definition != null && (definition.hideFlags & HideFlags.DontSave) != 0)
            {
                Destroy(definition);
            }
        }
    }
}
}
