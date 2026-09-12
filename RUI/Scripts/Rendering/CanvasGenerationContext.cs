using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class CanvasGenerationContext
    {
        public enum Layer { Background = 0, Content = 1, Text = 2, Overlay = 3 }

        // Словарь: Индекс слоя -> (Материал -> Батч)
        private readonly SortedDictionary<int, Dictionary<Material, DrawBatch>> layerBuckets = new();
        
        // Текущее смещение (устанавливается элементом при обходе дерева)
        public int currentLayerOffset = 0;
        private Layer currentSubLayer = Layer.Background;

        public void SetLayer(Layer subLayer)
        {
            currentSubLayer = subLayer;
        }

        public void SetMaterial(Material mat)
        {
            if (mat == null) return;

            // Вычисляем итоговый индекс: Офсет + Поднаряд (например 100 + 2 = 102)
            int finalLayerIndex = currentLayerOffset + (int)currentSubLayer;

            if (!layerBuckets.TryGetValue(finalLayerIndex, out var materialDict))
            {
                materialDict = new Dictionary<Material, DrawBatch>();
                layerBuckets[finalLayerIndex] = materialDict;
            }

            if (!materialDict.TryGetValue(mat, out currentBatch))
            {
                currentBatch = new DrawBatch { material = mat };
                materialDict[mat] = currentBatch;
            }
        }

        public void Clear()
        {
            foreach (var dict in layerBuckets.Values)
            {
                foreach (var b in dict.Values) b.Clear();
            }
            finalizedBatches.Clear();
            currentBatch = null;
            clipStack.Clear();
            currentLayerOffset = 0;
        }

        public void FinalizeBatches()
        {
            finalizedBatches.Clear();
            // Благодаря SortedDictionary, слои будут перебраны строго по порядку (0, 1, 2... 100, 101...)
            foreach (var dict in layerBuckets.Values)
            {
                foreach (var b in dict.Values)
                {
                    if (b.verts.Count > 0)
                    {
                        b.ApplyToMesh();
                        finalizedBatches.Add(b);
                    }
                }
            }
        }
        
        public class DrawBatch
        {
            public Material material;
            public readonly List<Vector3> verts = new List<Vector3>();
            public readonly List<int> tris = new List<int>();
            public readonly List<Vector2> uvs = new List<Vector2>();
            public readonly List<Vector2> uv1RectSizes = new List<Vector2>();
            public readonly List<Vector4> uv2Data = new List<Vector4>();
            public readonly List<Vector4> uv3ClipRects = new List<Vector4>();
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
                mesh.SetUVs(3, uv3ClipRects);
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
        
        public readonly List<DrawBatch> finalizedBatches = new();
        private DrawBatch currentBatch;
        private Layer currentLayer = Layer.Background;

        public static readonly Rect InfiniteClipRect = Rect.MinMaxRect(-100000f, -100000f, 100000f, 100000f);
        public readonly Stack<Rect> clipStack = new();

        public Rect CurrentClipRect => clipStack.Count > 0 ? clipStack.Peek() : InfiniteClipRect;

        public void PushClipRect(Rect newClip)
        {
            if (clipStack.Count > 0)
            {
	            Rect parentClip = clipStack.Peek();
                float minX = Mathf.Max(parentClip.xMin, newClip.xMin);
                float minY = Mathf.Max(parentClip.yMin, newClip.yMin);
                float maxX = Mathf.Min(parentClip.xMax, newClip.xMax);
                float maxY = Mathf.Min(parentClip.yMax, newClip.yMax);
                clipStack.Push(Rect.MinMaxRect(minX, minY, maxX, maxY));
            }
            else
            {
                clipStack.Push(newClip);
            }
        }

        public void PopClipRect()
        {
            if (clipStack.Count > 0) clipStack.Pop();
        }

        // Отрисовка квада
        public void AppendQuad(float2 pos, float2 size, Matrix4x4 matrix, Color color, float4 cornerRadii, float4 uvRect = default)
        {
            // if (currentBatch == null) return;

            int baseIndex = currentBatch.verts.Count;

            Vector3 p0 = matrix.MultiplyPoint3x4(new Vector3(pos.x, pos.y, 0));
            Vector3 p1 = matrix.MultiplyPoint3x4(new Vector3(pos.x + size.x, pos.y, 0));
            Vector3 p2 = matrix.MultiplyPoint3x4(new Vector3(pos.x, pos.y + size.y, 0));
            Vector3 p3 = matrix.MultiplyPoint3x4(new Vector3(pos.x + size.x, pos.y + size.y, 0));

            currentBatch.verts.Add(p0);
            currentBatch.verts.Add(p1);
            currentBatch.verts.Add(p2);
            currentBatch.verts.Add(p3);

            if (math.dot(uvRect, uvRect) <= 0.0001f)
            {
                currentBatch.uvs.Add(new Vector2(0, 0));
                currentBatch.uvs.Add(new Vector2(1, 0));
                currentBatch.uvs.Add(new Vector2(0, 1));
                currentBatch.uvs.Add(new Vector2(1, 1));
            }
            else
            {
                currentBatch.uvs.Add(new Vector2(uvRect.x, uvRect.y));
                currentBatch.uvs.Add(new Vector2(uvRect.z, uvRect.y));
                currentBatch.uvs.Add(new Vector2(uvRect.x, uvRect.w));
                currentBatch.uvs.Add(new Vector2(uvRect.z, uvRect.w));
            }

            Rect currentClip = CurrentClipRect;
            for (int i = 0; i < 4; i++)
            {
                currentBatch.uv1RectSizes.Add(new(size.x, size.y));
                currentBatch.uv2Data.Add(cornerRadii);
                currentBatch.uv3ClipRects.Add(new(currentClip.min.x, currentClip.min.y, currentClip.max.x, currentClip.max.y));
                currentBatch.colors.Add(color);
            }

            currentBatch.tris.Add(baseIndex + 0);
            currentBatch.tris.Add(baseIndex + 2);
            currentBatch.tris.Add(baseIndex + 1);

            currentBatch.tris.Add(baseIndex + 2);
            currentBatch.tris.Add(baseIndex + 3);
            currentBatch.tris.Add(baseIndex + 1);
        }

        public void AppendQuad(float2 size, Matrix4x4 matrix, Color color, float4 cornerRadii, float4 uvRect = default)
        {
            AppendQuad(float2.zero, size, matrix, color, cornerRadii, uvRect);
        }

        public void Dispose()
        {
	        foreach (DrawBatch b in finalizedBatches) b.Dispose();
            finalizedBatches.Clear();
        }
    }
}