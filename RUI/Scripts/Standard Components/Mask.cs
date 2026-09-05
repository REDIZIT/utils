using UnityEngine;

namespace InGame.UI
{
	public class Mask : CanvasComponent
	{
		public bool enabled = true;

		public Vector4 GetWorldClipRect()
		{
			if (Element == null) return CanvasGenerationContext.InfiniteClipRect;
			return Element.GetScreenBounds();
		}
	}
}