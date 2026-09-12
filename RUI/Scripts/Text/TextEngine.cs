using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore;

namespace REDIZIT.RUI
{
    public static class TextEngine
    {
	    public static bool enableDebugLogs = false;
	    
        // -------------------------------------------------------------
        // СУБПИКСЕЛЬНЫЙ FREETYPE ДВИЖОК
        // -------------------------------------------------------------

        public static float2 MeasureSubpixel(string text, SubpixelFont font)
        {
            if (string.IsNullOrEmpty(text) || font == null)
                return float2.zero;

            float totalWidth = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                var glyph = font.GetGlyph(text[i]);
                if (glyph != null)
                {
                    totalWidth += glyph.advance;
                }
            }

            return new float2(totalWidth, font.FontSize);
        }

        public static void LayoutSubpixel(
		    string text,
		    SubpixelFont font,
		    float2 containerSize,
		    TextAlignmentOptions alignment,
		    List<FormattedGlyph> outputGlyphs)
		{
		    outputGlyphs.Clear();
		    if (string.IsNullOrEmpty(text) || font == null) return;

		    float2 measuredSize = MeasureSubpixel(text, font);

		    // 1. Горизонтальное выравнивание
		    float cursorX = 0f;
		    if (alignment == TextAlignmentOptions.Center || alignment == TextAlignmentOptions.Midline)
		    {
		        cursorX = Mathf.Round((containerSize.x - measuredSize.x) * 0.5f);
		    }
		    else if (alignment == TextAlignmentOptions.Right || alignment == TextAlignmentOptions.MidlineRight)
		    {
		        cursorX = Mathf.Round(containerSize.x - measuredSize.x);
		    }

		    // 2. Вертикальное выравнивание
		    float boxHeight = containerSize.y > 0 ? containerSize.y : measuredSize.y;
		    float fontDescent = font.FontSize * 0.22f;
		    float baselineY = Mathf.Round((boxHeight - font.FontSize) * 0.5f + fontDescent);

		    if (enableDebugLogs && (text.StartsWith("Переименовать") || text.StartsWith("Создать") || text.Length < 15))
		    {
		        Debug.Log($"[TextEngine] '{text}' | FontSize: {font.FontSize} | containerSize.y: {containerSize.y:F1} | measuredSize.y: {measuredSize.y:F1} | baselineY: {baselineY:F1}");
		    }

		    for (int i = 0; i < text.Length; i++)
		    {
		        char c = text[i];
		        var glyph = font.GetGlyph(c);
		        if (glyph == null) continue;

		        if (glyph.width > 0 && glyph.height > 0)
		        {
		            float x = Mathf.Round(cursorX + glyph.bearingX);
		            float y = Mathf.Round(baselineY + glyph.bearingY - glyph.height);

		            // Логируем метрики первой буквы слова (например 'П' или 'С')
		            if (enableDebugLogs && i == 0 && (text.StartsWith("Переименовать") || text.StartsWith("Создать")))
		            {
		                Debug.Log($"[TextEngine] Первый глиф '{c}' | bearingY: {glyph.bearingY} | glyphH: {glyph.height} | bottom(y): {y:F1} | top(y+h): {(y + glyph.height):F1}");
		            }

		            outputGlyphs.Add(new FormattedGlyph
		            {
		                character = c,
		                position = new float2(x, y),
		                size = new float2(glyph.width, glyph.height),
		                uv = glyph.uv
		            });
		        }

		        cursorX += glyph.advance;
		    }
		}

        // -------------------------------------------------------------
        // СТАРЫЙ SDF ДВИЖОК (Сохраняем без изменений)
        // -------------------------------------------------------------

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
                cursorX = (containerSize.x - measuredSize.x) * 0.5f;
            else if (alignment == TextAlignmentOptions.Right || alignment == TextAlignmentOptions.MidlineRight)
                cursorX = containerSize.x - measuredSize.x;

            float baselineY = (containerSize.y - measuredSize.y) * 0.5f - font.faceInfo.descentLine * scale;

            float atlasWidth = font.atlasTexture.width;
            float atlasHeight = font.atlasTexture.height;

            float gradientScale = font.atlasPadding > 0 ? font.atlasPadding : 5f;
            float scaleRatio = scale * gradientScale;

            for (int i = 0; i < text.Length; i++)
            {
                uint unicode = text[i];
                if (!font.characterLookupTable.TryGetValue(unicode, out TMP_Character ch))
                    continue;

                Glyph glyph = ch.glyph;
                GlyphMetrics metrics = glyph.metrics;

                float charW = Mathf.Round(metrics.width * scale);
                float charH = Mathf.Round(metrics.height * scale);
                float bearingX = Mathf.Round(metrics.horizontalBearingX * scale);
                float bearingY = Mathf.Round(metrics.horizontalBearingY * scale);

                float glyphX = Mathf.Round(cursorX + bearingX);
                float glyphY = Mathf.Round(baselineY + bearingY - charH);

                GlyphRect glyphRect = glyph.glyphRect;
                float u0 = (float)glyphRect.x / atlasWidth;
                float v0 = (float)glyphRect.y / atlasHeight;
                float u1 = (float)(glyphRect.x + glyphRect.width) / atlasWidth;
                float v1 = (float)(glyphRect.y + glyphRect.height) / atlasHeight;

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