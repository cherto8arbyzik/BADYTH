using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Core;

public static class FormationPlanner
{
    public static void BuildCenteredGrid(
        Vector3 center,
        int count,
        float spacing,
        List<Vector3> output)
    {
        output.Clear();
        if (count <= 0)
        {
            return;
        }

        int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt(count / (float)columns);

        for (int index = 0; index < count; index++)
        {
            int row = index / columns;
            int column = index % columns;
            int itemsInRow = Mathf.Min(columns, count - row * columns);

            float x = (column - (itemsInRow - 1) * 0.5f) * spacing;
            float z = (row - (rows - 1) * 0.5f) * spacing;
            output.Add(center + new Vector3(x, 0f, z));
        }
    }

    public static void AssignNearestSlots(
        IReadOnlyList<Vector3> unitPositions,
        IReadOnlyList<Vector3> slots,
        List<int> slotByUnit)
    {
        slotByUnit.Clear();
        int assignmentCount = Mathf.Min(unitPositions.Count, slots.Count);

        for (int index = 0; index < unitPositions.Count; index++)
        {
            slotByUnit.Add(-1);
        }

        if (assignmentCount == 0)
        {
            return;
        }

        bool[] usedSlots = new bool[slots.Count];

        for (int assigned = 0; assigned < assignmentCount; assigned++)
        {
            int bestUnit = -1;
            int bestSlot = -1;
            float bestDistance = float.PositiveInfinity;

            for (int unitIndex = 0; unitIndex < unitPositions.Count; unitIndex++)
            {
                if (slotByUnit[unitIndex] >= 0)
                {
                    continue;
                }

                for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
                {
                    if (usedSlots[slotIndex])
                    {
                        continue;
                    }

                    float distance = (unitPositions[unitIndex] - slots[slotIndex]).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestUnit = unitIndex;
                        bestSlot = slotIndex;
                    }
                }
            }

            if (bestUnit < 0)
            {
                break;
            }

            slotByUnit[bestUnit] = bestSlot;
            usedSlots[bestSlot] = true;
        }
    }
}
