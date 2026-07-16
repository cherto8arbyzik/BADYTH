using System.Collections;
using System.Collections.Generic;
using Hollowwest.Economy;
using Hollowwest.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollowwest.Gameplay
{

/// <summary>
/// Loads the playable expedition additively while keeping the complete town
/// scene alive but inactive. Returning restores the exact same town state.
/// </summary>
public sealed class ExpeditionTravelService : MonoBehaviour
{
    private struct RootState
    {
        public GameObject Root;
        public bool WasActive;
    }

    private readonly List<RootState> _townRoots = new();
    private ExpeditionSystem _owner;
    private Scene _townScene;
    private Scene _expeditionScene;
    private bool _returning;

    public static ExpeditionTravelService Instance { get; private set; }
    public static bool IsTraveling => Instance != null;
    public float LoadProgress { get; private set; }

    public static bool Begin(ExpeditionSystem owner, ResourceStockpile stockpile)
    {
        if (Instance != null || owner == null || stockpile == null)
        {
            return false;
        }

        GameObject root = new("Expedition Travel Service");
        ExpeditionTravelService service = root.AddComponent<ExpeditionTravelService>();
        service._owner = owner;
        service._townScene = owner.gameObject.scene;
        DontDestroyOnLoad(root);
        service.StartCoroutine(service.LoadExpedition());
        return true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ReturnHome(ExpeditionBackpack backpack, bool anomalyCompleted, bool defeated)
    {
        if (_returning)
        {
            return;
        }

        _returning = true;
        IReadOnlyList<ResourceAmount> loot = backpack?.GetContents() ?? new List<ResourceAmount>();
        StartCoroutine(ReturnToTown(loot, anomalyCompleted, defeated));
    }

    private IEnumerator LoadExpedition()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync("Expedition", LoadSceneMode.Additive);
        if (operation == null)
        {
            _owner.CancelPlayableExpedition("Не удалось открыть внешний остров", true);
            Destroy(gameObject);
            yield break;
        }

        while (!operation.isDone)
        {
            LoadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }

        _expeditionScene = SceneManager.GetSceneByName("Expedition");
        if (!_expeditionScene.IsValid() || !_expeditionScene.isLoaded)
        {
            _owner.CancelPlayableExpedition("Сцена внешнего острова не загрузилась", true);
            Destroy(gameObject);
            yield break;
        }

        foreach (GameObject root in _townScene.GetRootGameObjects())
        {
            _townRoots.Add(new RootState { Root = root, WasActive = root.activeSelf });
        }

        SceneManager.SetActiveScene(_expeditionScene);
        GameObject expeditionRoot = new("Playable Expedition Root");
        SceneManager.MoveGameObjectToScene(expeditionRoot, _expeditionScene);
        ExpeditionSceneController controller = expeditionRoot.AddComponent<ExpeditionSceneController>();
        controller.Initialize(this, 9137 + _owner.CompletedExpeditions * 7919);

        foreach (RootState state in _townRoots)
        {
            if (state.Root != null)
            {
                state.Root.SetActive(false);
            }
        }

        GameInputRouter.EnsureExists().ActivateHero();
        LoadProgress = 1f;
    }

    private IEnumerator ReturnToTown(
        IReadOnlyList<ResourceAmount> loot,
        bool anomalyCompleted,
        bool defeated)
    {
        GameInputRouter.EnsureExists().ActivateSettlement();
        if (_townScene.IsValid())
        {
            SceneManager.SetActiveScene(_townScene);
        }

        foreach (RootState state in _townRoots)
        {
            if (state.Root != null)
            {
                state.Root.SetActive(state.WasActive);
            }
        }

        if (_expeditionScene.IsValid() && _expeditionScene.isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(_expeditionScene);
            while (unload != null && !unload.isDone)
            {
                yield return null;
            }
        }

        _owner.CompletePlayableExpedition(loot, anomalyCompleted, defeated);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

}
