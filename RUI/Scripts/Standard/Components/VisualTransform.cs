using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public class VisualTransform : CanvasComponent
	{
		private float internalAngle = 0f;
		private float2 internalScale = 1f;

		public float angle
		{
			get => internalAngle;
			set
			{
				if (math.abs(internalAngle - value) < 0.001f) return;
				internalAngle = value;
				MarkDirty();
			}
		}

		public float2 scale
		{
			get => internalScale;
			set
			{
				if (math.all(internalScale == value)) return;
				internalScale = value;
				MarkDirty();
			}
		}
	}
}