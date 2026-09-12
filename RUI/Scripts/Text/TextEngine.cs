using System;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore;

namespace REDIZIT.RUI
{
    public struct TextLineInfo
    {
        public int start;
        public int length;
        public float lineWidth;
    }

    public static class TextAlignmentExtensions
    {
        public static bool IsRight(this TextAlignmentOptions alignment) =>
            alignment is TextAlignmentOptions.Right 
                or TextAlignmentOptions.TopRight 
                or TextAlignmentOptions.BottomRight 
                or TextAlignmentOptions.MidlineRight 
                or TextAlignmentOptions.BaselineRight;

        public static bool IsCenter(this TextAlignmentOptions alignment) =>
            alignment is TextAlignmentOptions.Center 
                or TextAlignmentOptions.Top 
                or TextAlignmentOptions.Bottom 
                or TextAlignmentOptions.Midline 
                or TextAlignmentOptions.Baseline;

        public static bool IsTop(this TextAlignmentOptions alignment) =>
            alignment is TextAlignmentOptions.TopLeft 
                or TextAlignmentOptions.Top 
                or TextAlignmentOptions.TopRight 
                or TextAlignmentOptions.TopJustified 
                or TextAlignmentOptions.TopFlush 
                or TextAlignmentOptions.TopGeoAligned;

        public static bool IsBottom(this TextAlignmentOptions alignment) =>
            alignment is TextAlignmentOptions.BottomLeft 
                or TextAlignmentOptions.Bottom 
                or TextAlignmentOptions.BottomRight 
                or TextAlignmentOptions.BottomJustified 
                or TextAlignmentOptions.BottomFlush 
                or TextAlignmentOptions.BottomGeoAligned;
    }

    public static class TextEngine
    {
        public static bool enableDebugLogs = false;

        // -------------------------------------------------------------
        // МНОГОСТРОЧНЫЙ СУБПИКСЕЛЬНЫЙ ДВИЖОК (ДЛЯ TEXTBOX)
        // -------------------------------------------------------------

