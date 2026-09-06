using Unity.Mathematics;

namespace REDIZIT.RUI
{
    public class Stack_Composer : IComposer
    {
        public CanvasElement e;
        public LayoutDirection axis = LayoutDirection.Vertical;
        public float4 padding = 0;
        public float spacing = 0;

        public float2 Solve(SizeConstraints c)
        {
            int main = (int)axis;
            int cross = 1 - main;

            float2 totalPadding = new float2(padding.x + padding.z, padding.y + padding.w);

            float2 innerMax = c.max;
            if (!float.IsPositiveInfinity(innerMax[cross]))
                innerMax[cross] -= totalPadding[cross];

            float fixedMainConsumed = 0;
            float crossMax = 0;
            float totalFlex = 0;
            int expandedCount = 0;
            int activeChildrenCount = 0;

            float2 selfContent = e.GetPreferredContentSize();
            if (selfContent[cross] > 0) crossMax = math.max(crossMax, selfContent[cross]);
            if (e.transform.size[cross] > 0) crossMax = math.max(crossMax, e.transform.size[cross] - totalPadding[cross]);

            // ПАСС 1: Замеряем только активных не-Expanded детей
            for (int i = 0; i < e.children.Count; i++)
            {
                CanvasElement child = e.children[i];
                if (!child.isEnabled) continue; // Пропуск выключенных!

                activeChildrenCount++;

                if (child.composer is Expanded_Composer exp)
                {
                    totalFlex += exp.flex > 0 ? exp.flex : 1f;
                    expandedCount++;
                }
                else
                {
                    float2 childSize = child.SolveLayout(SizeConstraints.Loose(innerMax));
                    fixedMainConsumed += childSize[main];
                    crossMax = math.max(crossMax, childSize[cross]);
                }
            }

            float totalSpacing = math.max(0, activeChildrenCount - 1) * spacing;

            // ПАСС 2: Раздаем место только активным Expanded детям
            if (expandedCount > 0)
            {
                float availableMainSpace = 0;
                if (!float.IsPositiveInfinity(c.max[main]))
                {
                    availableMainSpace = c.max[main] - totalPadding[main] - totalSpacing - fixedMainConsumed;
                    availableMainSpace = math.max(0, availableMainSpace);
                }

                for (int i = 0; i < e.children.Count; i++)
                {
                    CanvasElement child = e.children[i];
                    if (!child.isEnabled) continue;

                    if (child.composer is Expanded_Composer exp)
                    {
                        float flexFactor = (exp.flex > 0 ? exp.flex : 1f) / (totalFlex > 0 ? totalFlex : 1f);
                        float allocatedMain = availableMainSpace * flexFactor;

                        float2 childMax = innerMax;
                        childMax[main] = allocatedMain;
                        if (float.IsPositiveInfinity(childMax[cross]) && crossMax > 0)
                            childMax[cross] = crossMax;

                        float2 childMin = 0;
                        childMin[main] = allocatedMain;

                        float2 childSize = child.SolveLayout(new SizeConstraints(childMin, childMax));
                        crossMax = math.max(crossMax, childSize[cross]);
                    }
                }
            }

            float2 totalSize = 0;
            if (expandedCount > 0 && !float.IsPositiveInfinity(c.max[main]))
                totalSize[main] = c.max[main];
            else
                totalSize[main] = fixedMainConsumed + totalPadding[main] + totalSpacing;

            totalSize[cross] = crossMax + totalPadding[cross];

            float2 finalSize = c.Constrain(totalSize);
            e.transform.size = finalSize;

            // ПАСС 3: Расстановка только активных детей
            float cursor = 0;
            for (int i = 0; i < e.children.Count; i++)
            {
                CanvasElement child = e.children[i];
                if (!child.isEnabled) continue;

                float2 childPos = 0;
                if (axis == LayoutDirection.Vertical)
                {
                    childPos.y = finalSize.y - padding.w - cursor - child.transform.size.y;
                    childPos.x = padding.x;
                    cursor += child.transform.size.y + spacing;
                }
                else
                {
                    childPos.x = padding.x + cursor;
                    childPos.y = padding.y;
                    cursor += child.transform.size.x + spacing;
                }
                child.transform.localPos = childPos;
            }

            return finalSize;
        }
    }
}