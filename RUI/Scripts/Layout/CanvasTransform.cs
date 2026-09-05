using Unity.Mathematics;
using UnityEngine;

public class CanvasTransform
{
	public float2 localPos = float2.zero;
	public float2 size = new float2(100f, 100f);
	public float2 scale = new float2(1f, 1f);
	public float2 pivot = float2.zero;

	public Matrix4x4 LocalMatrix => Matrix4x4.TRS(
		new Vector3(localPos.x, localPos.y, 0f),
		Quaternion.identity,
		new Vector3(scale.x, scale.y, 1f)
	);
}