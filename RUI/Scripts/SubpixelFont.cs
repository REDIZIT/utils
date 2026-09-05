using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace InGame.UI
{
    public class SubpixelFont : IDisposable
    {
        public Texture2D AtlasTexture { get; private set; }

        private IntPtr library;
        private IntPtr face;
        private byte[] fontData;

        public int FontSize { get; private set; }

        private readonly Dictionary<char, SubpixelGlyph> glyphs = new Dictionary<char, SubpixelGlyph>();

        private int atlasWidth = 512;
        private int atlasHeight = 512;
        private int cursorX = 2;
        private int cursorY = 2;
        private int rowHeight = 0;
        private Color32[] atlasPixels;
        private bool isAtlasDirty = false;

        public SubpixelFont(byte[] ttfBytes, int fontSize)
        {
            FontSize = fontSize;
            fontData = ttfBytes;

            int error = FreeTypeNative.FT_Init_FreeType(out library);
            if (error != 0) throw new Exception($"[FreeType] Init error: {error}");

            error = FreeTypeNative.FT_New_Memory_Face(library, fontData, (IntPtr)fontData.Length, IntPtr.Zero, out face);
            if (error != 0) throw new Exception($"[FreeType] Face error: {error}");

            FreeTypeNative.FT_Set_Pixel_Sizes(face, 0, (uint)fontSize);

            AtlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false)
            {
                name = "SubpixelFontAtlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            atlasPixels = new Color32[atlasWidth * atlasHeight];
            for (int i = 0; i < atlasPixels.Length; i++)
                atlasPixels[i] = new Color32(0, 0, 0, 255);

            AtlasTexture.SetPixels32(atlasPixels);
            AtlasTexture.Apply(false, false);
        }

        public SubpixelGlyph GetGlyph(char c)
        {
            if (glyphs.TryGetValue(c, out SubpixelGlyph glyph))
                return glyph;

            return RasterizeGlyph(c);
        }

        private SubpixelGlyph RasterizeGlyph(char c)
        {
            // 0x30000 включает настоящий 3-байтный LCD-растеризатор
            uint loadFlags = FreeTypeNative.FT_LOAD_RENDER | FreeTypeNative.FT_LOAD_TARGET_LCD;
            int err = FreeTypeNative.FT_Load_Char(face, (uint)c, loadFlags);
            if (err != 0) return null;

            IntPtr glyphSlotPtr = Marshal.ReadIntPtr(face, 120);
            if (glyphSlotPtr == IntPtr.Zero) return null;

            var slot = Marshal.PtrToStructure<FreeTypeNative.FT_GlyphSlotRec>(glyphSlotPtr);

            int pixelMode = slot.bitmap_pixel_mode;
            int rawWidth = (int)slot.bitmap_width;
            int pixelH = (int)slot.bitmap_rows;

            // В режиме LCD (pixelMode == 5) ширина втрое больше, так как содержит 3 субпикселя RGB
            int pixelW = (pixelMode == 5) ? (rawWidth / 3) : rawWidth;
            int advance = slot.advance_x >> 6;
            if (advance <= 0) advance = pixelW > 0 ? pixelW + 1 : FontSize / 3;

            // Пустой глиф или пробел
            if (pixelW <= 0 || pixelH <= 0 || slot.bitmap_buffer == IntPtr.Zero)
            {
                var empty = new SubpixelGlyph
                {
                    character = c, width = 0, height = 0, bearingX = 0, bearingY = 0,
                    advance = advance, uv = float4.zero
                };
                glyphs[c] = empty;
                return empty;
            }

            // Перенос каретки атласа
            if (cursorX + pixelW + 2 >= atlasWidth)
            {
                cursorX = 2;
                cursorY += rowHeight + 2;
                rowHeight = 0;
            }

            if (cursorY + pixelH + 2 >= atlasHeight)
            {
                Debug.LogError("[FreeType] Атлас переполнен!");
                return null;
            }

            int rawPitch = slot.bitmap_pitch;
            int absPitch = Math.Abs(rawPitch);
            int totalBytes = absPitch * pixelH;

            byte[] rawBuffer = new byte[totalBytes];
            Marshal.Copy(slot.bitmap_buffer, rawBuffer, 0, totalBytes);

            for (int y = 0; y < pixelH; y++)
            {
                int srcRow = (rawPitch > 0) ? (y * rawPitch) : ((pixelH - 1 - y) * absPitch);
                int dstRow = (cursorY + (pixelH - 1 - y)) * atlasWidth + cursorX;

                for (int x = 0; x < pixelW; x++)
                {
                    if (pixelMode == 5) // Истинный LCD режим (3 байта на пиксель)
                    {
                        int byteIndex = srcRow + x * 3;
                        if (byteIndex + 2 < totalBytes)
                        {
                            byte r = rawBuffer[byteIndex + 0];
                            byte g = rawBuffer[byteIndex + 1];
                            byte b = rawBuffer[byteIndex + 2];
                            atlasPixels[dstRow + x] = new Color32(r, g, b, 255);
                        }
                    }
                    else // Grayscale (1 байт на пиксель)
                    {
                        int byteIndex = srcRow + x;
                        if (byteIndex < totalBytes)
                        {
                            byte val = rawBuffer[byteIndex];
                            atlasPixels[dstRow + x] = new Color32(val, val, val, 255);
                        }
                    }
                }
            }

            rowHeight = Math.Max(rowHeight, pixelH);

            float uMin = (float)cursorX / atlasWidth;
            float vMin = (float)cursorY / atlasHeight;
            float uMax = (float)(cursorX + pixelW) / atlasWidth;
            float vMax = (float)(cursorY + pixelH) / atlasHeight;

            var newGlyph = new SubpixelGlyph
            {
                character = c,
                width = pixelW,
                height = pixelH,
                bearingX = slot.bitmap_left,
                bearingY = slot.bitmap_top,
                advance = advance,
                uv = new float4(uMin, vMin, uMax, vMax)
            };

            cursorX += pixelW + 2;
            glyphs[c] = newGlyph;
            isAtlasDirty = true;

            return newGlyph;
        }

        public void ApplyAtlasIfNeeded()
        {
            if (isAtlasDirty)
            {
                AtlasTexture.SetPixels32(atlasPixels);
                AtlasTexture.Apply(false, false);
                isAtlasDirty = false;
            }
        }

        public void Dispose()
        {
            if (face != IntPtr.Zero)
            {
                FreeTypeNative.FT_Done_Face(face);
                face = IntPtr.Zero;
            }

            if (library != IntPtr.Zero)
            {
                FreeTypeNative.FT_Done_FreeType(library);
                library = IntPtr.Zero;
            }

            if (AtlasTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(AtlasTexture);
                AtlasTexture = null;
            }
        }
    }
}