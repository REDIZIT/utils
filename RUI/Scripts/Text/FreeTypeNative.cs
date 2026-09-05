using System;
using System.Runtime.InteropServices;

namespace InGame.UI
{
    public static class FreeTypeNative
    {
        private const string DllName = "freetype";

        public const uint FT_LOAD_DEFAULT = 0x0;
        public const uint FT_LOAD_RENDER = 0x4;
        public const uint FT_LOAD_TARGET_LCD = 0x30000;
        public const int FT_RENDER_MODE_LCD = 3;
        
        public const int FT_LCD_FILTER_NONE = 0;
        public const int FT_LCD_FILTER_DEFAULT = 1; // <--- Каноничный сбалансированный фильтр ClearType!
        public const int FT_LCD_FILTER_LIGHT = 2;   // Мягкий фильтр

        // Жесткая разметка слота под Windows x64 (MSVC)
        [StructLayout(LayoutKind.Explicit, Size = 160)]
        public struct FT_GlyphSlotRec
        {
            [FieldOffset(0)] public IntPtr library;
            [FieldOffset(8)] public IntPtr face;
            [FieldOffset(16)] public IntPtr next;
            [FieldOffset(24)] public uint glyph_index;

            // metrics
            [FieldOffset(48)] public int metrics_width;
            [FieldOffset(52)] public int metrics_height;
            [FieldOffset(56)] public int metrics_horiBearingX;
            [FieldOffset(60)] public int metrics_horiBearingY;
            [FieldOffset(64)] public int metrics_horiAdvance;
            
            // advance (16.16 fixed-point)
            [FieldOffset(88)] public int advance_x;
            [FieldOffset(92)] public int advance_y;

            [FieldOffset(96)] public uint format;

            // bitmap
            [FieldOffset(104)] public uint bitmap_rows;
            [FieldOffset(108)] public uint bitmap_width;
            [FieldOffset(112)] public int bitmap_pitch;
            [FieldOffset(120)] public IntPtr bitmap_buffer;
            [FieldOffset(128)] public ushort bitmap_num_grays;
            [FieldOffset(130)] public byte bitmap_pixel_mode;

            // смещения отрисовки
            [FieldOffset(144)] public int bitmap_left;
            [FieldOffset(148)] public int bitmap_top;
        }

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Init_FreeType(out IntPtr alibrary);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Done_FreeType(IntPtr library);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_New_Memory_Face(IntPtr library, byte[] file_base, IntPtr file_size, IntPtr face_index, out IntPtr aface);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Done_Face(IntPtr face);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Set_Pixel_Sizes(IntPtr face, uint pixel_width, uint pixel_height);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Load_Char(IntPtr face, uint char_code, uint load_flags);
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Library_SetLcdFilter(IntPtr library, int filter);
    }
}