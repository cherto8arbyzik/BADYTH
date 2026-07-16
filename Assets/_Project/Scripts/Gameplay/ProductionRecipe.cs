using System;
using System.Collections.Generic;
using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

[Serializable]
public sealed class ProductionRecipe
{
    [SerializeField] private string displayName;
    [SerializeField] private ResourceAmount[] inputs;
    [SerializeField] private ResourceAmount[] outputs;
    [SerializeField] private float cycleSeconds;

    public string DisplayName => displayName;
    public IReadOnlyList<ResourceAmount> Inputs => inputs;
    public IReadOnlyList<ResourceAmount> Outputs => outputs;
    public float CycleSeconds => cycleSeconds;

    public ProductionRecipe(
        string recipeName,
        ResourceAmount[] recipeInputs,
        ResourceAmount[] recipeOutputs,
        float recipeCycleSeconds)
    {
        displayName = recipeName;
        inputs = recipeInputs ?? Array.Empty<ResourceAmount>();
        outputs = recipeOutputs ?? Array.Empty<ResourceAmount>();
        cycleSeconds = Mathf.Max(1f, recipeCycleSeconds);
    }
}
}
