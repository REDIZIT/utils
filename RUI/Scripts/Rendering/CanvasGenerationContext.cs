using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace InGame.UI
{
    public class CanvasGenerationContext
    {
        public class DrawBatch
        {
            public Material material;
            public readonly List<Vector3> verts = new List<Vector3>();
            public readonly List<int> tris = new List<int>();
            public readonly List<Vector2> uvs = new List<Vector2>();
            public readonly List<Vector2> uv1RectSizes = new List<Vector2>();
            public readonly List<Vector4> uv2Data = new List<Vector4>();
            public readonly List<Vector4> uv3ClipRects = new List<Vector4>(); // Канал маски!
            public readonly List<Color> colors = new List<Color>();
            public Mesh mesh;

            public void Clear()
            {
                verts.Clear();
                tris.Clear();
                uvs.Clear();
                uv1RectSizes.Clear();
                uv2Data.Clear();
                uv3ClipRects.Clear();
                colors.Clear();
            }

            public void ApplyToMesh()
            {
                mesh ??= new Mesh { name = "BatchMesh" };
                mesh.Clear();
                mesh.SetVertices(verts);
                mesh.SetTriangles(tris, 0);
                mesh.SetUVs(0, uvs);
                mesh.SetUVs(1, uv1RectSizes);
                mesh.SetUVs(2, uv2Data);
                mesh.SetUVs(3, uv3ClipRects); // Передаем clipRect в TEXCOORD3
                mesh.SetColors(colors);
            }

            public void Dispose()
            {
                if (mesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                    mesh = null;
                }
            }
        }

        public readonly List<DrawBatch> batches = new List<DrawBatch>();
        public DrawBatch currentBatch;

        // По умолчанию маска бесконечная (отсечение отключено)
        public static readonly Vector4 InfiniteClipRect = new Vector4(-100000f, -100000f, 100000f, 100000f);
        public readonly Stack<Vector4> clipStack = new Stack<Vector4>();

        public Vector4 CurrentClipRect => clipStack.Count > 0 ? clipStack.Peek() : InfiniteClipRect;

        public void PushClipRect(Vector4 newClip)
        {
            if (clipStack.Count > 0)
            {
                Vector4 parentClip = clipStack.Peek();
                // Пересечение двух AABB прямоугольников:
                float minX = Mathf.Max(parentClip.x, newClip.x);
                float minY = Mathf.Max(parentClip.y, newClip.y);
                float maxX = Mathf.Min(parentClip.z, newClip.z);
                float maxY = Mathf.Min(parentClip.w, newClip.w);

                clipStack.Push(new Vector4(minX, minY, maxX, maxY));
            }
            else
            {
                clipStack.Push(newClip);
            }
        }

        public void PopClipRect()
        {
            if (clipStack.Count > 0)
            {
                clipStack.Pop();
            }
        }

        public void Clear()
        {
            for (int i = 0; i < batches.Count; i++)
                batches[i].Clear();
            currentBatch = null;
            clipStack.Clear();
        }

        public void SetMaterial(Material mat)
        {
            if (currentBatch != null && currentBatch.material == mat)
                return;

            currentBatch = null;
            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i].verts.Count == 0)
                {
                    currentBatch = batches[i];
                    currentBatch.material = mat;
                    break;
                }
            }

            if (currentBatch == null)
            {
                currentBatch = new DrawBatch { material = mat };
                batches.Add(currentBatch);
            }
        }

        public void AppendQuad(float2 size, float2 pivot, Matrix4x4 matrix, Color color, float4 cornerRadii, float4 uvRect = default)
		{
		    if (currentBatch == null)
		        throw new InvalidOperationException("Материал не был установлен перед вызовом AppendQuad!");

		    int baseIndex = currentBatch.verts.Count;

		    float xMin = -size.x * pivot.x;
		    float yMin = -size.y * pivot.y;
		    float xMax = xMin + size.x;
		    float yMax = yMin + size.y;

		    Vector3 p0 = matrix.MultiplyPoint3x4(new Vector3(xMin, yMin, 0));
		    Vector3 p1 = matrix.MultiplyPoint3x4(new Vector3(xMax, yMin, 0));
		    Vector3 p2 = matrix.MultiplyPoint3x4(new Vector3(xMin, yMax, 0));
		    Vector3 p3 = matrix.MultiplyPoint3x4(new Vector3(xMax, yMax, 0));

		    currentBatch.verts.Add(p0);
		    currentBatch.verts.Add(p1);
		    currentBatch.verts.Add(p2);
		    currentBatch.verts.Add(p3);

		    // Если кастомный UV не передан (нули), используем стандартный 0..1
		    if (math.dot(uvRect, uvRect) <= 0.0001f)
		    {
		        currentBatch.uvs.Add(new Vector2(0, 0));
		        currentBatch.uvs.Add(new Vector2(1, 0));
		        currentBatch.uvs.Add(new Vector2(0, 1));
		        currentBatch.uvs.Add(new Vector2(1, 1));
		    }
		    else
		    {
		        // Иначе берем точные координаты спрайта из атласа: (uMin, vMin, uMax, vMax)
		        currentBatch.uvs.Add(new Vector2(uvRect.x, uvRect.y));
		        currentBatch.uvs.Add(new Vector2(uvRect.z, uvRect.y));
		        currentBatch.uvs.Add(new Vector2(uvRect.x, uvRect.w));
		        currentBatch.uvs.Add(new Vector2(uvRect.z, uvRect.w));
		    }

		    Vector2 rectSize = new Vector2(size.x, size.y);
		    Vector4 radii = new Vector4(cornerRadii.x, cornerRadii.y, cornerRadii.z, cornerRadii.w);
		    Vector4 currentClip = CurrentClipRect;

		    for (int i = 0; i < 4; i++)
		    {
		        currentBatch.uv1RectSizes.Add(rectSize);
		        currentBatch.uv2Data.Add(radii);
		        currentBatch.uv3ClipRects.Add(currentClip);
		        currentBatch.colors.Add(color);
		    }

		    currentBatch.tris.Add(baseIndex + 0);
		    currentBatch.tris.Add(baseIndex + 2);
		    currentBatch.tris.Add(baseIndex + 1);

		    currentBatch.tris.Add(baseIndex + 2);
		    currentBatch.tris.Add(baseIndex + 3);
		    currentBatch.tris.Add(baseIndex + 1);
		}

        public void AppendTextGlyph(float2 pos, float2 size, float4 uvRect, Matrix4x4 matrix, Color color)
        {
            if (currentBatch == null)
                throw new InvalidOperationException("Материал не был установлен перед вызовом AppendTextGlyph!");

            int baseIndex = currentBatch.verts.Count;

            Vector3 p0 = matrix.MultiplyPoint3x4(new Vector3(pos.x, pos.y, 0));
            Vector3 p1 = matrix.MultiplyPoint3x4(new Vector3(pos.x + size.x, pos.y, 0));
            Vector3 p2 = matrix.MultiplyPoint3x4(new Vector3(pos.x, pos.y + size.y, 0));
            Vector3 p3 = matrix.MultiplyPoint3x4(new Vector3(pos.x + size.x, pos.y + size.y, 0));

            currentBatch.verts.Add(p0);
            currentBatch.verts.Add(p1);
            currentBatch.verts.Add(p2);
            currentBatch.verts.Add(p3);

            currentBatch.uvs.Add(new Vector2(uvRect.x, uvRect.y));
            currentBatch.uvs.Add(new Vector2(uvRect.z, uvRect.y));
            currentBatch.uvs.Add(new Vector2(uvRect.x, uvRect.w));
            currentBatch.uvs.Add(new Vector2(uvRect.z, uvRect.w));

            Vector2 rectSize = new Vector2(size.x, size.y);
            Vector4 textFlag = new Vector4(-1f, 0, 0, 0);
            Vector4 currentClip = CurrentClipRect;

            for (int i = 0; i < 4; i++)
            {
                currentBatch.uv1RectSizes.Add(rectSize);
                currentBatch.uv2Data.Add(textFlag);
                currentBatch.uv3ClipRects.Add(currentClip); // Записываем маску для текста!
                currentBatch.colors.Add(color);
            }

            currentBatch.tris.Add(baseIndex + 0);
            currentBatch.tris.Add(baseIndex + 2);
            currentBatch.tris.Add(baseIndex + 1);

            currentBatch.tris.Add(baseIndex + 2);
            currentBatch.tris.Add(baseIndex + 3);
            currentBatch.tris.Add(baseIndex + 1);
        }

        public void FinalizeBatches()
        {
            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i].verts.Count > 0)
                    batches[i].ApplyToMesh();
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < batches.Count; i++)
                batches[i].Dispose();
            batches.Clear();
        }
    }
}