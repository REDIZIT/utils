using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace InGame.UI
{
    public class SubpixelLabelTest : CanvasComponent
    {
        public string text = "test-workspace | SectorID | Transform";
        public Color textColor = Color.white;
        public int fontSize = 13;
        
        // Путь к TTF файлу (можно переопределить в .ui)
        public string fontPath;

        private SubpixelFont subpixelFont;
        private Material subpixelMaterial;

        public override void OnAttached()
        {
            base.OnAttached();

            // Если путь не задан в .ui — берем стандартный системный шрифт Windows Segoe UI
            string targetPath = fontPath;
            if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
            {
                targetPath = "C:/Windows/Fonts/segoeui.ttf";
                if (!File.Exists(targetPath))
                {
                    targetPath = "C:/Windows/Fonts/arial.ttf";
                }
            }

            if (!File.Exists(targetPath))
            {
                Debug.LogError($"[SubpixelLabelTest] Шрифт не найден: {targetPath}");
                return;
            }

            byte[] fontBytes = File.ReadAllBytes(targetPath);

            subpixelFont?.Dispose();
            subpixelFont = new SubpixelFont(fontBytes, fontSize);

            var shader = Shader.Find("InGame/UI/SubpixelTest");
            if (shader != null)
            {
                subpixelMaterial = new Material(shader);
                subpixelMaterial.mainTexture = subpixelFont.AtlasTexture;
            }
        }

        public override void GenerateMesh(CanvasGenerationContext ctx)
        {
            if (subpixelFont == null || subpixelMaterial == null || string.IsNullOrEmpty(text))
                return;

            ctx.SetMaterial(subpixelMaterial);

            float penX = 0f;
            float baselineY = fontSize; // Базовая линия строки

            Matrix4x4 m = Element.LocalToRoot;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                var glyph = subpixelFont.GetGlyph(c);
                if (glyph == null) continue;
                
                if (glyph.width > 0 && glyph.height > 0)
                {
                    float x = Mathf.Round(penX + glyph.bearingX);
                    float y = Mathf.Round(baselineY + glyph.bearingY - glyph.height);

                    ctx.AppendQuad(
                        new float2(glyph.width, glyph.height),
                        float2.zero,
                        m * Matrix4x4.Translate(new Vector3(x, y, 0)),
                        textColor,
                        float4.zero,
                        glyph.uv
                    );
                }

                penX += glyph.advance;
            }

            subpixelFont.ApplyAtlasIfNeeded();
        }

        public override void OnDetached()
        {
            base.OnDetached();
            subpixelFont?.Dispose();
            subpixelFont = null;
        }
    }
}