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
            public readonly List<Vector3> verts = new();
            public readonly List<int> tris = new();
            public readonly List<Vector2> uvs = new();
            public readonly List<Vector2> uv1RectSizes = new();
            public readonly List<Vector4> uv2Data = new(); // Для UI это Radii, для TMP это SDF Scale
            public readonly List<Color> colors = new();
            public Mesh mesh;

            public void Clear()
            {
                verts.Clear();
                tris.Clear();
                uvs.Clear();
                uv1RectSizes.Clear();
                uv2Data.Clear();
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

        public readonly List<DrawBatch> batches = new();
        public DrawBatch currentBatch;

        public void Clear()
        {
            for (int i = 0; i < batches.Count; i++)
                batches[i].Clear();
            currentBatch = null;
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

        // Отрисовка обычных квадов (кнопки, панели)
        public void AppendQuad(float2 size, Matrix4x4 matrix, Color color, float4 cornerRadii)
        {
            if (currentBatch == null)
                throw new InvalidOperationException("Материал не был установлен перед вызовом AppendQuad!");

            int baseIndex = currentBatch.verts.Count;

            Vector3 p0 = matrix.MultiplyPoint3x4(new Vector3(0, 0, 0));
            Vector3 p1 = matrix.MultiplyPoint3x4(new Vector3(size.x, 0, 0));
            Vector3 p2 = matrix.MultiplyPoint3x4(new Vector3(0, size.y, 0));
            Vector3 p3 = matrix.MultiplyPoint3x4(new Vector3(size.x, size.y, 0));

            currentBatch.verts.Add(p0);
            currentBatch.verts.Add(p1);
            currentBatch.verts.Add(p2);
            currentBatch.verts.Add(p3);

            currentBatch.uvs.Add(new Vector2(0, 0));
            currentBatch.uvs.Add(new Vector2(1, 0));
            currentBatch.uvs.Add(new Vector2(0, 1));
            currentBatch.uvs.Add(new Vector2(1, 1));

            Vector2 rectSize = new Vector2(size.x, size.y);
            Vector4 radii = new Vector4(cornerRadii.x, cornerRadii.y, cornerRadii.z, cornerRadii.w);

            for (int i = 0; i < 4; i++)
            {
                currentBatch.uv1RectSizes.Add(rectSize);
                currentBatch.uv2Data.Add(radii);
                currentBatch.colors.Add(color);
            }

            currentBatch.tris.Add(baseIndex + 0);
            currentBatch.tris.Add(baseIndex + 2);
            currentBatch.tris.Add(baseIndex + 1);

            currentBatch.tris.Add(baseIndex + 2);
            currentBatch.tris.Add(baseIndex + 3);
            currentBatch.tris.Add(baseIndex + 1);
        }

        // Отрисовка текста (SDF глифов)
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

	        // UV в атласе TMP
	        currentBatch.uvs.Add(new Vector2(uvRect.x, uvRect.y));
	        currentBatch.uvs.Add(new Vector2(uvRect.z, uvRect.y));
	        currentBatch.uvs.Add(new Vector2(uvRect.x, uvRect.w));
	        currentBatch.uvs.Add(new Vector2(uvRect.z, uvRect.w));

	        Vector2 rectSize = new Vector2(size.x, size.y);
    
	        // extra.x = -1f является флагом для шейдера: "ЭТО ТЕКСТ SDF"
	        Vector4 textFlag = new Vector4(-1f, 0, 0, 0);

	        for (int i = 0; i < 4; i++)
	        {
		        currentBatch.uv1RectSizes.Add(rectSize);
		        currentBatch.uv2Data.Add(textFlag);
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