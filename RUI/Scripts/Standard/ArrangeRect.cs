using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public readonly struct ArrangeRect
	{
		public readonly float2 pos;
		public readonly float2 size;

		public ArrangeRect(float2 pos, float2 size)
		{
			this.pos = pos;
			this.size = size;
		}

		public ArrangeRect Shrink(float4 padding)
		{
			return new(
				new(pos.x + padding.x, pos.y + padding.y), 
				new(size.x - (padding.x + padding.z), size.y - (padding.y + padding.w)));
		}

		public override string ToString()
		{
			return $"(pos: {pos}, size: {size})";
		}
	}
}