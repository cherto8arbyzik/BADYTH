using System.Collections.Generic;
using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class TownWorkplace : MonoBehaviour
{
    private readonly List<TownResident> _workers = new();

    private TownBuilding _building;
    private BuildingDefinition _definition;
    private ResourceStockpile _stockpile;
    private bool _operational;
    private float _cycleProgress;
    private int _cyclesSinceToolWear;
    private ResourceNode _forestTarget;

    public int WorkerCount => _workers.Count;
    public int WorkerCapacity => _definition == null ? 0 : _definition.WorkerCapacity;
    public float CycleProgress => _cycleProgress;
    public bool HasVacancy => _operational && _workers.Count < WorkerCapacity;
    public ProductionRecipe Recipe => _definition == null ? null : _definition.ProductionRecipe;
    public string Status { get; private set; } = "Не работает";

    public void Initialize(TownBuilding building, BuildingDefinition definition, ResourceStockpile stockpile)
    {
        _building = building;
        _definition = definition;
        _stockpile = stockpile;
        _operational = false;
        _cycleProgress = 0f;
        Status = "Строится";
    }

    public void SetOperational(bool operational)
    {
        _operational = operational;
        if (!operational)
        {
            ReleaseAllWorkers();
            Status = "Не работает";
        }
        else
        {
            Status = WorkerCapacity == 0 ? "Пассивный эффект" : "Нет работников";
        }
    }

    public bool TryAssignAvailableResident()
    {
        if (!HasVacancy)
        {
            return false;
        }

        TownResident closest = null;
        float closestDistance = float.MaxValue;
        foreach (TownResident resident in TownResident.ActiveResidents)
        {
            if (resident == null || !resident.IsAvailable)
            {
                continue;
            }

            float distance = (resident.transform.position - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = resident;
                closestDistance = distance;
            }
        }

        if (closest == null || !closest.TryAssignWorkplace(this, GetWorkPosition(_workers.Count)))
        {
            return false;
        }

        _workers.Add(closest);
        return true;
    }

    public bool ReleaseOneWorker()
    {
        if (_workers.Count == 0)
        {
            return false;
        }

        TownResident resident = _workers[_workers.Count - 1];
        _workers.RemoveAt(_workers.Count - 1);
        resident?.ReleaseWorkplace(this);
        return true;
    }

    public void ReleaseWorker(TownResident resident)
    {
        if (resident == null || !_workers.Remove(resident))
        {
            return;
        }

        resident.ReleaseWorkplace(this);
    }

    private void Update()
    {
        RemoveMissingWorkers();
        if (!_operational || WorkerCapacity == 0)
        {
            return;
        }

        if (_workers.Count == 0)
        {
            Status = "Нет работников";
            return;
        }

        ProductionRecipe recipe = Recipe;
        if (recipe == null)
        {
            Status = "Дежурство";
            return;
        }

        if (_stockpile == null)
        {
            Status = "Нет доступа к складу";
            return;
        }

        if (_definition != null && _definition.Id == "lumber_camp")
        {
            UpdateForestStewardship(recipe);
            return;
        }

        if (!_stockpile.Has(recipe.Inputs))
        {
            Status = "Не хватает сырья";
            return;
        }

        if (_cyclesSinceToolWear >= 5)
        {
            if (!_stockpile.TrySpend(ResourceType.Tool, 1))
            {
                Status = "Нужен новый инструмент";
                return;
            }

            _cyclesSinceToolWear = 0;
        }

        if (!_stockpile.CanAccept(recipe.Outputs))
        {
            Status = "Амбар заполнен";
            return;
        }

        Status = recipe.DisplayName;
        _cycleProgress += Time.deltaTime * _workers.Count / recipe.CycleSeconds;
        if (_cycleProgress < 1f)
        {
            return;
        }

        _cycleProgress = 0f;
        if (_stockpile.TrySpend(recipe.Inputs))
        {
            _stockpile.AddAll(recipe.Outputs);
            _cyclesSinceToolWear++;
        }
    }

    private void UpdateForestStewardship(ProductionRecipe recipe)
    {
        if (_forestTarget == null || _forestTarget.IsDepleted || !_forestTarget.IsRenewable)
        {
            _forestTarget = FindNearestRenewableTree();
            _cycleProgress = 0f;
        }

        if (_forestTarget == null)
        {
            Status = "Лес восстанавливается";
            return;
        }

        int arrivedWorkers = 0;
        for (int index = 0; index < _workers.Count; index++)
        {
            TownResident worker = _workers[index];
            if (worker == null)
            {
                continue;
            }

            float angle = index * Mathf.PI * 2f / Mathf.Max(1, _workers.Count);
            Vector3 workPosition = _forestTarget.InteractionPoint +
                                   new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.25f;
            worker.UpdateWorkplacePosition(this, workPosition);
            if (worker.IsAtWorkPosition)
            {
                arrivedWorkers++;
            }
        }

        if (arrivedWorkers == 0)
        {
            Status = "Идут к отмеченным деревьям";
            return;
        }

        if (_stockpile.UsedCapacity >= _stockpile.StorageCapacity)
        {
            Status = "Амбар заполнен";
            return;
        }

        if (_cyclesSinceToolWear >= 8)
        {
            if (!_stockpile.TrySpend(ResourceType.Tool, 1))
            {
                Status = "Нужен новый инструмент";
                return;
            }

            _cyclesSinceToolWear = 0;
        }

        Status = "Рубят и подсаживают лес";
        _cycleProgress += Time.deltaTime * arrivedWorkers / recipe.CycleSeconds;
        if (_cycleProgress < 1f)
        {
            return;
        }

        _cycleProgress = 0f;
        int harvested = _forestTarget.Harvest(4);
        if (harvested > 0)
        {
            _stockpile.Add(ResourceType.Timber, harvested);
            _cyclesSinceToolWear++;
        }
    }

    private ResourceNode FindNearestRenewableTree()
    {
        ResourceNode best = null;
        float bestDistance = 75f * 75f;
        foreach (ResourceNode node in ResourceNode.ActiveNodes)
        {
            if (node == null || node.ResourceType != ResourceType.Timber || node.IsDepleted || !node.IsRenewable)
            {
                continue;
            }

            float distance = (node.InteractionPoint - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                best = node;
                bestDistance = distance;
            }
        }

        return best;
    }

    private Vector3 GetWorkPosition(int workerIndex)
    {
        float footprint = _definition == null ? 4f : _definition.Footprint;
        float radius = footprint * 0.54f + 0.9f;
        float angle = (35f + workerIndex * 110f) * Mathf.Deg2Rad;
        Vector3 offset = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        return transform.position + transform.rotation * offset;
    }

    private void ReleaseAllWorkers()
    {
        for (int index = _workers.Count - 1; index >= 0; index--)
        {
            _workers[index]?.ReleaseWorkplace(this);
        }

        _workers.Clear();
    }

    private void RemoveMissingWorkers()
    {
        for (int index = _workers.Count - 1; index >= 0; index--)
        {
            if (_workers[index] == null)
            {
                _workers.RemoveAt(index);
            }
        }
    }

    private void OnDestroy()
    {
        ReleaseAllWorkers();
    }
}
}
