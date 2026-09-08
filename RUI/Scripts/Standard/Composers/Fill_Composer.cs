using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public class Fill_Composer : CanvasComponent, IComposer, IMeasurable
	{
		public DesiredSize Measure(SizeConstraints c)
		{
			float childMaxWidth = 0;
			float childMaxHeight = 0;
			foreach (CanvasElement child in Element.Children)
			{
				DesiredSize childSize = child.measurable.Measure(c);
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

		public void Arrange(float2 size)
		{
			Debug.Log($"Fill arrange: {size}");

			foreach (CanvasElement child in Element.Children)
			{
				child.composer.Arrange(size);
			}

			Element.transform = new(0, size);
		}
	}
}