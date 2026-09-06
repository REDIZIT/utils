using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
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

	        float2 totalSize = 0;
	        totalSize[main] = mainConsumed + (main == 1 ? (padding.y + padding.w) : (padding.x + padding.z));
	        totalSize[cross] = crossMax + (cross == 1 ? (padding.x + padding.z) : (padding.y + padding.w));

	        float2 finalSize = c.Constrain(totalSize);
	        e.transform.size = finalSize;

	        float cursor = 0;
	        for (int i = 0; i < e.children.Count; i++)
	        {
		        var child = e.children[i];
		        float2 childPos = 0;

		        if (axis == LayoutDirection.Vertical)
		        {
			        // Высота родителя - отступ сверху - курсор - высота самого ребенка
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
