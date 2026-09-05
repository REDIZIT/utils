using Unity.Mathematics;
using UnityEngine;

namespace InGame.UI
{
    public class Stack_Composer : IComposer
    {
        public CanvasElement e;
        public LayoutDirection axis = LayoutDirection.Vertical;
        public float4 padding = float4.zero;
        public float spacing = 0f;

        public float2 Solve(SizeConstraints c)
        {
            int main = (int)axis;
            int cross = 1 - main;

            float mainConsumed = 0;
            float crossMax = 0;

            float2 innerMax = c.max;
            innerMax[cross] -= (cross == 0 ? (padding.x + padding.z) : (padding.y + padding.w));
            
            for (int i = 0; i < e.children.Count; i++)
            {
                float2 childSize = e.children[i].SolveLayout(SizeConstraints.Loose(innerMax));
                mainConsumed += childSize[main];
                if (i < e.children.Count - 1) mainConsumed += spacing;
                crossMax = math.max(crossMax, childSize[cross]);
            }

            float2 totalContent = 0;
            totalContent[main] = mainConsumed + (main == 1 ? (padding.y + padding.w) : (padding.x + padding.z));
            totalContent[cross] = crossMax + (cross == 1 ? (padding.x + padding.z) : (padding.y + padding.w));

            float2 finalSize = c.Constrain(totalContent);
            e.transform.size = finalSize;

            float topEdge = finalSize.y * (1f - e.transform.pivot.y);
            float leftEdge = -finalSize.x * e.transform.pivot.x;

            float cursor = 0;
            for (int i = 0; i < e.children.Count; i++)
            {
                var child = e.children[i];
                float2 childPos = 0;

                if (axis == LayoutDirection.Vertical)
                {
                    float slotTop = topEdge - padding.w - cursor;
                    // Центрируем ребенка по горизонтали внутри стека (Cross-Axis Alignment)
                    childPos.x = leftEdge + padding.x + child.transform.size.x * child.transform.pivot.x;
                    childPos.y = slotTop - child.transform.size.y * (1f - child.transform.pivot.y);
                    cursor += child.transform.size.y + spacing;
                }
                else
                {
                    float slotLeft = leftEdge + padding.x + cursor;
                    childPos.x = slotLeft + child.transform.size.x * child.transform.pivot.x;
                    // Центрируем ребенка по вертикали внутри горизонтального стека
                    childPos.y = (topEdge - finalSize.y) + padding.y + child.transform.size.y * child.transform.pivot.y;
                    cursor += child.transform.size.x + spacing;
                }
                child.transform.localPos = childPos;
            }

            return finalSize;
        }
    }
}