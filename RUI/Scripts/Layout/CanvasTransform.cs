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

		public Matrix4x4 LocalMatrix
		{
			get
			{
				if (angle == 0f && scale.x == 1f && scale.y == 1f)
				{
					return Matrix4x4.Translate(new Vector3(localPos.x, localPos.y, 0f));
				}

				float2 c = calculatedSize * 0.5f;
				Vector3 centerInParent = new Vector3(localPos.x + c.x, localPos.y + c.y, 0f);
				Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
				Vector3 scale3D = new Vector3(scale.x, scale.y, 1f);

				Matrix4x4 trs = Matrix4x4.TRS(centerInParent, rotation, scale3D);
				Matrix4x4 invCenter = Matrix4x4.Translate(new Vector3(-c.x, -c.y, 0f));

				return trs * invCenter;
			}
		}
	}
}