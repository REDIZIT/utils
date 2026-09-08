using System;
using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public struct AxisConstraints
	{
		private Mode mode;
		private float value;
		
		private enum Mode
		{
			Unlimited,
			LessOrEqual,
			Equal
		}

		public bool TryGetMax(out float maxSize)
		{
			if (mode == Mode.Unlimited)
			{
				maxSize = default;
				return false;
			}
			else
			{
				maxSize = value;
				return true;
			}
		}
		
		public float Clamp(float desiredSize)
		{
			if (mode == Mode.Unlimited) return desiredSize;
			if (mode == Mode.Equal) return value;
			if (mode == Mode.LessOrEqual) return math.min(desiredSize, value);
			throw new NotImplementedException();
		}

		public override string ToString()
		{
			if (mode == Mode.Unlimited) return $"unlimited";
			else if (mode == Mode.LessOrEqual) return $"<={value}";
			else if (mode == Mode.Equal) return $"=={value}";
			else throw new NotImplementedException();
		}
		
		public static AxisConstraints Unlimited() => new()
		{
			mode = Mode.Unlimited
		};
		
		public static AxisConstraints LessOrEqual(float maxSize) => new()
		{
			mode = Mode.LessOrEqual,
			value = maxSize
		};
		
		public static AxisConstraints Equal(float exactSize) => new()
		{
			mode = Mode.Equal,
			value = exactSize
		};
	}
}