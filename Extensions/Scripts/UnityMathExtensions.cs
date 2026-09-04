using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

public static class UnityMathExtensions
{
	private static readonly float _tolerance = .0001f;
	
	public static Vector3Int RoundToInt(this Vector3 v) => new(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
	public static Vector2Int RoundToInt(this Vector2 v) => new(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
	public static int2 RoundToInt2(this Vector2 v) => new(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
	public static int2 FloorToInt2(this Vector2 v) => new(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
	public static Vector2Int ToV2(this Vector3Int v) => new(v.x, v.y);


	public static int Int3ToIndex(int3 xyz, int n) => Int3ToIndex(xyz.x, xyz.y, xyz.z, n);
	public static int Int3ToIndex(int x, int y, int z, int n) => Int3ToIndex(x, y, z, n, n * n);

	public static int Int3ToIndex(int x, int y, int z, int n, int nn)
	{
		if (x >= n) throw new ArgumentException($"({x}, {y}, {z}).x is out of bounds n ({n})");
		if (y >= n) throw new ArgumentException($"({x}, {y}, {z}).y is out of bounds n ({n})");
		if (z >= n) throw new ArgumentException($"({x}, {y}, {z}).z is out of bounds n ({n})");
		return x + y * n + z * nn;
	}

	public static int Int2ToIndex(int2 xy, int n) => Int2ToIndex(xy.x, xy.y, n);

	public static int Int2ToIndex(int x, int y, int n)
	{
		if (x >= n) throw new ArgumentException($"({x}, {y}).x is out of bounds n ({n})");
		if (y >= n) throw new ArgumentException($"({x}, {y}).y is out of bounds n ({n})");
		return x + y * n;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int3 IndexToInt3(int index, int n)
	{
		int z = index / (n * n);
		index -= z * (n * n);
		int y = index / n;
		int x = index % n;

		return new int3(x, y, z);
	}

	public static int3 IndexToInt3(int index, int3 nnn)
	{
		int xyPlaneSize = nnn.x * nnn.y;
		int z = index / xyPlaneSize;
		index %= xyPlaneSize;
		int y = index / nnn.x;
		int x = index % nnn.x;

		return new int3(x, y, z);
	}

	public static int2 IndexToInt2(int index, int n)
	{
		int y = index / n;
		int x = index % n;

		return new int2(x, y);
	}
	
	
	public static float3 xyz(this float4 v) => new float3(v.x, v.y, v.z);
	public static Vector3 ToV3(this int3 i) => new(i.x, i.y, i.z);
	public static Vector3 ToV3(this float3 f) => new(f.x, f.y, f.z);
	public static int3 ToInt3(this Vector3 v) => new((int)v.x, (int)v.y, (int)v.z);
	public static int3 ToInt3(this Vector3Int v) => new(v.x, v.y, v.z);
	public static float3 ToFloat3(this Vector3 v) => new(v.x, v.y, v.z);
	public static float2 ToFloat2(this Vector3 v) => new(v.x, v.y);

	public static int3 FloorToInt(this float3 v) => new((int)v.x, (int)v.y, (int)v.z);
	public static int3 FloorToInt3(this Vector3 v) => new int3((int) v.x, (int) v.y, (int) v.z);

	public static int3 RoundToInt(this float3 v) => new((int) math.round(v.x), (int) math.round(v.y), (int) math.round(v.z));
	public static int3 CeilToInt(this float3 v) => new((int) math.ceil(v.x), (int) math.ceil(v.y), (int) math.ceil(v.z));
	public static int3 RoundToInt3(this Vector3 v) => new((int) math.round(v.x), (int) math.round(v.y), (int) math.round(v.z));

	public static Vector2 yx(this Vector2 v) => new(v.y, v.x);
	public static float4 ToFloat(this Color c) => new float4(c.r, c.g, c.b, c.a);
	public static float3 ToFloat3(this Color c) => new float3(c.r, c.g, c.b);
	
	

	public static bool Approx(this float value, in float other) {
		var difference = math.abs(value - other);
		return difference < _tolerance;
	}
	public static bool Approx(this float3 value, in float3 other) {
		var difference = math.abs(value - other);
		return difference.x < _tolerance &&
		       difference.y < _tolerance &&
		       difference.z < _tolerance;
	}
	
	public static void SetAxis(this ref int3 target, int axisIndex, int value) => target[axisIndex % 3] = value;
	public static void SetAxis(this ref int2 target, int axisIndex, int value) => target[axisIndex % 2] = value;
	public static void SetAxis(this ref float3 target, int axisIndex, float value) => target[axisIndex % 3] = value;
	public static void SetAxis(this ref float2 target, int axisIndex, float value) => target[axisIndex % 2] = value;
	
	public static int Count(this bool3 b, bool flag)
	{
		int count = 0;
		if (b.x == flag) count++;
		if (b.y == flag) count++;
		if (b.z == flag) count++;
		return count;
	}
	
	public static float3 Sum(this IEnumerable<float3> ls)
	{
		float3 sum = 0;
		foreach (float3 f in ls)
		{
			sum.x += f.x;
			sum.y += f.y;
			sum.z += f.z;
		}
		return sum;
	}
	
	
	public static Color ToColor(this float3 f) => new(f.x, f.y, f.z, 1);
	public static Color WithAlpha(this Color c, float alpha) => new(c.r, c.g, c.b, alpha);
}