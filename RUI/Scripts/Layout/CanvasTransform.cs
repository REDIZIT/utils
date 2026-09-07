using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public class CanvasTransform
	{
		public float2 pos = 0;
		public float2 size = 0;
		public float angle = 0f;
		public float2 scale = 1;
		
		public Matrix4x4 LocalToParent
		{
			get
			{
				if (angle == 0f && Mathf.Approximately(scale.x, 1f) && Mathf.Approximately(scale.y, 1f))
				{
					return Matrix4x4.Translate(new(pos.x, pos.y, 0f));
				}
				else
				{
					float2 c = size * 0.5f;
					Vector3 centerInParent = new Vector3(pos.x + c.x, pos.y + c.y, 0f);
					Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
					Vector3 scale3D = new Vector3(scale.x, scale.y, 1f);

					Matrix4x4 trs = Matrix4x4.TRS(centerInParent, rotation, scale3D);
					Matrix4x4 invCenter = Matrix4x4.Translate(new(-c.x, -c.y, 0f));

					return trs * invCenter;
				}
			}
		}
	}
}