        public static List<TextLineInfo> BreakLines(string text, SubpixelFont font, float maxWidth, bool wrap)
        {
            var lines = new List<TextLineInfo>();
            if (string.IsNullOrEmpty(text) || font == null) return lines;

            int textLen = text.Length;
            int paraStart = 0;

            while (paraStart < textLen)
            {
                // Находим конец текущего абзаца (по \n или \r)
                int paraEnd = paraStart;
                while (paraEnd < textLen && text[paraEnd] != '\n' && text[paraEnd] != '\r')
                {
                    paraEnd++;
                }

                // Обработка абзаца
                if (!wrap || maxWidth <= 0)
                {
                    float w = 0f;
                    for (int i = paraStart; i < paraEnd; i++)
                    {
                        var g = font.GetGlyph(text[i]);
                        if (g != null) w += g.advance;
                    }

                    lines.Add(new TextLineInfo
                    {
                        start = paraStart,
                        length = paraEnd - paraStart,
                        lineWidth = w
                    });
                }
                else
                {
                    int lineStart = paraStart;
                    float currentLineWidth = 0f;
                    int lastSpaceIndex = -1;
                    float widthAtLastSpace = 0f;

                    int i = paraStart;
                    while (i < paraEnd)
                    {
                        char c = text[i];
                        if (c == ' ' || c == '\t')
                        {
                            var g = font.GetGlyph(c == '\t' ? ' ' : c);
                            float adv = g != null ? g.advance : (font.FontSize * 0.25f);
                            if (c == '\t') adv *= 4;

                            if (currentLineWidth > 0)
                            {
                                lastSpaceIndex = i;
                                widthAtLastSpace = currentLineWidth;
                                currentLineWidth += adv;
                            }
                            else
                            {
                                // Пропускаем пробелы в начале новой перенесенной строки
                                lineStart = i + 1;
                            }
                            i++;
                        }
                        else
                        {
                            int wordStart = i;
                            float wordAdv = 0f;
                            while (i < paraEnd && text[i] != ' ' && text[i] != '\t')
                            {
                                var g = font.GetGlyph(text[i]);
                                wordAdv += g != null ? g.advance : 0;
                                i++;
                            }

                            if (currentLineWidth + wordAdv > maxWidth)
                            {
                                if (lastSpaceIndex > lineStart)
                                {
                                    // Перенос по последнему пробелу
                                    lines.Add(new TextLineInfo
                                    {
                                        start = lineStart,
                                        length = lastSpaceIndex - lineStart,
                                        lineWidth = widthAtLastSpace
                                    });

                                    lineStart = wordStart;
                                    currentLineWidth = wordAdv;
                                    lastSpaceIndex = -1;
                                    widthAtLastSpace = 0f;
                                }
                                else if (currentLineWidth > 0)
                                {
                                    lines.Add(new TextLineInfo
                                    {
                                        start = lineStart,
                                        length = wordStart - lineStart,
                                        lineWidth = currentLineWidth
                                    });

                                    lineStart = wordStart;
                                    currentLineWidth = wordAdv;
                                    lastSpaceIndex = -1;
                                    widthAtLastSpace = 0f;
                                }
                                else
                                {
                                    // Одно слово шире всей доступной строки: разбиваем по символам
                                    int charStart = wordStart;
                                    float charW = 0f;
                                    for (int ci = wordStart; ci < i; ci++)
                                    {
                                        var g = font.GetGlyph(text[ci]);
                                        float ca = g != null ? g.advance : 0;
                                        if (charW + ca > maxWidth && charW > 0)
                                        {
                                            lines.Add(new TextLineInfo
                                            {
                                                start = charStart,
                                                length = ci - charStart,
                                                lineWidth = charW
                                            });
                                            charStart = ci;
                                            charW = 0f;
                                        }
                                        charW += ca;
                                    }
                                    lineStart = charStart;
                                    currentLineWidth = charW;
                                    lastSpaceIndex = -1;
                                    widthAtLastSpace = 0f;
                                }
                            }
                            else
                            {
                                currentLineWidth += wordAdv;
                            }
                        }
                    }

                    if (paraEnd > lineStart)
                    {
                        int end = paraEnd;
                        while (end > lineStart && (text[end - 1] == ' ' || text[end - 1] == '\t'))
                            end--;

                        float finalW = (lastSpaceIndex >= end && widthAtLastSpace > 0) ? widthAtLastSpace : currentLineWidth;
                        lines.Add(new TextLineInfo
                        {
                            start = lineStart,
                            length = end - lineStart,
                            lineWidth = finalW
                        });
                    }
                    else if (paraEnd == paraStart)
                    {
                        lines.Add(new TextLineInfo { start = paraStart, length = 0, lineWidth = 0f });
                    }
                }

                // Переход к следующему абзацу (пропуск \r, \n)
                if (paraEnd < textLen && text[paraEnd] == '\r') paraEnd++;
                if (paraEnd < textLen && text[paraEnd] == '\n') paraEnd++;
                paraStart = paraEnd;
            }

            return lines;
        }

        public static float2 MeasureMultilineSubpixel(
            string text,
            SubpixelFont font,
            float maxWidth,
            bool wrap = true,
            float lineSpacing = 1.2f)
        {
            if (string.IsNullOrEmpty(text) || font == null)
                return float2.zero;

            List<TextLineInfo> lines = BreakLines(text, font, maxWidth, wrap);
            if (lines.Count == 0) return float2.zero;

            float maxLineWidth = 0f;
            for (int i = 0; i < lines.Count; i++)
            {
                maxLineWidth = Mathf.Max(maxLineWidth, lines[i].lineWidth);
            }

            float lineHeight = Mathf.Round(font.FontSize * lineSpacing);
            float totalHeight = lines.Count > 1 
                ? ((lines.Count - 1) * lineHeight + font.FontSize) 
                : font.FontSize;

            return new float2(maxLineWidth, totalHeight);
        }

