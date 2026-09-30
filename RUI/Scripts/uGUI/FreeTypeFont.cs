using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class FreeTypeGlyph
    {
        public char character;
        public int width;
        public int height;
        public int bearingX;
        public int bearingY;
        public float advance; // Честный float для правильного кернинга
        public Rect uv;
    }

    public class FreeTypeFont : IDisposable
    {
        public Texture2D AtlasTexture { get; private set; }
        public int FontSize { get; private set; }
        public int HintingMode { get; private set; }

        private IntPtr _library;
        private IntPtr _face;
        private byte[] _fontData;
        private GCHandle _pinnedData;

        private readonly Dictionary<char, FreeTypeGlyph> _glyphs = new();

        private const int AtlasWidth = 1024;
        private const int AtlasHeight = 1024;
        private const int Padding = 2;

        private int _cursorX = Padding;
        private int _cursorY = Padding;
        private int _rowHeight = 0;
        private Color32[] _atlasPixels;
        private bool _isDirty = false;

        // Коррекция гаммы для устранения "жирности" сглаживания в Linear Color Space
        private const float TextGamma = 1.4f;

        public FreeTypeFont(byte[] fontBytes, int fontSize, int hintingMode)
        {
            FontSize = fontSize;
            HintingMode = hintingMode;
            _fontData = fontBytes;

            _pinnedData = GCHandle.Alloc(_fontData, GCHandleType.Pinned);

            int err = FreeTypeNative.FT_Init_FreeType(out _library);
            if (err != 0) throw new Exception($"[FreeType] Init failed: {err}");

            err = FreeTypeNative.FT_New_Memory_Face(_library, _fontData, (IntPtr)_fontData.Length, IntPtr.Zero, out _face);
            if (err != 0) throw new Exception($"[FreeType] New Face failed: {err}");

            FreeTypeNative.FT_Set_Pixel_Sizes(_face, 0, (uint)fontSize);

            // Создаем sRGB-текстуру (linear: false)
            AtlasTexture = new Texture2D(AtlasWidth, AtlasHeight, TextureFormat.RGBA32, false, false)
            {
                name = $"FreeType_Atlas_{fontSize}px",
                filterMode = FilterMode.Bilinear, // Bilinear дает мягкие субпиксельные переходы
                wrapMode = TextureWrapMode.Clamp
            };

            _atlasPixels = new Color32[AtlasWidth * AtlasHeight];
            for (int i = 0; i < _atlasPixels.Length; i++)
                _atlasPixels[i] = new Color32(255, 255, 255, 0);

            AtlasTexture.SetPixels32(_atlasPixels);
            AtlasTexture.Apply(false, false);
        }

        public FreeTypeGlyph GetGlyph(char c)
        {
            if (_glyphs.TryGetValue(c, out FreeTypeGlyph glyph))
                return glyph;

            return RasterizeGlyph(c);
        }

        private FreeTypeGlyph RasterizeGlyph(char c)
        {
            // FT_LOAD_RENDER = 0x4
            // HintingMode: 0 = Normal, 0x10000 = Light (FT_LOAD_TARGET_LIGHT)
            uint loadFlags = FreeTypeNative.FT_LOAD_RENDER | (uint)HintingMode;
            int err = FreeTypeNative.FT_Load_Char(_face, c, loadFlags);
            if (err != 0) return null;

            IntPtr slotPtr = Marshal.ReadIntPtr(_face, 120);
            if (slotPtr == IntPtr.Zero) return null;

            var slot = Marshal.PtrToStructure<FreeTypeNative.FT_GlyphSlotRec>(slotPtr);

            int w = (int)slot.bitmap_width;
            int h = (int)slot.bitmap_rows;

            // Точный расчет шага без потери долей пикселя
            float advance = slot.advance_x / 64.0f;
            if (advance <= 0) advance = w > 0 ? w + 1 : FontSize / 3.0f;

            if (w <= 0 || h <= 0 || slot.bitmap_buffer == IntPtr.Zero)
            {
                var emptyGlyph = new FreeTypeGlyph
                {
                    character = c, width = 0, height = 0, bearingX = 0, bearingY = 0,
                    advance = advance, uv = Rect.zero
                };
                _glyphs[c] = emptyGlyph;
                return emptyGlyph;
            }

            if (_cursorX + w + Padding >= AtlasWidth)
            {
                _cursorX = Padding;
                _cursorY += _rowHeight + Padding;
                _rowHeight = 0;
            }

            if (_cursorY + h + Padding >= AtlasHeight)
            {
                Debug.LogError("[FreeType] Атлас переполнен!");
                return null;
            }

            int pitch = slot.bitmap_pitch;
            int absPitch = Math.Abs(pitch);
            int totalBytes = absPitch * h;

            byte[] rawBuffer = new byte[totalBytes];
            Marshal.Copy(slot.bitmap_buffer, rawBuffer, 0, totalBytes);

            for (int y = 0; y < h; y++)
            {
                int srcRow = (pitch > 0) ? (y * pitch) : ((h - 1 - y) * absPitch);
                int dstRow = (_cursorY + (h - 1 - y)) * AtlasWidth + _cursorX;

                for (int x = 0; x < w; x++)
                {
                    float rawA = rawBuffer[srcRow + x] / 255.0f;
                    
                    // Гамма-коррекция для четкости контуров в темных темах
                    if (rawA > 0f && TextGamma != 1.0f)
                    {
                        rawA = Mathf.Pow(rawA, TextGamma);
                    }

                    byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(rawA * 255f), 0, 255);
                    _atlasPixels[dstRow + x] = new Color32(255, 255, 255, alpha);
                }
            }

            _rowHeight = Math.Max(_rowHeight, h);

            float uMin = (float)_cursorX / AtlasWidth;
            float vMin = (float)_cursorY / AtlasHeight;
            float uMax = (float)(_cursorX + w) / AtlasWidth;
            float vMax = (float)(_cursorY + h) / AtlasHeight;

            var newGlyph = new FreeTypeGlyph
            {
                character = c,
                width = w,
                height = h,
                bearingX = slot.bitmap_left,
                bearingY = slot.bitmap_top,
                advance = advance,
                uv = Rect.MinMaxRect(uMin, vMin, uMax, vMax)
            };

            _cursorX += w + Padding;
            _glyphs[c] = newGlyph;
            _isDirty = true;

            return newGlyph;
        }

        public void ApplyAtlasIfNeeded()
        {
            if (_isDirty)
            {
                AtlasTexture.SetPixels32(_atlasPixels);
                AtlasTexture.Apply(false, false);
                _isDirty = false;
            }
        }

        public void Dispose()
        {
            if (_face != IntPtr.Zero) FreeTypeNative.FT_Done_Face(_face);
            if (_library != IntPtr.Zero) FreeTypeNative.FT_Done_FreeType(_library);
            if (_pinnedData.IsAllocated) _pinnedData.Free();
            if (AtlasTexture != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(AtlasTexture);
                else UnityEngine.Object.DestroyImmediate(AtlasTexture);
            }
        }
    }
}