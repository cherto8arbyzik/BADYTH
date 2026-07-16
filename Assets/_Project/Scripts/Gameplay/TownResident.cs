using System.Collections.Generic;
using Hollowwest.Economy;
using Hollowwest.Navigation;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class TownResident : MonoBehaviour
{
    private const float WorkArrivalDistance = 1.15f;
    private const float WorkRetryDelay = 0.8f;

    private static readonly List<TownResident> ActiveResidentsInternal = new();

    private NavigationAgent _agent;
    private Vector3 _home;
    private float _wanderRadius;
    private float _nextDecisionTime;
    private float _nextWorkRetryTime;
    private int _seed;
    private TownConstructionSite _constructionSite;
    private Vector3 _workPosition;
    private TownWorkplace _workplace;
    private RoadConstructionSite _roadConstructionSite;
    private ResourceNode _gatherTarget;
    private ResourceStockpile _gatherStockpile;
    private float _harvestTimer;
    private bool _dialoguePaused;

    public static IReadOnlyList<TownResident> ActiveResidents => ActiveResidentsInternal;
    public static int AvailableCount
    {
        get
        {
            int count = 0;
            foreach (TownResident resident in ActiveResidentsInternal)
            {
                if (resident != null && resident.IsAvailable)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public bool IsAvailable => !_dialoguePaused && _constructionSite == null && _workplace == null && _roadConstructionSite == null && _gatherTarget == null && isActiveAndEnabled;
    public bool IsBuilding => _constructionSite != null;
    public bool IsWorking => _workplace != null;
    public bool IsAtWorkPosition
    {
        get
        {
            Vector3 offset = _workPosition - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= WorkArrivalDistance * WorkArrivalDistance;
        }
    }
    public ResourceNode GatherTarget => _gatherTarget;
    public bool IsGathering => _gatherTarget != null;

    public void Initialize(NavigationAgent agent, Vector3 home, float wanderRadius, int seed)
    {
        _agent = agent;
        _home = home;
        _wanderRadius = Mathf.Max(1f, wanderRadius);
        _seed = seed;
        ScheduleNextDecision(0.8f, 2.2f);
    }

    private void Update()
    {
        if (_dialoguePaused)
        {
            return;
        }

        if (_constructionSite != null)
        {
            UpdateConstructionJob();
            return;
        }

        if (_workplace != null)
        {
            UpdateWorkplaceJob();
            return;
        }

        if (_roadConstructionSite != null)
        {
            UpdateRoadConstructionJob();
            return;
        }

        if (_gatherTarget != null)
        {
            UpdateGatheringJob();
            return;
        }

        if (_agent == null || _agent.IsMoving || Time.time < _nextDecisionTime)
        {
            return;
        }

        float angle = Hash01(_seed++) * Mathf.PI * 2f;
        float distance = Mathf.Lerp(_wanderRadius * 0.25f, _wanderRadius, Hash01(_seed++));
        Vector3 destination = _home + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;

        if (!_agent.SetDestination(destination))
        {
            ScheduleNextDecision(0.4f, 0.9f);
            return;
        }

        ScheduleNextDecision(2.5f, 5f);
    }

    public bool TryAssignConstruction(TownConstructionSite site, Vector3 workPosition)
    {
        if (!IsAvailable || site == null || !site.AcceptsWorkers || _agent == null)
        {
            return false;
        }

        _agent.Stop();
        _constructionSite = site;
        _workPosition = workPosition;
        _nextWorkRetryTime = 0f;

        if (_agent.SetDestination(_workPosition))
        {
            return true;
        }

        _constructionSite = null;
        return false;
    }

    public void ReleaseConstruction(TownConstructionSite site)
    {
        if (_constructionSite != site)
        {
            return;
        }

        _constructionSite = null;
        _agent?.Stop();
        ScheduleNextDecision(0.5f, 1.2f);
    }

    public bool TryAssignWorkplace(TownWorkplace workplace, Vector3 workPosition)
    {
        if (!IsAvailable || workplace == null || _agent == null)
        {
            return false;
        }

        _agent.Stop();
        _workplace = workplace;
        _workPosition = workPosition;
        _nextWorkRetryTime = 0f;
        if (_agent.SetDestination(_workPosition))
        {
            return true;
        }

        _workplace = null;
        return false;
    }

    public void ReleaseWorkplace(TownWorkplace workplace)
    {
        if (_workplace != workplace)
        {
            return;
        }

        _workplace = null;
        _agent?.Stop();
        ScheduleNextDecision(0.5f, 1.2f);
    }

    public void UpdateWorkplacePosition(TownWorkplace workplace, Vector3 workPosition)
    {
        if (_workplace != workplace || _agent == null)
        {
            return;
        }

        Vector3 difference = workPosition - _workPosition;
        difference.y = 0f;
        if (difference.sqrMagnitude < 0.20f)
        {
            return;
        }

        _workPosition = workPosition;
        _agent.Stop();
        _agent.SetDestination(_workPosition);
        _nextWorkRetryTime = Time.time + WorkRetryDelay;
    }

    public bool TryAssignRoadConstruction(RoadConstructionSite site, Vector3 workPosition)
    {
        if (!IsAvailable || site == null || _agent == null)
        {
            return false;
        }

        _agent.Stop();
        _roadConstructionSite = site;
        _workPosition = workPosition;
        _nextWorkRetryTime = 0f;
        if (_agent.SetDestination(_workPosition))
        {
            return true;
        }

        _roadConstructionSite = null;
        return false;
    }

    public void ReleaseRoadConstruction(RoadConstructionSite site)
    {
        if (_roadConstructionSite != site)
        {
            return;
        }

        _roadConstructionSite = null;
        _agent?.Stop();
        ScheduleNextDecision(0.5f, 1.2f);
    }

    public bool TryAssignGathering(ResourceNode target, ResourceStockpile stockpile)
    {
        if (!IsAvailable || target == null || target.IsDepleted || stockpile == null || _agent == null)
        {
            return false;
        }

        _agent.Stop();
        _gatherTarget = target;
        _gatherStockpile = stockpile;
        _harvestTimer = 0f;
        _nextWorkRetryTime = 0f;
        if (_agent.SetDestination(target.InteractionPoint))
        {
            return true;
        }

        _gatherTarget = null;
        _gatherStockpile = null;
        return false;
    }

    public void ReleaseGathering()
    {
        _gatherTarget = null;
        _gatherStockpile = null;
        _agent?.Stop();
        ScheduleNextDecision(0.5f, 1.2f);
    }

    public void BeginDialogue()
    {
        _dialoguePaused = true;
        _agent?.Stop();
    }

    public void EndDialogue()
    {
        _dialoguePaused = false;
        _nextWorkRetryTime = 0f;
        ScheduleNextDecision(0.5f, 1.1f);
    }

    private void UpdateConstructionJob()
    {
        if (!_constructionSite.AcceptsWorkers)
        {
            ReleaseConstruction(_constructionSite);
            return;
        }

        Vector3 offset = _workPosition - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= WorkArrivalDistance * WorkArrivalDistance)
        {
            _agent.Stop();
            FaceConstructionSite();
            _constructionSite.ContributeWork(this, Time.deltaTime);
            return;
        }

        if (_agent.IsMoving || Time.time < _nextWorkRetryTime)
        {
            return;
        }

        if (!_agent.SetDestination(_workPosition))
        {
            TownConstructionSite failedSite = _constructionSite;
            failedSite.ReleaseWorker(this);
            return;
        }

        _nextWorkRetryTime = Time.time + WorkRetryDelay;
    }

    private void FaceConstructionSite()
    {
        Vector3 direction = _constructionSite.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 10f * Time.deltaTime);
    }

    private void UpdateWorkplaceJob()
    {
        Vector3 offset = _workPosition - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= WorkArrivalDistance * WorkArrivalDistance)
        {
            _agent.Stop();
            FaceTarget(_workplace.transform.position);
            return;
        }

        if (_agent.IsMoving || Time.time < _nextWorkRetryTime)
        {
            return;
        }

        if (!_agent.SetDestination(_workPosition))
        {
            TownWorkplace failedWorkplace = _workplace;
            failedWorkplace.ReleaseWorker(this);
            return;
        }

        _nextWorkRetryTime = Time.time + WorkRetryDelay;
    }

    private void UpdateRoadConstructionJob()
    {
        if (!_roadConstructionSite.AcceptsWorkers)
        {
            ReleaseRoadConstruction(_roadConstructionSite);
            return;
        }

        Vector3 offset = _workPosition - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= WorkArrivalDistance * WorkArrivalDistance)
        {
            _agent.Stop();
            FaceTarget(_roadConstructionSite.InteractionPoint);
            _roadConstructionSite.ContributeWork(this, Time.deltaTime);
            return;
        }

        if (_agent.IsMoving || Time.time < _nextWorkRetryTime)
        {
            return;
        }

        if (!_agent.SetDestination(_workPosition))
        {
            RoadConstructionSite failedSite = _roadConstructionSite;
            failedSite.ReleaseWorker(this);
            return;
        }

        _nextWorkRetryTime = Time.time + WorkRetryDelay;
    }

    private void UpdateGatheringJob()
    {
        if (_gatherTarget == null || _gatherTarget.IsDepleted || _gatherStockpile == null ||
            _gatherStockpile.UsedCapacity >= _gatherStockpile.StorageCapacity)
        {
            ReleaseGathering();
            return;
        }

        Vector3 offset = _gatherTarget.InteractionPoint - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= WorkArrivalDistance * WorkArrivalDistance)
        {
            _agent.Stop();
            FaceTarget(_gatherTarget.InteractionPoint);
            _harvestTimer += Time.deltaTime;
            if (_harvestTimer >= 0.8f)
            {
                _harvestTimer = 0f;
                ResourceType type = _gatherTarget.ResourceType;
                _gatherStockpile.Add(type, _gatherTarget.Harvest());
            }
            return;
        }

        if (!_agent.IsMoving && Time.time >= _nextWorkRetryTime)
        {
            if (!_agent.SetDestination(_gatherTarget.InteractionPoint))
            {
                ReleaseGathering();
                return;
            }

            _nextWorkRetryTime = Time.time + WorkRetryDelay;
        }
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 8f * Time.deltaTime);
    }

    private void OnEnable()
    {
        if (!ActiveResidentsInternal.Contains(this))
        {
            ActiveResidentsInternal.Add(this);
        }
    }

    private void OnDisable()
    {
        if (_constructionSite != null)
        {
            _constructionSite.ReleaseWorker(this);
        }

        if (_workplace != null)
        {
            _workplace.ReleaseWorker(this);
        }

        if (_roadConstructionSite != null)
        {
            _roadConstructionSite.ReleaseWorker(this);
        }

        if (_gatherTarget != null)
        {
            ReleaseGathering();
        }

        ActiveResidentsInternal.Remove(this);
    }

    private void ScheduleNextDecision(float minimum, float maximum)
    {
        _nextDecisionTime = Time.time + Mathf.Lerp(minimum, maximum, Hash01(_seed++));
    }

    private static float Hash01(int value)
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
}
}
