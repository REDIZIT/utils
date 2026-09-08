using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public struct SizeConstraints
	{
		public AxisConstraints x, y;

		public DesiredSize Clamp(DesiredSize desiredSize)
		{
			desiredSize.x = x.Clamp(desiredSize.x);
			desiredSize.y = y.Clamp(desiredSize.y);
			return desiredSize;
		}

		public void Shrink(float2 delta) => Shrink(delta.x, delta.y);
		public void Shrink(float deltaX, float deltaY)
		{
			x.Shrink(deltaX);
			y.Shrink(deltaY);
		}

		public override string ToString()
		{
			return $"(x: {x}, y: {y})";
		}
	}
}