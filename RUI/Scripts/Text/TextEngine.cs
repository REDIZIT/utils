using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore;

namespace InGame.UI
{
    public static class TextEngine
    {
        public static float2 MeasureSingleLine(string text, TMP_FontAsset font, float fontSize)
        {
            if (string.IsNullOrEmpty(text) || font == null)
                return float2.zero;

            float scale = fontSize / font.faceInfo.pointSize;
            float totalWidth = 0f;

            for (int i = 0; i < text.Length; i++)
            {
                uint unicode = text[i];
                if (!font.characterLookupTable.TryGetValue(unicode, out TMP_Character ch))
                    continue;

                totalWidth += ch.glyph.metrics.horizontalAdvance * scale;
            }

            float totalHeight = font.faceInfo.lineHeight * scale;
            return new float2(totalWidth, totalHeight);
        }

        public static void LayoutSingleLine(
            string text, 
            TMP_FontAsset font, 
            float fontSize, 
            float2 containerSize,
            TextAlignmentOptions alignment,
            List<FormattedGlyph> outputGlyphs)
        {
            outputGlyphs.Clear();
            if (string.IsNullOrEmpty(text) || font == null) return;

            float scale = fontSize / font.faceInfo.pointSize;
            float2 measuredSize = MeasureSingleLine(text, font, fontSize);

            float cursorX = 0f;
            if (alignment == TextAlignmentOptions.Center || alignment == TextAlignmentOptions.Midline)
            {
                cursorX = (containerSize.x - measuredSize.x) * 0.5f;
            }
            else if (alignment == TextAlignmentOptions.Right || alignment == TextAlignmentOptions.MidlineRight)
            {
                cursorX = containerSize.x - measuredSize.x;
            }

            float baselineY = (containerSize.y - measuredSize.y) * 0.5f - font.faceInfo.descentLine * scale;

            float atlasWidth = font.atlasTexture.width;
            float atlasHeight = font.atlasTexture.height;

            // Коэффициент резкости SDF для шейдера TMP
            // Показывает, сколько текселей атласа приходится на один экранный пиксель
            float gradientScale = font.atlasPadding > 0 ? font.atlasPadding : 5f;
            float scaleRatio = scale * gradientScale;

            for (int i = 0; i < text.Length; i++)
            {
                uint unicode = text[i];
                if (!font.characterLookupTable.TryGetValue(unicode, out TMP_Character ch))
                    continue;

                Glyph glyph = ch.glyph;
                GlyphMetrics metrics = glyph.metrics;

                float charW = metrics.width * scale;
                float charH = metrics.height * scale;
                float bearingX = metrics.horizontalBearingX * scale;
                float bearingY = metrics.horizontalBearingY * scale;
                
                GlyphRect glyphRect = glyph.glyphRect;
                float u0 = (float)glyphRect.x / atlasWidth;
                float v0 = (float)glyphRect.y / atlasHeight;
                float u1 = (float)(glyphRect.x + glyphRect.width) / atlasWidth;
                float v1 = (float)(glyphRect.y + glyphRect.height) / atlasHeight;

                float glyphX = cursorX + bearingX;
                float glyphY = baselineY + bearingY - charH;

				// ПИКСЕЛЬНЫЙ СНЭППИНГ: Округляем до целых экранных пикселей!
                glyphX = Mathf.Round(glyphX);
                glyphY = Mathf.Round(glyphY);
                charW = Mathf.Round(charW);
                charH = Mathf.Round(charH);

                outputGlyphs.Add(new FormattedGlyph
                {
	                character = text[i],
	                position = new float2(glyphX, glyphY),
	                size = new float2(charW, charH),
	                uv = new float4(u0, v0, u1, v1),
	                scaleRatio = scaleRatio
                });

                cursorX += metrics.horizontalAdvance * scale;
            }
        }
    }
}