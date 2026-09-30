using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace REDIZIT.RUI
{
    [ExecuteAlways]
    [RequireComponent(typeof(UnityEngine.CanvasRenderer))]
    public class FreeTypeText : MaskableGraphic
    {
        [TextArea(1, 16)]
        [SerializeField] private string text = "New FreeTypeText";
        [SerializeField] private string fontName = "Main";
        [SerializeField] private int fontSize = 10;
        [SerializeField] private FreeTypeHinting hinting = FreeTypeHinting.Light;
        [SerializeField] private TextAnchor alignment = TextAnchor.UpperLeft;
        [SerializeField] private TextWrapMode wrapMode = TextWrapMode.Wrap;
        [SerializeField] private float lineSpacing = 1.2f;

        private FreeTypeFont _font;
        private readonly List<FormattedGlyph> _cachedGlyphs = new(128);

        public string Text
        {
            get => text;
            set
            {
                if (text == value) return;
                text = value ?? string.Empty;
                SetVerticesDirty();
                RecalculatePreferredSize();
            }
        }

        public string FontName
        {
            get => fontName;
            set
            {
                if (fontName == value) return;
                fontName = value;
                Invalidate();
            }
        }

        public int FontSize
        {
            get => fontSize;
            set
            {
                if (fontSize == value) return;
                fontSize = Mathf.Max(6, value);
                Invalidate();
                RecalculatePreferredSize();
            }
        }

        public TextWrapMode WrapMode
        {
            get => wrapMode;
            set
            {
                if (wrapMode == value) return;
                wrapMode = value;
                SetVerticesDirty();
            }
        }

        public float LineSpacing
        {
            get => lineSpacing;
            set
            {
                if (Mathf.Approximately(lineSpacing, value)) return;
                lineSpacing = value;
                SetVerticesDirty();
            }
        }

        public TextAnchor Alignment
        {
            get => alignment;
            set
            {
                if (alignment == value) return;
                alignment = value;
                SetVerticesDirty();
            }
        }
        
        public float PreferredWidth { get; private set; }

        public override Texture mainTexture
        {
            get
            {
                EnsureFont();
                return _font != null ? _font.AtlasTexture : s_WhiteTexture;
            }
        }

        private void Invalidate()
        {
            _font = null;
            EnsureFont();
            SetVerticesDirty();
            SetMaterialDirty();
        }

        private void EnsureFont()
        {
            var manager = FreeTypeFontManager.Instance;
            if (manager == null) return;

            _font = manager.GetFont(fontName, fontSize, hinting);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            EnsureFont();
            if (_font == null || string.IsNullOrEmpty(text)) return;

            // 1. Вся компоновка выполняется единым движком TextEngine
            bool wrap = wrapMode == TextWrapMode.Wrap;
            TextEngine.LayoutMultiline(
                text,
                _font,
                rectTransform.rect,
                alignment,
                wrap,
                lineSpacing,
                _cachedGlyphs
            );

            if (_cachedGlyphs.Count == 0) return;

            // 2. Screen-Space Pixel Snapping включен ВСЕГДА
            float subpixelOffsetX = 0f;
            float subpixelOffsetY = 0f;

            Canvas c = canvas;
            if (c != null)
            {
                Camera cam = (c.renderMode == RenderMode.ScreenSpaceOverlay) ? null : c.worldCamera;
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position);
                float scale = c.scaleFactor > 0f ? c.scaleFactor : 1f;

                subpixelOffsetX = (screenPos.x - Mathf.Round(screenPos.x)) / scale;
                subpixelOffsetY = (screenPos.y - Mathf.Round(screenPos.y)) / scale;
            }

            Color32 vertexColor = color;

            // 3. Формирование квадов
            foreach (FormattedGlyph glyph in _cachedGlyphs)
            {
	            float x0 = Mathf.Round(glyph.position.x) - subpixelOffsetX;
	            float y0 = Mathf.Round(glyph.position.y) - subpixelOffsetY;
	            float x1 = x0 + glyph.size.x;
	            float y1 = y0 + glyph.size.y;

	            Rect uv = glyph.uvRect;

	            UIVertex v0 = new UIVertex { position = new(x0, y0, 0), color = vertexColor, uv0 = new(uv.xMin, uv.yMin) };
	            UIVertex v1 = new UIVertex { position = new(x0, y1, 0), color = vertexColor, uv0 = new(uv.xMin, uv.yMax) };
	            UIVertex v2 = new UIVertex { position = new(x1, y1, 0), color = vertexColor, uv0 = new(uv.xMax, uv.yMax) };
	            UIVertex v3 = new UIVertex { position = new(x1, y0, 0), color = vertexColor, uv0 = new(uv.xMax, uv.yMin) };

	            vh.AddUIVertexQuad(new[] { v0, v1, v2, v3 });
            }

            _font.ApplyAtlasIfNeeded();
        }

        private void RecalculatePreferredSize()
        {
	        EnsureFont();
	        if (_font == null || string.IsNullOrEmpty(text)) return;
	        
	        bool wrap = wrapMode == TextWrapMode.Wrap;
	        TextEngine.LayoutMultiline(
                text,
                _font,
                rectTransform.rect,
                alignment,
                wrap,
                lineSpacing,
                _cachedGlyphs
            );

            if (_cachedGlyphs.Count == 0) return;
            float subpixelOffsetX = 0f;

            Canvas c = canvas;
            if (c != null)
            {
                Camera cam = (c.renderMode == RenderMode.ScreenSpaceOverlay) ? null : c.worldCamera;
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position);
                float scale = c.scaleFactor > 0f ? c.scaleFactor : 1f;
                subpixelOffsetX = (screenPos.x - Mathf.Round(screenPos.x)) / scale;
            }

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            foreach (FormattedGlyph glyph in _cachedGlyphs)
            {
	            float x0 = Mathf.Round(glyph.position.x) - subpixelOffsetX;
	            float x1 = x0 + glyph.size.x;
	            minX = math.min(minX, x0);
	            maxX = math.max(maxX, x1);
            }

            PreferredWidth = maxX - minX;
        }
    }
}