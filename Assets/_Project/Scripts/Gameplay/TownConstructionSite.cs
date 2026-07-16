using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class TownConstructionSite : MonoBehaviour
{
    private const int MaximumWorkers = 3;
    private const float AssignmentInterval = 0.35f;
    private const float WorkPositionPadding = 1.15f;

    private readonly List<TownResident> _workers = new();
    private readonly List<TownResident> _assignmentCandidates = new();
    private readonly Dictionary<TownResident, int> _workerSlots = new();

    private TownBuilding _building;
    private float _workRequired;
    private float _completedWork;
    private float _nextAssignmentTime;
    private bool _acceptsWorkers;

    public bool AcceptsWorkers => _acceptsWorkers;
    public int AssignedWorkerCount => _workers.Count;
    public float Progress => _workRequired <= 0f ? 1f : Mathf.Clamp01(_completedWork / _workRequired);

    public void Initialize(TownBuilding building, float workRequired)
    {
        _building = building;
        _workRequired = Mathf.Max(1f, workRequired);
        _completedWork = 0f;
        _acceptsWorkers = true;
        _building?.SetConstructionProgress(0f);
    }

    public void ContributeWork(TownResident resident, float amount)
    {
        if (!_acceptsWorkers || resident == null || !_workers.Contains(resident) || amount <= 0f)
        {
            return;
        }

        _completedWork = Mathf.Min(_workRequired, _completedWork + amount);
        _building?.SetConstructionProgress(Progress);

        if (_completedWork >= _workRequired)
        {
            CompleteConstruction();
        }
    }

    public void ReleaseWorker(TownResident resident)
    {
        if (resident == null || !_workers.Remove(resident))
        {
            return;
        }

        _workerSlots.Remove(resident);
        resident.ReleaseConstruction(this);
    }

    public void Cancel()
    {
        if (!_acceptsWorkers && _workers.Count == 0)
        {
            return;
        }

        _acceptsWorkers = false;
        ReleaseAllWorkers();
        enabled = false;
    }

    private void Update()
    {
        RemoveMissingWorkers();

        if (!_acceptsWorkers || _workers.Count >= MaximumWorkers || Time.time < _nextAssignmentTime)
        {
            return;
        }

        _nextAssignmentTime = Time.time + AssignmentInterval;
        AssignNearestAvailableResident();
    }

    private void AssignNearestAvailableResident()
    {
        _assignmentCandidates.Clear();

        foreach (TownResident resident in TownResident.ActiveResidents)
        {
            if (resident == null || !resident.IsAvailable)
            {
                continue;
            }

            _assignmentCandidates.Add(resident);
        }

        _assignmentCandidates.Sort((left, right) =>
        {
            float leftDistance = (left.transform.position - transform.position).sqrMagnitude;
            float rightDistance = (right.transform.position - transform.position).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        int workerSlot = FindFreeWorkerSlot();
        foreach (TownResident candidate in _assignmentCandidates)
        {
            Vector3 workPosition = GetWorkPosition(workerSlot);
            if (!candidate.TryAssignConstruction(this, workPosition))
            {
                continue;
            }

            _workers.Add(candidate);
            _workerSlots[candidate] = workerSlot;
            return;
        }
    }

    private int FindFreeWorkerSlot()
    {
        for (int slot = 0; slot < MaximumWorkers; slot++)
        {
            if (!_workerSlots.ContainsValue(slot))
            {
                return slot;
            }
        }

        return 0;
    }

    private Vector3 GetWorkPosition(int workerIndex)
    {
        float footprint = _building == null || _building.Definition == null
            ? 4f
            : _building.Definition.Footprint;
        float radius = footprint * 0.56f + WorkPositionPadding;
        float angle = (35f + workerIndex * 120f) * Mathf.Deg2Rad;
        Vector3 localOffset = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        return transform.position + transform.rotation * localOffset;
    }

    private void CompleteConstruction()
    {
        _acceptsWorkers = false;
        _building?.CompleteConstruction();
        ReleaseAllWorkers();
        enabled = false;
    }

    private void ReleaseAllWorkers()
    {
        for (int index = _workers.Count - 1; index >= 0; index--)
        {
            TownResident resident = _workers[index];
            if (resident != null)
            {
                resident.ReleaseConstruction(this);
            }
        }

        _workers.Clear();
        _workerSlots.Clear();
    }

    private void RemoveMissingWorkers()
    {
        for (int index = _workers.Count - 1; index >= 0; index--)
        {
            if (_workers[index] == null)
            {
                TownResident missingResident = _workers[index];
                if (!ReferenceEquals(missingResident, null))
                {
                    _workerSlots.Remove(missingResident);
                }

                _workers.RemoveAt(index);
            }
        }
    }

    private void OnDestroy()
    {
        Cancel();
    }
}
}
