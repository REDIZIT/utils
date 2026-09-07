using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public struct SolveContext
	{
		public float2 containerSize;

		public SolveContext(float x, float y)
		{
			containerSize = new(x, y);
		}
	}

	public struct PreferredSize
	{
		public float2 size;

		public PreferredSize(float x, float y)
		{
			size = new(x, y);
		}
		
		public PreferredSize(float2 size)
		{
			this.size = size;
		}
	}

	public struct SolvedSize
	{
		public float2 size;

		public SolvedSize(float2 size)
		{
			this.size = size;
		}
	}
	
	
	public struct ResolvedTransform
	{
		public float2 pos;
		public float2 size;
		public float angle;
		public float2 scale;
		public Matrix4x4 localToParent;

		public ResolvedTransform(float2 pos, float2 size)
		{
			this.pos = pos;
			this.size = size;
			angle = 0;
			scale = 1;
			localToParent = default;
			
			RecalculateMatrix();
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
	}
}