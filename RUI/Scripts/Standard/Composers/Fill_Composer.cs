using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public class Fill_Composer : CanvasComponent, IComposer, IMeasurable
	{
		public float4 padding;
		
		public DesiredSize Measure(SizeConstraints c)
		{
			c.Shrink(padding.x + padding.z, padding.y + padding.w);
			
			float childMaxWidth = 0;
			float childMaxHeight = 0;
			foreach (CanvasElement child in Element.Children)
			{
				DesiredSize childSize = child.Measure(c);
				childMaxWidth = math.max(childMaxWidth, childSize.x);
				childMaxHeight = math.max(childMaxHeight, childSize.y);
			}

			float? desiredWidth = null;
			float? desiredHeight = null;
			if (c.x.TryGetMax(out float maxWidth)) desiredWidth = maxWidth;
			if (c.y.TryGetMax(out float maxHeight)) desiredHeight = maxHeight;

			desiredWidth ??= childMaxWidth;
			desiredHeight ??= childMaxHeight;
			
			Debug.Log($"Fill constraints: {c}, desired size: {desiredWidth}x{desiredHeight}");

			return new(desiredWidth!.Value, desiredHeight!.Value);
		}

		public void Arrange(ArrangeRect finalRect)
		{
			ArrangeRect childRect = finalRect.Shrink(padding);
			foreach (CanvasElement child in Element.Children)
			{
				child.Arrange(childRect);
			}
		}
	}
}