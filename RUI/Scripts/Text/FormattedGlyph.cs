using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public struct FormattedGlyph
	{
		public char character;
		public float2 position;
		public float2 size;
		public float4 uv;
		public float scaleRatio; // Коэффициент масштабирования для SDF-сглаживания
	}
}
