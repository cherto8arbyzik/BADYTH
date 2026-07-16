using Hollowwest.Gameplay;
using UnityEngine;

namespace Hollowwest.Economy
{

public sealed class SettlementEconomy : MonoBehaviour
{
    private ResourceStockpile _stockpile;
    private SettlementState _settlement;
    private float _foodCycleSeconds;
    private float _foodTimer;

    public float FoodCycleProgress => _foodCycleSeconds <= 0f ? 0f : Mathf.Clamp01(_foodTimer / _foodCycleSeconds);
    public string LastFoodReport { get; private set; } = "Запасы пищи стабильны";

    public void Initialize(ResourceStockpile stockpile, SettlementState settlement, float foodCycleSeconds)
    {
        _stockpile = stockpile;
        _settlement = settlement;
        _foodCycleSeconds = Mathf.Max(10f, foodCycleSeconds);
        _foodTimer = 0f;
        SynchronizeStorage();
    }

    private void Update()
    {
        if (_stockpile == null || _settlement == null)
        {
            return;
        }

        SynchronizeStorage();
        _foodTimer += Time.deltaTime;
        if (_foodTimer >= _foodCycleSeconds)
        {
            _foodTimer = 0f;
            ProcessFoodCycle();
        }
    }

    public void ProcessFoodCycle()
    {
        if (_stockpile == null || _settlement == null)
        {
            return;
        }

        int requiredFood = _settlement.CurrentResidents * 2;
        bool fullyFed = _stockpile.TrySpend(ResourceType.Food, requiredFood);
        if (!fullyFed)
        {
            int remainingFood = _stockpile.Get(ResourceType.Food);
            _stockpile.TrySpend(ResourceType.Food, remainingFood);
        }

        _settlement.ApplyFoodCycle(fullyFed);
        LastFoodReport = fullyFed
            ? $"Жители получили {requiredFood} пищи"
            : $"Голод: требовалось {requiredFood} пищи";
    }

    private void SynchronizeStorage()
    {
        _stockpile.SetStorageCapacity(_settlement.StorageCapacity);
    }
}
}
