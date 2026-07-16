using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class RoadConstructionSite : MonoBehaviour
{
    private readonly List<Renderer> _renderers = new();
    private readonly List<Material[]> _finishedMaterials = new();
    private readonly List<Material> _blueprintMaterials = new();

    private RoadPlacementController _owner;
    private TownResident _worker;
    private Vector3 _start;
    private Vector3 _end;
    private float _workRequired;
    private float _completedWork;
    private float _nextAssignmentTime;
    private RoadGrade _grade;

    public bool AcceptsWorkers { get; private set; }
    public float Progress => _workRequired <= 0f ? 1f : Mathf.Clamp01(_completedWork / _workRequired);
    public Vector3 InteractionPoint => (_start + _end) * 0.5f;

    public void Initialize(
        RoadPlacementController owner,
        Vector3 start,
        Vector3 end,
        float workRequired,
        RoadGrade grade)
    {
        _owner = owner;
        _start = start;
        _end = end;
        _workRequired = Mathf.Max(1f, workRequired);
        _grade = grade;
        AcceptsWorkers = true;
        ConfigureBlueprintVisuals();
    }

    public void ContributeWork(TownResident resident, float amount)
    {
        if (!AcceptsWorkers || resident != _worker || amount <= 0f)
        {
            return;
        }

        _completedWork = Mathf.Min(_workRequired, _completedWork + amount);
        ApplyReveal();
        if (_completedWork >= _workRequired)
        {
            CompleteRoad();
        }
    }

    public void ReleaseWorker(TownResident resident)
    {
        if (_worker != resident)
        {
            return;
        }

        TownResident released = _worker;
        _worker = null;
        released?.ReleaseRoadConstruction(this);
    }

    private void Update()
    {
        if (!AcceptsWorkers || _worker != null || Time.time < _nextAssignmentTime)
        {
            return;
        }

        _nextAssignmentTime = Time.time + 0.4f;
        TownResident closest = null;
        float closestDistance = float.MaxValue;
        Vector3 midpoint = InteractionPoint;
        foreach (TownResident resident in TownResident.ActiveResidents)
        {
            if (resident == null || !resident.IsAvailable)
            {
                continue;
            }

            float distance = (resident.transform.position - midpoint).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = resident;
                closestDistance = distance;
            }
        }

        if (closest == null)
        {
            return;
        }

        Vector3 direction = (_end - _start).normalized;
        Vector3 workPosition = midpoint + Vector3.Cross(Vector3.up, direction) * 2.2f;
        if (closest.TryAssignRoadConstruction(this, workPosition))
        {
            _worker = closest;
        }
    }

    private void CompleteRoad()
    {
        AcceptsWorkers = false;
        RestoreFinishedMaterials();
        TownResident worker = _worker;
        _worker = null;
        worker?.ReleaseRoadConstruction(this);
        _owner?.RegisterCompletedRoad(_start, _end, _grade);
        enabled = false;
    }

    private void ConfigureBlueprintVisuals()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            Material[] originals = renderer.sharedMaterials;
            Material[] blueprints = new Material[originals.Length];
            for (int index = 0; index < originals.Length; index++)
            {
                Material material = new(originals[index])
                {
                    color = new Color(0.25f, 0.78f, 0.92f, 0.28f),
                    hideFlags = HideFlags.DontSave
                };
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 3000;
                blueprints[index] = material;
                _blueprintMaterials.Add(material);
            }

            _renderers.Add(renderer);
            _finishedMaterials.Add(originals);
            renderer.sharedMaterials = blueprints;
        }
    }

    private void ApplyReveal()
    {
        float alpha = Mathf.Lerp(0.28f, 0.72f, Progress);
        foreach (Material material in _blueprintMaterials)
        {
            if (material != null)
            {
                Color color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }
    }

    private void RestoreFinishedMaterials()
    {
        for (int index = 0; index < _renderers.Count; index++)
        {
            if (_renderers[index] != null)
            {
                _renderers[index].sharedMaterials = _finishedMaterials[index];
            }
        }

        foreach (Material material in _blueprintMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        _blueprintMaterials.Clear();
    }

    private void OnDestroy()
    {
        ReleaseWorker(_worker);
        if (AcceptsWorkers)
        {
            RestoreFinishedMaterials();
        }
    }
}
}
