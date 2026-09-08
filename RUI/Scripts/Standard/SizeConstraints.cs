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

		public override string ToString()
		{
			return $"(x: {x}, y: {y})";
		}
	}
}