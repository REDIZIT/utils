using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public struct DesiredSize
	{
		public float x, y;

		public DesiredSize(float x, float y)
		{
			this.x = x;
			this.y = y;
		}
		
		public DesiredSize(float2 size)
		{
			x = size.x;
			y = size.y;
		}

		public override string ToString()
		{
			return $"({x}; {y})";
		}
	}

	public struct ResolvedTransform
	{
		public float2 pos;
		public float2 size;
		public float angle;
		public float2 scale;

		public Matrix4x4 LocalToParent => localToParent;
		
		private Matrix4x4 localToParent;
		
		private ResolvedTransform(float2 pos, float2 size, float angle, float2 scale)
		{
			this.pos = pos;
			this.size = size;
			this.angle = angle;
			this.scale = scale;
			localToParent = default;
			
			RecalculateMatrix();
		}

		public static ResolvedTransform FromRect(ArrangeRect rect)
		{
			return new(rect.pos, rect.size, 0, 1);
		}

		private void RecalculateMatrix()
		{
			if (angle == 0f && Mathf.Approximately(scale.x, 1f) && Mathf.Approximately(scale.y, 1f))
			{
				localToParent = Matrix4x4.Translate(new(pos.x, pos.y, 0f));
			}
			else
			{
				float2 c = size * 0.5f;
				Vector3 centerInParent = new Vector3(pos.x + c.x, pos.y + c.y, 0f);
				Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
				Vector3 scale3D = new Vector3(scale.x, scale.y, 1f);

				Matrix4x4 trs = Matrix4x4.TRS(centerInParent, rotation, scale3D);
				Matrix4x4 invCenter = Matrix4x4.Translate(new(-c.x, -c.y, 0f));

				localToParent = trs * invCenter;
			}
		}

		public override string ToString()
		{
			// return $"(pos: {pos}, size: {size}, angle: {angle}, scale: {scale})";
			return $"(localPos: {pos.x}x{pos.y}, size: {size.x}x{size.y})";
		}
	}
}