using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public struct SizeConstraints
	{
		public float? minX;
		public float? minY;
		public float? maxX;
		public float? maxY;

		public SizeConstraints(float? minX, float? minY, float? maxX, float? maxY)
		{
			this.minX = minX;
			this.minY = minY;
			this.maxX = maxX;
			this.maxY = maxY;
		}

		// Вписываем размер в рамки (если они заданы):
		public float2 Constrain(float2 size)
		{
			float x = size.x;
			if (minX.HasValue && x < minX.Value) x = minX.Value;
			if (maxX.HasValue && x > maxX.Value) x = maxX.Value;

			float y = size.y;
			if (minY.HasValue && y < minY.Value) y = minY.Value;
			if (maxY.HasValue && y > maxY.Value) y = maxY.Value;

			return new float2(x, y);
		}

		// Хелперы:
		public static SizeConstraints Loose(float? maxX = null, float? maxY = null)
			=> new SizeConstraints(null, null, maxX, maxY);

		public static SizeConstraints Tight(float x, float y)
			=> new SizeConstraints(x, y, x, y);

		public static SizeConstraints Tight(float2 size)
			=> new SizeConstraints(size.x, size.y, size.x, size.y);
	}
}