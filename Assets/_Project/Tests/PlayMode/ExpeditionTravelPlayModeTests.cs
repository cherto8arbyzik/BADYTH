using System.Collections;
using Hollowwest.Controls;
using Hollowwest.Economy;
using Hollowwest.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hollowwest.Tests
{

public sealed class ExpeditionTravelPlayModeTests
{
    [UnityTest]
    public IEnumerator ExpeditionTravel_LoadsIslandAndReturnsLootToLivingTown()
    {
        Scene originalScene = SceneManager.GetActiveScene();
        Scene townScene = SceneManager.CreateScene("Expedition Test Town");
        SceneManager.SetActiveScene(townScene);

        GameObject townRoot = new("Expedition Test Town Root");
        ResourceStockpile stockpile = townRoot.AddComponent<ResourceStockpile>();
        stockpile.Initialize(100, new[]
        {
            new ResourceAmount(ResourceType.Food, 20),
            new ResourceAmount(ResourceType.Provisions, 10)
        });
        SettlementState settlement = townRoot.AddComponent<SettlementState>();
        settlement.Initialize(3, 4, 100);
        ExpeditionSystem expedition = townRoot.AddComponent<ExpeditionSystem>();
        expedition.Initialize(stockpile, settlement, 20f);

        GameObject persistentTownMarker = new("Town State Marker");
        SceneManager.MoveGameObjectToScene(persistentTownMarker, townScene);

        Assert.That(expedition.TryStart(out string reason), Is.True, reason);

        float loadDeadline = Time.realtimeSinceStartup + 25f;
        ExpeditionSceneController controller = null;
        while (controller == null && Time.realtimeSinceStartup < loadDeadline)
        {
            controller = Object.FindFirstObjectByType<ExpeditionSceneController>();
            yield return null;
        }

        Assert.That(controller, Is.Not.Null, "Playable expedition scene did not finish loading");
        Assert.That(SceneManager.GetSceneByName("Expedition").isLoaded, Is.True);
        Assert.That(persistentTownMarker.activeSelf, Is.False, "Town roots should be preserved but inactive while traveling");
        Assert.That(GameInputRouter.Instance.Context, Is.EqualTo(PlayerControlContext.Hero));

        Assert.That(controller.Backpack.TryAdd(ResourceType.Timber, 4), Is.EqualTo(4));
        controller.EnterAnomaly(controller.Hero);
        Assert.That(controller.IsInAnomaly, Is.True);
        controller.MarkAnomalyCompleted();
        controller.RequestReturnHome(false);

        float returnDeadline = Time.realtimeSinceStartup + 25f;
        while (ExpeditionTravelService.Instance != null && Time.realtimeSinceStartup < returnDeadline)
        {
            yield return null;
        }

        Assert.That(ExpeditionTravelService.Instance, Is.Null, "Return to town did not finish");
        Assert.That(SceneManager.GetSceneByName("Expedition").isLoaded, Is.False);
        Assert.That(persistentTownMarker.activeSelf, Is.True);
        Assert.That(GameInputRouter.Instance.Context, Is.EqualTo(PlayerControlContext.Settlement));
        Assert.That(stockpile.Get(ResourceType.Timber), Is.EqualTo(4));
        Assert.That(settlement.HasDiscovery(DiscoveryType.AncientBlueprints), Is.True);
        Assert.That(settlement.CurrentResidents, Is.EqualTo(4));

        SceneManager.SetActiveScene(originalScene);
        Object.Destroy(townRoot);
        Object.Destroy(persistentTownMarker);
        yield return null;
        AsyncOperation unloadTown = SceneManager.UnloadSceneAsync(townScene);
        while (unloadTown != null && !unloadTown.isDone)
        {
            yield return null;
        }
    }
}

}
