using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace REDIZIT.RUI
{
	public class Label : CanvasComponent, IMeasurable
    {
        private string internalText = "Label";
        private int internalFontSize = 13;
        private Color internalColor = Color.white;
        private TextAlignmentOptions internalAlignment = TextAlignmentOptions.Center;

        public readonly List<FormattedGlyph> cachedGlyphs = new List<FormattedGlyph>();

        public SubpixelFont subpixelFont;
        public Material fontMaterial;
        
        private string internalFontName = string.Empty;

        public CanvasService canvasService;
        public Assets assetDatabase;
        
        public string font
        {
	        get => internalFontName;
	        set
	        {
		        if (internalFontName == value) return;
		        internalFontName = value;
		        UpdateFont();
		        MarkDirty();
	        }
        }

        public string text
        {
            get => internalText;
            set
            {
                if (internalText == value) return;
                internalText = value;
                MarkDirty();
            }
        }

        public int fontSize
        {
            get => internalFontSize;
            set
            {
                if (internalFontSize == value) return;
                internalFontSize = value;
                UpdateFont();
                MarkDirty();
            }
        }

        public Color color
        {
            get => internalColor;
            set
            {
                if (internalColor == value) return;
                internalColor = value;
                MarkDirty();
            }
        }

        public TextAlignmentOptions alignment
        {
            get => internalAlignment;
            set
            {
                if (internalAlignment == value) return;
                internalAlignment = value;
                MarkDirty();
            }
        }

        public override void OnAttached()
        {
            base.OnAttached();
            UpdateFont();
        }

        private void UpdateFont()
        {
	        if (canvasService == null) return;

	        subpixelFont = canvasService.GetOrCreateSubpixelFont(internalFontName, internalFontSize, assetDatabase);

	        if (subpixelFont != null && canvasService.defaultSubpixelMaterial != null)
	        {
		        // БЕРЕМ ЕДИНЫЙ ОБЩИЙ МАТЕРИАЛ ИЗ КЭША СЕРВИСА:
		        fontMaterial = canvasService.GetOrCreateMaterial(
			        canvasService.defaultSubpixelMaterial, 
			        subpixelFont.AtlasTexture
		        );
	        }
        }
        
        public override float2 GetPreferredSize()
        {
	        if (subpixelFont != null) return TextEngine.MeasureSubpixel(text, subpixelFont);
	        return float2.zero;
        }

        public override void GenerateMesh(CanvasGenerationContext ctx, Matrix4x4 worldMatrix)
		{
			if (string.IsNullOrEmpty(text) || subpixelFont == null || fontMaterial == null)
				return;

			// --- 1. КУЛЛИНГ ВСЕЙ СТРОКИ ЦЕЛИКОМ ---
			Rect clip = ctx.CurrentClipRect;

			float2 size = Transform.size;
			Vector3 p0 = worldMatrix.MultiplyPoint3x4(new Vector3(0, 0, 0));
			Vector3 p1 = worldMatrix.MultiplyPoint3x4(new Vector3(size.x, 0, 0));
			Vector3 p2 = worldMatrix.MultiplyPoint3x4(new Vector3(0, size.y, 0));
			Vector3 p3 = worldMatrix.MultiplyPoint3x4(new Vector3(size.x, size.y, 0));

			float bMinX = Mathf.Min(Mathf.Min(p0.x, p1.x), Mathf.Min(p2.x, p3.x));
			float bMinY = Mathf.Min(Mathf.Min(p0.y, p1.y), Mathf.Min(p2.y, p3.y));
			float bMaxX = Mathf.Max(Mathf.Max(p0.x, p1.x), Mathf.Max(p2.x, p3.x));
			float bMaxY = Mathf.Max(Mathf.Max(p0.y, p1.y), Mathf.Max(p2.y, p3.y));

			// Если строка целиком вне текущей маски — не тратим время на LayoutSubpixel и цикл
			if (bMaxX < clip.min.x || bMinX > clip.max.x ||
			    bMaxY < clip.min.y || bMinY > clip.max.y)
			{
				return;
			}

			// Раскладываем глифы только для видимого текста
			TextEngine.LayoutSubpixel(text, subpixelFont, Transform.size, alignment, cachedGlyphs);

			ctx.SetLayer(CanvasGenerationContext.Layer.Text);
			ctx.SetMaterial(fontMaterial);

			// Быстрый путь: проверяем, нет ли вращения (в 99.9% UI элементы выровнены по осям)
			bool isAxisAligned = Mathf.Approximately(worldMatrix.m01, 0f) && Mathf.Approximately(worldMatrix.m10, 0f);

			// --- 2. ГЕНЕРАЦИЯ С КУЛЛИНГОМ ОТДЕЛЬНЫХ СИМВОЛОВ ---
			int glyphCount = cachedGlyphs.Count;
			for (int i = 0; i < glyphCount; i++)
			{
				var glyph = cachedGlyphs[i];

				float gMinX, gMinY, gMaxX, gMaxY;
				Matrix4x4 glyphMatrix;

				if (isAxisAligned)
				{
					// Быстрый путь без перемножения матриц:
					// Масштаб берется из диагонали m00 / m11, трансляция — из колонки m03 / m13
					float scaleX = worldMatrix.m00;
					float scaleY = worldMatrix.m11;
					float posX = worldMatrix.m03 + glyph.position.x * scaleX;
					float posY = worldMatrix.m13 + glyph.position.y * scaleY;

					gMinX = posX;
					gMinY = posY;
					gMaxX = posX + glyph.size.x * scaleX;
					gMaxY = posY + glyph.size.y * scaleY;

					// Строим матрицу квада прямой подстановкой трансляции (0 аллокаций, в разы быстрее Multiply)
					glyphMatrix = worldMatrix;
					glyphMatrix.m03 = posX;
					glyphMatrix.m13 = posY;
				}
				else
				{
					// Редкий путь: для элементов с поворотом (VisualTransform)
					Vector3 glyphWorldMin = worldMatrix.MultiplyPoint3x4(new Vector3(glyph.position.x, glyph.position.y, 0));
					Vector3 glyphWorldMax = worldMatrix.MultiplyPoint3x4(new Vector3(glyph.position.x + glyph.size.x, glyph.position.y + glyph.size.y, 0));

					gMinX = Mathf.Min(glyphWorldMin.x, glyphWorldMax.x);
					gMaxX = Mathf.Max(glyphWorldMin.x, glyphWorldMax.x);
					gMinY = Mathf.Min(glyphWorldMin.y, glyphWorldMax.y);
					gMaxY = Mathf.Max(glyphWorldMin.y, glyphWorldMax.y);

					glyphMatrix = worldMatrix * Matrix4x4.Translate(new Vector3(glyph.position.x, glyph.position.y, 0));
				}

				// Если конкретная буква целиком за пределами маски — пропускаем
				if (gMaxX < clip.min.x || gMinX > clip.max.x ||
				    gMaxY < clip.min.y || gMinY > clip.max.y)
				{
					continue;
				}

				ctx.AppendQuad(
					glyph.size,
					glyphMatrix,
					color,
					float4.zero,
					glyph.uv
				);
			}

			subpixelFont.ApplyAtlasIfNeeded();
		}

        public DesiredSize Measure(SizeConstraints constraints)
        {
	        return new(GetPreferredSize());
        }
    }
}