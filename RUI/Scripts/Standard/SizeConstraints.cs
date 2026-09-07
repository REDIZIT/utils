using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public struct SizeConstraints
	{
		public float? minX;
		public float? minY;
		public float? maxX;
		public float? maxY;

		public SizeConstraints(float2 size)
		{
			minX = null;
			minY = null;
			maxX = size.x;
			maxY = size.y;
		}
		
		public SizeConstraints(float? minX, float? minY, float? maxX, float? maxY)
		{
			this.minX = minX;
			this.minY = minY;
			this.maxX = maxX;
			this.maxY = maxY;
		}

		public float? GetMax(int index) => index == 0 ? maxX : maxY;
		public float? GetMin(int index) => index == 0 ? minX : minY;

		public void SetMax(int index, float v)
		{
			if (index == 0) maxX = v;
			else maxY = v;
		}
		
		public void SetMin(int index, float v)
		{
			if (index == 0) minX = v;
			else minY = v;
		}

		public PreferredSize Clamp(PreferredSize size)
		{
			return new(Clamp(size.size));
		}
		
		public float2 Clamp(float2 size)
		{
			float x = size.x;
			if (minX.HasValue && x < minX.Value) x = minX.Value;
			if (maxX.HasValue && x > maxX.Value) x = maxX.Value;

			float y = size.y;
			if (minY.HasValue && y < minY.Value) y = minY.Value;
			if (maxY.HasValue && y > maxY.Value) y = maxY.Value;

			return new float2(x, y);
		}
	}
}