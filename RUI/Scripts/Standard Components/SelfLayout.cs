using Unity.Mathematics;
using UnityEngine;

namespace InGame.UI
{
    public struct SizeConstraints
    {
        public float2 min;
        public float2 max;

        public SizeConstraints(float2 min, float2 max)
        {
            this.min = min;
            this.max = max;
        }

        public float2 Constrain(float2 size) => math.clamp(size, min, max);
        
        public static SizeConstraints Loose(float2 max) => new SizeConstraints(float2.zero, max);
        public static SizeConstraints Tight(float2 size) => new SizeConstraints(size, size);

        public override string ToString()
        {
	        return $"(min: {min}; max: {max})";
        }
    }

    public interface IComposer
    {
        float2 Solve(SizeConstraints c);
    }

    // Стандартный композитор: просто выставляет размер и позицию из Transform
    public class Manual_Composer : IComposer
    {
        public CanvasElement e;

        public float2 Solve(SizeConstraints c)
        {
            // Решаем детей (в ручном режиме дети не ограничены родителем)
            for (int i = 0; i < e.children.Count; i++)
            {
                e.children[i].SolveLayout(SizeConstraints.Loose(new float2(float.PositiveInfinity)));
            }

            // Вписываем заданный в разметке size в ограничения
            float2 finalSize = c.Constrain(e.transform.size);
            e.transform.size = finalSize;
            
            return finalSize;
        }
    }

    // Композитор для LayoutGroup (Column / Row)
    public class Stack_Composer : IComposer
    {
        public CanvasElement e;

        public float2 Solve(SizeConstraints c)
	    {
	        var group = e.GetComponent<LayoutGroup>();
	        if (group == null) return new Manual_Composer { e = e }.Solve(c);

	        int axis = (int)group.direction;
	        float4 pad = group.padding;
	        float mainAxisConsumed = 0;
	        float crossAxisMax = 0;

	        // Подготавливаем ограничения для детей
	        float2 innerMax = c.max;
	        if (axis == 1) // Vertical
	        {
	            innerMax.y = float.PositiveInfinity;
	            innerMax.x -= (pad.x + pad.z);
	        }
	        else // Horizontal
	        {
	            innerMax.x = float.PositiveInfinity;
	            innerMax.y -= (pad.y + pad.w);
	        }

	        // 1. Первый проход: просим детей решить свои размеры
	        foreach (var child in e.children)
	        {
	            float2 childSize = child.SolveLayout(SizeConstraints.Loose(innerMax));
	            mainAxisConsumed += childSize[axis] + group.spacing;
	            crossAxisMax = math.max(crossAxisMax, childSize[1 - axis]);
	        }
	        if (e.children.Count > 0) mainAxisConsumed -= group.spacing;

	        // 2. Определяем размер самого контейнера
	        float2 totalContentSize = 0;
	        totalContentSize[axis] = mainAxisConsumed + (axis == 1 ? (pad.y + pad.w) : (pad.x + pad.z));
	        totalContentSize[1 - axis] = crossAxisMax + (axis == 1 ? (pad.x + pad.z) : (pad.y + pad.w));

	        float2 finalSize = group.fitContent ? c.Constrain(totalContentSize) : c.max;
	        e.transform.size = finalSize;

	        // 3. Второй проход: расставляем детей
	        // Считаем локальные границы контейнера относительно его пивота
	        float topEdge = finalSize.y * (1f - e.transform.pivot.y);
	        float leftEdge = -finalSize.x * e.transform.pivot.x;

	        float cursor = 0;
	        foreach (var child in e.children)
	        {
	            float2 childPos = 0;
	            if (axis == 1) // Vertical (Top -> Bottom)
	            {
	                // Позиция Y: от верхнего края отступаем вниз
	                float slotTop = topEdge - pad.w - cursor;
	                childPos.y = slotTop - child.transform.size.y * (1f - child.transform.pivot.y);
	                
	                // Позиция X: от левого края
	                childPos.x = leftEdge + pad.x + child.transform.size.x * child.transform.pivot.x;
	                
	                cursor += child.transform.size.y + group.spacing;
	            }
	            else // Horizontal (Left -> Right)
	            {
	                childPos.x = leftEdge + pad.x + cursor + child.transform.size.x * child.transform.pivot.x;
	                childPos.y = leftEdge + pad.y + child.transform.size.y * child.transform.pivot.y;
	                
	                cursor += child.transform.size.x + group.spacing;
	            }
	            child.transform.localPos = childPos;
	        }

	        return finalSize;
	    }
	}
}