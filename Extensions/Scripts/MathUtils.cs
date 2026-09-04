using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BaseMathUtils
{
	// Возвращает кратчайшее расстояние от точки до реального треугольника (с учетом его краев)
    public static float DistancePointToTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = p - a;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return Vector3.Distance(p, a);

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return Vector3.Distance(p, b);

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float v = d1 / (d1 - d3);
            return Vector3.Distance(p, a + v * ab);
        }

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return Vector3.Distance(p, c);

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float w = d2 / (d2 - d6);
            return Vector3.Distance(p, a + w * ac);
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return Vector3.Distance(p, b + w * (c - b));
        }

        float denom = 1f / (va + vb + vc);
        float vn = vb * denom;
        float wn = vc * denom;
        return Vector3.Distance(p, a + ab * vn + ac * wn);
    }
    
    public static float SqDistancePointToTriangle(float3 p, float3 a, float3 b, float3 c)
    {
        float3 ab = b - a;
        float3 ac = c - a;
        float3 ap = p - a;

        float d1 = math.dot(ab, ap);
        float d2 = math.dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return math.distancesq(p, a);

        float3 bp = p - b;
        float d3 = math.dot(ab, bp);
        float d4 = math.dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return math.distancesq(p, b);

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float v = d1 / (d1 - d3);
            return math.distancesq(p, a + v * ab);
        }

        float3 cp = p - c;
        float d5 = math.dot(ab, cp);
        float d6 = math.dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return math.distancesq(p, c);

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float w = d2 / (d2 - d6);
            return math.distancesq(p, a + w * ac);
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return math.distancesq(p, b + w * (c - b));
        }

        float denom = 1f / (va + vb + vc);
        float vn = vb * denom;
        float wn = vc * denom;
        return math.distancesq(p, a + ab * vn + ac * wn);
    }
    
    public static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = p - a;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return a;

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return b;

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float v = d1 / (d1 - d3);
            return a + v * ab;
        }

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return c;

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float w = d2 / (d2 - d6);
            return a + w * ac;
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return b + w * (c - b);
        }

        float denom = 1f / (va + vb + vc);
        float vn = vb * denom;
        float wn = vc * denom;
        return a + ab * vn + ac * wn;
    }

    public static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 dir = Vector3.Cross(b - a, c - a);
        Vector3 norm = Vector3.Normalize(dir);
        return norm;
    }

    
    public static float DistancePointToPlane(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        // 1. Находим нормаль к плоскости через векторное произведение сторон
        Vector3 side1 = b - a;
        Vector3 side2 = c - a;
        Vector3 normal = Vector3.Cross(side1, side2);

        // 2. Находим длину нормали
        float magnitude = normal.magnitude;

        // Проверка на вырожденный треугольник (если точки на одной линии, площадь 0)
        if (magnitude < 0.000001f) return Vector3.Distance(p, a);

        // 3. Расстояние — это скалярное произведение вектора (P - A) 
        // на нормализованную нормаль. Берем модуль для получения расстояния.
        // Формула: dist = |(P - A) · N| / |N|
        return Mathf.Abs(Vector3.Dot(p - a, normal)) / magnitude;
    }

    public static Random rnd;

    private static RaycastHit[] hits = new RaycastHit[32];
    private static RaycastHitDstComparer hitsComparer = new();

    public static int IndexFromCoordRev(int3 xyz, int n) => IndexFromCoordRev(xyz.x, xyz.y, xyz.z, n);
    public static int IndexFromCoordRev(int x, int y, int z, int n) => x * n * n + y * n + z;


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
        // if (y >= n) throw new ArgumentException($"({x}, {y}).y is out of bounds n ({n})");
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


    public static int IndexFromCoord(int2 xy, int n) => IndexFromCoord(xy.x, xy.y, n);
    public static int IndexFromCoord(int x, int y, int n) => x * n + y;


    public static float3 Interpolate(float4 a, float4 b, float x) => a.xyz + (x - a.w) / (b.w - a.w) * (b.xyz - a.xyz);

    public static int3[] selfAndneighbourOffsets = new[]
    {
        new int3(0, 0, 0),
        new int3(1, 0, 0),
        new int3(0, 1, 0),
        new int3(0, 0, 1),
        new int3(-1, 0, 0),
        new int3(0, -1, 0),
        new int3(0, 0, -1),
    };

    public static int3[] neighbourOffsets = new[]
    {
        new int3(1, 0, 0),
        new int3(0, 1, 0),
        new int3(0, 0, 1),
        new int3(-1, 0, 0),
        new int3(0, -1, 0),
        new int3(0, 0, -1),
    };

    public static int3[] axises = new int3[]
    {
        new(1, 0, 0),  // +X
        new(-1, 0, 0), // -X
        new(0, 1, 0),  // +Y
        new(0, -1, 0), // -Y
        new(0, 0, 1),  // +Z
        new(0, 0, -1), // -Z
    };

    public static int3[] neighbourWithCornersAndEdgesOffsets = new[]
    {
        // Planes
        new int3(1, 0, 0),
        new int3(0, 1, 0),
        new int3(0, 0, 1),
        new int3(-1, 0, 0),
        new int3(0, -1, 0),
        new int3(0, 0, -1),

        // Edges
        new int3(0, 1, 1),
        new int3(0, -1, 1),
        new int3(0, 1, -1),
        new int3(0, -1, -1),
        new int3(1, 0, 1),
        new int3(1, 0, -1),
        new int3(-1, 0, 1),
        new int3(-1, 0, -1),
        new int3(1, 1, 0),
        new int3(-1, 1, 0),
        new int3(1, -1, 0),
        new int3(-1, 1, 0),

        // Corners
        new int3(1, 1, 1),
        new int3(-1, 1, 1),
        new int3(1, -1, 1),
        new int3(1, 1, -1),
        new int3(-1, -1, 1),
        new int3(1, -1, -1),
        new int3(-1, 1, -1),
        new int3(-1, -1, -1),
    };

    public static int3[] chunkSelfAndPositive = {
        new int3(0,0,0), new int3(1,0,0), new int3(0,1,0), new int3(1,1,0),
        new int3(0,0,1), new int3(1,0,1), new int3(0,1,1), new int3(1,1,1)
    };
    public static int3[] chunkSelfAndNegative = {
        new int3(0,0,0), new int3(-1,0,0), new int3(0,-1,0), new int3(-1,-1,0),
        new int3(0,0,-1), new int3(-1,0,-1), new int3(0,-1,-1), new int3(-1,-1,-1)
    };


    

    public static int GetKernelGroupsCount(int numthreads, int arrayCount)
    {
        return (arrayCount + numthreads - 1) / numthreads;
    }

    public static int2 GetKernelGroupsCount(int2 numthreads, int arrayCount)
    {
        int numGroupsX = Mathf.CeilToInt((float)arrayCount / numthreads.x);
        int numGroupsY = Mathf.CeilToInt((float)arrayCount / numthreads.y);

        return new(numGroupsX, numGroupsY);
    }

    public static int2 GetKernelGroupsCount(int2 numthreads, int2 arrayCount)
    {
        int numGroupsX = Mathf.CeilToInt((float)arrayCount.x / (float)numthreads.x);
        int numGroupsY = Mathf.CeilToInt((float)arrayCount.y / (float)numthreads.y);

        return new(numGroupsX, numGroupsY);
    }

    public static int3 GetKernelGroupsCount(int3 numthreads, int3 arrayCount)
    {
        int numGroupsX = Mathf.CeilToInt((float)arrayCount.x / (float)numthreads.x);
        int numGroupsY = Mathf.CeilToInt((float)arrayCount.y / (float)numthreads.y);
        int numGroupsZ = Mathf.CeilToInt((float)arrayCount.z / (float)numthreads.z);

        return new(numGroupsX, numGroupsY, numGroupsZ);
    }

    public static int GetAxialDistance(int3 a, int3 b)
    {
        return math.abs(a.x - b.x) + math.abs(a.y - b.y) + math.abs(a.z - b.z);
    }

    public static bool IsInside(int3 ask, int3 min, int3 max, bool includeMin, bool includeMax)
    {
        bool3 bmin, bmax;

        if (includeMin) bmin = ask >= min;
        else bmin = ask > min;

        if (includeMax) bmax = ask <= max;
        else bmax = ask < max;

        return bmin.x && bmin.y && bmin.z &&
               bmax.x && bmax.y && bmax.z;
    }

    public static bool IsInside(int2 ask, int2 min, int2 max, bool includeMin, bool includeMax)
    {
        bool2 bmin, bmax;

        if (includeMin) bmin = ask >= min;
        else bmin = ask > min;

        if (includeMax) bmax = ask <= max;
        else bmax = ask < max;

        return bmin.x && bmin.y &&
               bmax.x && bmax.y;
    }

    public static bool IsInside(float ask, float min, float max, bool includeMin, bool includeMax)
    {
        bool bmin, bmax;

        if (includeMin) bmin = ask >= min;
        else bmin = ask > min;

        if (includeMax) bmax = ask <= max;
        else bmax = ask < max;

        return bmin && bmax;
    }
    

    public static float NotNegative(float value)
    {
        if (value < 0) return 0;
        return value;
    }

    public static int NotNegative(int value)
    {
        if (value < 0) return 0;
        return value;
    }

    public static IEnumerable<int3> EnumeratePlane(PlaneEnum plane, int2 size, int axisDistance)
    {
        int indexA = (int) plane;
        int indexI = (indexA + 1) % 3;
        int indexJ = (indexA + 2) % 3;

        for (int i = 0; i < size.x; i++)
        for (int j = 0; j < size.y; j++)
        {
            int3 pos = default;
            pos[indexA] = axisDistance;
            pos[indexI] = i;
            pos[indexJ] = j;
            yield return pos;
        }
    }

    public static IEnumerable<(int3, int2)> EnumerateBlockFace(int3 axis, int size, bool useMin = true, int customMainSize = -1)
    {
        int main_axisIndex = GetAxisComponentIndex(axis);
        int main_axisSign = GetAxisComponentSign(axis);
        int u_axisIndex = (main_axisIndex + 1) % 3;
        int v_axisIndex = (main_axisIndex + 2) % 3;

        int d;
        if (useMin)
        {
            if (customMainSize == -1) customMainSize = size;
            d = main_axisSign == 1 ? customMainSize : 0;
        }
        else
        {
            int halfSize = size / 2;
            d = main_axisSign == 1 ? halfSize : -halfSize;
        }

        for (int u = 0; u < size; u++)
        for (int v = 0; v < size; v++)
        {
            int3 pos = int3.zero;
            pos[main_axisIndex] = d;
            pos[u_axisIndex] = u;
            pos[v_axisIndex] = v;
            yield return (pos, new int2(u, v));
        }
    }

    public static IEnumerable<int3> EnumerateFaces(int3 size, int extend, bool includeEdges)
    {
        size += extend * 2;

        int3 innerCubeMin = 1;
        int3 innerCubeMax = size - 2;

        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        for (int z = 0; z < size.z; z++)
        {
            int3 pos = new(x, y, z);

            if (IsInside(pos, innerCubeMin, innerCubeMax, true, true))
            {
                continue;
            }

            if (includeEdges == false)
            {
                bool3 flags = pos == 0;
                flags |= pos == size - 1;

                if (flags.Count(true) >= 2)
                {
                    continue;
                }
            }


            yield return pos - extend;
        }
    }

    public static float3 GetOddOffest(int3 size)
    {
        return ((float3)size % 2) / 2f;
    }

    public enum PlaneEnum
    {
        YZ,
        XZ,
        XY
    }

    public static int GetAxisComponentIndex(int3 axis)
    {
        if (math.abs(axis.x) > 0) return 0;
        if (math.abs(axis.y) > 0) return 1;
        if (math.abs(axis.z) > 0) return 2;
        throw new("Axis is zero");
    }

    public static int GetAxisComponentSign(int3 axis)
    {
        int index = GetAxisComponentIndex(axis);
        return math.sign(axis[index]);
    }

    public static IEnumerable<RaycastHit> RaycastAll(Ray ray, LayerMask mask = default)
    {
        if (mask == default) mask = -5;

        int count = Physics.RaycastNonAlloc(ray, hits, float.MaxValue, mask);
        Array.Sort(hits, 0, count, hitsComparer);
        for (int i = 0; i < count; i++)
        {
            yield return hits[i];
        }
    }

    private class RaycastHitDstComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            RaycastHit a = (RaycastHit) x;
            RaycastHit b = (RaycastHit) y;

            return a.distance.CompareTo(b.distance);
        }
    }

    
   

    public static PlaneEnum GetPlaneByNormal(int3 axis)
    {
        int3 absAxis = math.abs(axis);
        if (absAxis.x > 0) return PlaneEnum.YZ;
        if (absAxis.y > 0) return PlaneEnum.XZ;
        if (absAxis.z > 0) return PlaneEnum.XY;
        throw new ArgumentException();
    }

    public static int GetZeroComponentIndex(int3 axis)
    {
        if (axis.x == 0) return 0;
        if (axis.y == 0) return 1;
        if (axis.z == 0) return 2;
        throw new ArgumentException();
    }

    public static int GetNonZeroComponentsCount(int3 v)
    {
        int3 abs = math.abs(v);
        int c = 0;
        if (abs.x != 0) c++;
        if (abs.y != 0) c++;
        if (abs.z != 0) c++;
        return c;
    }

    

    public static float3 Average(float3[] array, int length)
    {
        float3 sum = 0;
        for (int i = 0; i < length; i++) sum += array[i];
        return sum / (float)length;
    }

    public static bool IsSameSign(float a, float b)
    {
        return GetBiSign(a) == GetBiSign(b);
    }

    public static int GetBiSign(float x)
    {
        if (x == 0.0) return 1;
        else return (x > 0.0 ? 1 : 0) - (x < 0.0 ? 1 : 0);
    }

    public static float3 Clamp(float3 vector, float maxLength)
    {
        float3 norm = math.normalize(vector);
        float length = math.length(vector);
        return math.min(maxLength, length) * norm;
    }

    public static float GetDistanceOnSurface(float3 posA_Normalized, float3 posB_Normalized, float radius)
    {
        // Скалярное произведение дает косинус угла
        // clamp нужен, чтобы избежать ошибок float (например, 1.000001)
        float dotProd = math.clamp(math.dot(posA_Normalized, posB_Normalized), -1f, 1f);

        // Арккосинус дает угол в радианах
        float angleRadians = math.acos(dotProd);

        // Длина дуги = угол * радиус
        return angleRadians * radius;
    }

    public static float GetAngleInDegrees(float3 posA, float3 posB)
    {
        float dot = math.clamp(math.dot(posA, posB), -1f, 1f);
        return math.degrees(math.acos(dot));
    }
    
    public static Matrix4x4 MakeProjectionMatrix(float fx, float fy, float cx, float cy, float width, float height, float near, float far)
    {
        // 1. Scale для осей X и Y (фокусное расстояние)
        float x0 = 2.0f * fx / width;
        float y1 = 2.0f * fy / height;

        // 2. Сдвиг центра (Principal Point)
        // Polycam cy идет сверху-вниз. Unity Projection матрица работает в Clip Space (Y-вверх).
        // Формула для NDC Y: 1 - 2 * (cy / height).
        // Если cy = height/2, то результат 0 (центр).
        // Если cy = 0 (верх), то результат 1 (верх NDC).

        float a0 = 1.0f - 2.0f * cx / width;
        float b1 = 1.0f - 2.0f * cy / height;

        Matrix4x4 m = Matrix4x4.zero;

        m[0, 0] = x0;
        m[0, 1] = 0;
        m[0, 2] = a0;  // Сдвиг по X
        m[0, 3] = 0;

        m[1, 0] = 0;
        m[1, 1] = y1;
        m[1, 2] = -b1;  // <--- ИСПРАВЛЕНИЕ: УБРАН МИНУС (было -b1)
        m[1, 3] = 0;

        // Стандартный OpenGL Z-range [-1, 1] для правильной работы GL.GetGPUProjectionMatrix
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = -(far + near) / (far - near);
        m[2, 3] = -(2.0f * far * near) / (far - near);

        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = -1.0f; // Для Perspective division (w = -z)
        m[3, 3] = 0;

        return m;
    }
}