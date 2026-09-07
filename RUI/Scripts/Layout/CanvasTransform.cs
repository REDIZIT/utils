using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public class CanvasTransform
	{
		public float2 localPos = float2.zero;

		public float? width = null;
		public float? height = null;
		
		public float2 scale = new float2(1f, 1f);
		public float angle = 0f;

		public float2 calculatedSize;
	}
}