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

            float? innerCrossMax = (cross == 0 ? c.maxX : c.maxY);
            if (innerCrossMax.HasValue)
            {
                innerCrossMax -= totalPadding[cross];
            }

            float fixedMainConsumed = 0;
            float crossMax = 0;
            float totalFlex = 0;
            int expandedCount = 0;
            int activeCount = 0;

            // Учитываем собственный размер контейнера (если задан)
            float? selfCross = (cross == 0 ? e.transform.width : e.transform.height);
            if (selfCross.HasValue)
            {
                crossMax = math.max(crossMax, selfCross.Value - totalPadding[cross]);
            }

            // ПАСС 1: Замеряем не-Expanded детей
            foreach (CanvasElement child in e.Children)
            {
                if (!child.isEnabled) continue;
                activeCount++;

                if (child.composer is Expanded_Composer exp)
                {
                    totalFlex += exp.flex > 0 ? exp.flex : 1f;
                    expandedCount++;
                }
                else
                {
                    SizeConstraints childC = (main == 0)
                        ? SizeConstraints.Loose(null, innerCrossMax)
                        : SizeConstraints.Loose(innerCrossMax, null);

                    float2 childSize = child.SolveLayout(childC);
                    fixedMainConsumed += childSize[main];
                    crossMax = math.max(crossMax, childSize[cross]);
                }
            }

            float totalSpacing = math.max(0, activeCount - 1) * spacing;

            // ПАСС 2: Раздаем место Expanded детям
            float? parentMainMax = (main == 0 ? c.maxX : c.maxY);
            if (expandedCount > 0 && parentMainMax.HasValue)
            {
                float availableMain = math.max(0, parentMainMax.Value - totalPadding[main] - totalSpacing - fixedMainConsumed);

                foreach (CanvasElement child in e.Children)
                {
                    if (!child.isEnabled) continue;

                    if (child.composer is Expanded_Composer exp)
                    {
                        float flexFactor = (exp.flex > 0 ? exp.flex : 1f) / (totalFlex > 0 ? totalFlex : 1f);
                        float allocatedMain = availableMain * flexFactor;

                        SizeConstraints expC = (main == 0)
                            ? new SizeConstraints(allocatedMain, null, allocatedMain, innerCrossMax ?? crossMax)
                            : new SizeConstraints(null, allocatedMain, innerCrossMax ?? crossMax, allocatedMain);

                        float2 childSize = child.SolveLayout(expC);
                        crossMax = math.max(crossMax, childSize[cross]);
                    }
                }
            }

            // Итоговый размер
            float? selfMain = (main == 0 ? e.transform.width : e.transform.height);
            float2 totalSize = 0;

            if (expandedCount > 0 && parentMainMax.HasValue)
            {
                totalSize[main] = parentMainMax.Value;
            }
            else if (selfMain.HasValue)
            {
                totalSize[main] = selfMain.Value;
            }
            else
            {
                totalSize[main] = fixedMainConsumed + totalPadding[main] + totalSpacing;
            }
            totalSize[cross] = crossMax + totalPadding[cross];

            float2 finalSize = c.Constrain(totalSize);
            e.transform.calculatedSize = finalSize;

            // ПАСС 3: Расстановка детей
            float cursor = 0;
            foreach (CanvasElement child in e.Children)
            {
                if (!child.isEnabled) continue;

                float2 childPos = 0;
                if (axis == LayoutDirection.Vertical)
                {
                    childPos.y = finalSize.y - padding.w - cursor - child.transform.calculatedSize.y;
                    childPos.x = padding.x;
                    cursor += child.transform.calculatedSize.y + spacing;
                }
                else
                {
                    childPos.x = padding.x + cursor;
                    childPos.y = padding.y;
                    cursor += child.transform.calculatedSize.x + spacing;
                }
                child.transform.localPos = childPos;
            }

            return e.transform.calculatedSize;
        }
    }
}