using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public class SizedBox_Composer : CanvasComponent, IMeasurable, IComposer
	{
		public float2 size;

		public DesiredSize Measure(SizeConstraints constraints)
		{
			// 1. Измеряем детей в рамках ограничений родителя
			float childMaxW = 0;
			float childMaxH = 0;

			foreach (CanvasElement child in Element.Children)
			{
				if (!child.isEnabled) continue;
				DesiredSize childSize = child.Measure(constraints);
				childMaxW = math.max(childMaxW, childSize.x);
				childMaxH = math.max(childMaxH, childSize.y);
			}

			// Если в свойстве задано > 0, фиксируем его. Иначе берем размер детей
			float desiredW = size.x > 0 ? size.x : childMaxW;
			float desiredH = size.y > 0 ? size.y : childMaxH;

			return constraints.Clamp(new DesiredSize(desiredW, desiredH));
		}

		public void Arrange(ArrangeRect finalRect)
		{
			// Передаем детям размер, выделенный элементу родителем
			ArrangeRect childRect = new ArrangeRect(float2.zero, finalRect.size);

			foreach (CanvasElement child in Element.Children)
			{
				if (!child.isEnabled) continue;
				child.Arrange(childRect);
			}
		}
	}
}