        public static void LayoutMultilineSubpixel(
            string text,
            SubpixelFont font,
            float2 containerSize,
            TextAlignmentOptions alignment,
            bool wrap,
            float lineSpacing,
            List<FormattedGlyph> outputGlyphs)
        {
            outputGlyphs.Clear();
            if (string.IsNullOrEmpty(text) || font == null) return;

            float maxWidth = containerSize.x > 0 ? containerSize.x : -1f;
            List<TextLineInfo> lines = BreakLines(text, font, maxWidth, wrap);
            if (lines.Count == 0) return;

            float lineHeight = Mathf.Round(font.FontSize * lineSpacing);
            float totalTextHeight = lines.Count > 1 
                ? ((lines.Count - 1) * lineHeight + font.FontSize) 
                : font.FontSize;

            float boxHeight = containerSize.y > 0 ? containerSize.y : totalTextHeight;
            float boxWidth = containerSize.x > 0 ? containerSize.x : 0f;
            float fontDescent = font.FontSize * 0.22f;

            // Расчет Y базовой линии для верхней строки в Y-Up системе
            float topBaselineY;
            if (alignment.IsTop())
            {
                // Верхний край текста прижат к верху контейнера
                topBaselineY = Mathf.Round(boxHeight - (font.FontSize - fontDescent));
            }
            else if (alignment.IsBottom())
            {
                // Нижний край последней строки прижат к низу контейнера
                topBaselineY = Mathf.Round(fontDescent + (lines.Count - 1) * lineHeight);
            }
            else
            {
                // По центру высоты
                float startY = (boxHeight - totalTextHeight) * 0.5f;
                topBaselineY = Mathf.Round(startY + fontDescent + (lines.Count - 1) * lineHeight);
            }

            for (int li = 0; li < lines.Count; li++)
            {
                var line = lines[li];
                float lineBaselineY = topBaselineY - li * lineHeight;

                float cursorX = 0f;
                if (alignment.IsRight())
                {
                    cursorX = Mathf.Round(boxWidth - line.lineWidth);
                }
                else if (alignment.IsCenter())
                {
                    cursorX = Mathf.Round((boxWidth - line.lineWidth) * 0.5f);
                }

                for (int ci = line.start; ci < line.start + line.length; ci++)
                {
                    char c = text[ci];
                    var glyph = font.GetGlyph(c);
                    if (glyph == null) continue;

                    if (glyph.width > 0 && glyph.height > 0)
                    {
                        float x = Mathf.Round(cursorX + glyph.bearingX);
                        float y = Mathf.Round(lineBaselineY + glyph.bearingY - glyph.height);

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
        }

        // -------------------------------------------------------------
        // СУБПИКСЕЛЬНЫЙ ОДНОСТРОЧНЫЙ ДВИЖОК (ДЛЯ LABEL - БЕЗ ИЗМЕНЕНИЙ)
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

            float cursorX = 0f;
            if (alignment == TextAlignmentOptions.Center || alignment == TextAlignmentOptions.Midline)
            {
                cursorX = Mathf.Round((containerSize.x - measuredSize.x) * 0.5f);
            }
            else if (alignment == TextAlignmentOptions.Right || alignment == TextAlignmentOptions.MidlineRight)
            {
                cursorX = Mathf.Round(containerSize.x - measuredSize.x);
            }

            float boxHeight = containerSize.y > 0 ? containerSize.y : measuredSize.y;
            float fontDescent = font.FontSize * 0.22f;
            float baselineY = Mathf.Round((boxHeight - font.FontSize) * 0.5f + fontDescent);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                var glyph = font.GetGlyph(c);
                if (glyph == null) continue;

                if (glyph.width > 0 && glyph.height > 0)
                {
                    float x = Mathf.Round(cursorX + glyph.bearingX);
                    float y = Mathf.Round(baselineY + glyph.bearingY - glyph.height);

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
            if (string.IsNullOrEmpty(text) || font == null) return float2.zero;
            float scale = fontSize / font.faceInfo.pointSize;
            float totalWidth = 0f;

            for (int i = 0; i < text.Length; i++)
            {
                uint unicode = text[i];
                if (!font.characterLookupTable.TryGetValue(unicode, out TMP_Character ch)) continue;
                totalWidth += ch.glyph.metrics.horizontalAdvance * scale;
            }

            return new float2(totalWidth, font.faceInfo.lineHeight * scale);
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
                if (!font.characterLookupTable.TryGetValue(unicode, out TMP_Character ch)) continue;

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