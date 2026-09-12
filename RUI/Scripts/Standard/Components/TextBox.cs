using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class TextBox : CanvasComponent, IMeasurable
    {
        private string internalText = string.Empty;
        private int internalFontSize = 13;
        private Color internalColor = Color.white;
        private TextAlignmentOptions internalAlignment = TextAlignmentOptions.TopLeft;
        private float internalLineSpacing = 1.2f;
        private bool internalWrap = true;
        private string internalFontName = string.Empty;

        public readonly List<FormattedGlyph> cachedGlyphs = new List<FormattedGlyph>();

        public SubpixelFont subpixelFont;
        public Material fontMaterial;

        public CanvasService canvasService;
        public Assets assetDatabase;

        public string text
        {
            get => internalText;
            set
            {
                if (internalText == value) return;
                internalText = value ?? string.Empty;
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

        public float lineSpacing
        {
            get => internalLineSpacing;
            set
            {
                if (Mathf.Approximately(internalLineSpacing, value)) return;
                internalLineSpacing = value;
                MarkDirty();
            }
        }

        public bool wrap
        {
            get => internalWrap;
            set
            {
                if (internalWrap == value) return;
                internalWrap = value;
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
            canvasService ??= Element?.service;
            if (canvasService == null) return;

            assetDatabase ??= canvasService.assetDatabase;

            subpixelFont = canvasService.GetOrCreateSubpixelFont(internalFontName, internalFontSize, assetDatabase);

            if (subpixelFont != null && canvasService.defaultSubpixelMaterial != null)
            {
                fontMaterial = canvasService.GetOrCreateMaterial(
                    canvasService.defaultSubpixelMaterial, 
                    subpixelFont.AtlasTexture
                );
            }
        }

        public override float2 GetPreferredSize()
        {
            if (subpixelFont != null && !string.IsNullOrEmpty(text))
            {
                return TextEngine.MeasureMultilineSubpixel(text, subpixelFont, -1f, false, internalLineSpacing);
            }
            return float2.zero;
        }

        public DesiredSize Measure(SizeConstraints constraints)
        {
            if (subpixelFont == null || string.IsNullOrEmpty(text))
                return constraints.Clamp(new DesiredSize(0f, 0f));

            float availableWidth = -1f;
            if (wrap && constraints.x.TryGetMax(out float maxW))
            {
                availableWidth = maxW;
            }

            float2 size = TextEngine.MeasureMultilineSubpixel(
                text,
                subpixelFont,
                availableWidth,
                wrap,
                internalLineSpacing
            );

            return constraints.Clamp(new DesiredSize(size));
        }

        public override void GenerateMesh(CanvasGenerationContext ctx)
        {
            if (string.IsNullOrEmpty(text) || subpixelFont == null || fontMaterial == null)
                return;

            Rect clip = ctx.CurrentClipRect;
            Rect bounds = Element.GetScreenBounds();
            if (clip.Overlaps(bounds) == false) return;

            TextEngine.LayoutMultilineSubpixel(
                text,
                subpixelFont,
                Transform.size,
                alignment,
                wrap,
                lineSpacing,
                cachedGlyphs
            );

            ctx.SetLayer(CanvasGenerationContext.Layer.Text);
            ctx.SetMaterial(fontMaterial);

            Matrix4x4 localToRoot = Element.LocalToRoot;

            for (int i = 0; i < cachedGlyphs.Count; i++)
            {
                var glyph = cachedGlyphs[i];

                Vector3 glyphWorldMin = localToRoot.MultiplyPoint3x4(new Vector3(glyph.position.x, glyph.position.y, 0));
                Vector3 glyphWorldMax = localToRoot.MultiplyPoint3x4(new Vector3(glyph.position.x + glyph.size.x, glyph.position.y + glyph.size.y, 0));

                float gMinX = Mathf.Min(glyphWorldMin.x, glyphWorldMax.x);
                float gMaxX = Mathf.Max(glyphWorldMin.x, glyphWorldMax.x);
                float gMinY = Mathf.Min(glyphWorldMin.y, glyphWorldMax.y);
                float gMaxY = Mathf.Max(glyphWorldMin.y, glyphWorldMax.y);

                if (gMaxX < clip.min.x || gMinX > clip.max.x ||
                    gMaxY < clip.min.y || gMinY > clip.max.y)
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