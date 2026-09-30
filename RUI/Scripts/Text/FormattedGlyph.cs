using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public struct FormattedGlyph
	{
		public char character;
		public float2 position;
		public float2 size;
		public float4 uv;
		public Rect uvRect;
	}
}