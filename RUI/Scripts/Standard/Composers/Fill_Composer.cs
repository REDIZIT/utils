using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public class Fill_Composer : CanvasComponent, IComposer, IMeasurable
	{
		public float4 padding;

		public DesiredSize Measure(SizeConstraints c)
		{
			SizeConstraints inner = c;
			inner.Shrink(padding.x + padding.z, padding.y + padding.w);

			float childMaxWidth = 0;
			float childMaxHeight = 0;

			foreach (CanvasElement child in Element.Children)
			{
				if (!child.isEnabled) continue;
				DesiredSize childSize = child.Measure(inner);
				childMaxWidth = math.max(childMaxWidth, childSize.x);
				childMaxHeight = math.max(childMaxHeight, childSize.y);
			}

			// Если родитель задал жесткий размер (например, SizedBox_Composer: size=(0, 20)), берем его
			float desiredWidth = c.x.TryGetMax(out float mw) ? mw : (childMaxWidth + padding.x + padding.z);
			float desiredHeight = c.y.TryGetMax(out float mh) ? mh : (childMaxHeight + padding.y + padding.w);

			return c.Clamp(new DesiredSize(desiredWidth, desiredHeight));
		}

		public void Arrange(ArrangeRect finalRect)
		{
			// Вычитаем паддинги
			ArrangeRect contentRect = finalRect.Shrink(padding);

			foreach (CanvasElement child in Element.Children)
			{
				if (!child.isEnabled) continue;
				
				// FILL = STRETCH! Насильно отдаем детям весь доступный контентный слот!
				child.Arrange(contentRect);
			}
		}
	}
}