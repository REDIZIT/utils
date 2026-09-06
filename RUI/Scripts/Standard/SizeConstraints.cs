using Unity.Mathematics;

namespace REDIZIT.RUI
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
        
		public static SizeConstraints Loose(float2 max) => new(float2.zero, max);
		public static SizeConstraints Tight(float2 size) => new(size, size);
	}
}