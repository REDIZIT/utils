using UnityEngine;

namespace REDIZIT.RUI
{
	public class Mask : CanvasComponent
	{
		public bool enabled = true;

		public Rect GetWorldClipRect()
		{
			if (Element == null) return CanvasGenerationContext.InfiniteClipRect;
			return Element.GetScreenBounds();
		}
	}
}