using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace REDIZIT.RUI
{
    public class Label : CanvasComponent
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

        public override void GenerateMesh(CanvasGenerationContext ctx)
        {
	        if (string.IsNullOrEmpty(text) || subpixelFont == null || fontMaterial == null)
		        return;

	        // --- 1. БЫСТРЫЙ CPU КУЛЛИНГ ВСЕГО ЭЛЕМЕНТА ЦЕЛИКОМ ---
	        Vector4 clip = ctx.CurrentClipRect; // (minX, minY, maxX, maxY)
	        Vector4 bounds = Element.GetScreenBounds(); // (minX, minY, maxX, maxY)

	        // Если весь элемент целиком за пределами маски (выше, ниже, левее, правее)
	        if (bounds.z < clip.x || bounds.x > clip.z ||
	            bounds.w < clip.y || bounds.y > clip.w)
	        {
		        // Не тратим ресурсы CPU и не генерируем вершины для невидимого текста!
		        return;
	        }

	        // Раскладываем глифы
	        TextEngine.LayoutSubpixel(text, subpixelFont, Transform.calculatedSize, alignment, cachedGlyphs);

	        ctx.SetLayer(CanvasGenerationContext.Layer.Text);
	        ctx.SetMaterial(fontMaterial);

	        Matrix4x4 localToRoot = Element.LocalToRoot;

	        // --- 2. ГЕНЕРАЦИЯ С КУЛЛИНГОМ ОТДЕЛЬНЫХ СИМВОЛОВ ---
	        for (int i = 0; i < cachedGlyphs.Count; i++)
	        {
		        var glyph = cachedGlyphs[i];

		        // Экранные координаты конкретной буквы
		        Vector3 glyphWorldMin = localToRoot.MultiplyPoint3x4(new Vector3(glyph.position.x, glyph.position.y, 0));
		        Vector3 glyphWorldMax = localToRoot.MultiplyPoint3x4(new Vector3(glyph.position.x + glyph.size.x, glyph.position.y + glyph.size.y, 0));

		        float gMinX = Mathf.Min(glyphWorldMin.x, glyphWorldMax.x);
		        float gMaxX = Mathf.Max(glyphWorldMin.x, glyphWorldMax.x);
		        float gMinY = Mathf.Min(glyphWorldMin.y, glyphWorldMax.y);
		        float gMaxY = Mathf.Max(glyphWorldMin.y, glyphWorldMax.y);

		        // Если конкретная буква целиком вне маски — пропускаем квад
		        if (gMaxX < clip.x || gMinX > clip.z ||
		            gMaxY < clip.y || gMinY > clip.w)
		        {
			        continue;
		        }

		        ctx.AppendQuad(
			        glyph.size,
			        localToRoot * Matrix4x4.Translate(new Vector3(glyph.position.x, glyph.position.y, 0)),
			        color,
			        float4.zero,
			        glyph.uv
		        );
	        }

	        subpixelFont.ApplyAtlasIfNeeded();
        }
    }
}