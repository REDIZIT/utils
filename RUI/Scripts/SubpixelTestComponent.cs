using Unity.Mathematics;
using UnityEngine;

namespace InGame.UI
{
    public class SubpixelTestComponent : CanvasComponent
    {
        public Color textColor = Color.white;
        public Material subpixelMaterial;

        private Texture2D maskTexture;

        public override void OnAttached()
        {
            base.OnAttached();

            // Создаем материал, если он не назначен
            if (subpixelMaterial == null)
            {
                var shader = Shader.Find("InGame/UI/SubpixelTest");
                if (shader != null)
                {
                    subpixelMaterial = new Material(shader);
                }
            }

            if (maskTexture == null)
            {
                GenerateFilteredLcdTexture();
            }

            if (subpixelMaterial != null)
            {
                subpixelMaterial.mainTexture = maskTexture;
            }

            // Фиксируем физический размер 1:1 (120х26 пикселей экрана)
            Transform.size = new float2(120f, 26f);
        }

        public override void GenerateMesh(CanvasGenerationContext ctx)
        {
            if (subpixelMaterial == null) return;

            ctx.SetMaterial(subpixelMaterial);
            ctx.AppendQuad(Transform.size, Transform.pivot, Element.LocalToRoot, textColor, float4.zero);
        }

        // Генерация субпиксельной маски с 5-точечной сверткой FreeType LCD Filter
        private void GenerateFilteredLcdTexture()
        {
            int pixelWidth = 120;
            int pixelHeight = 26;

            // Буфер сверхвысокого разрешения: 3 субпикселя на 1 экранный пиксель
            int subWidth = pixelWidth * 3;
            float[,] rawSubpixels = new float[subWidth, pixelHeight];

            // Рисуем тестовые буквы на 3x субпиксельной сетке:
            // "T"
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 10 * 3, 20, 10 * 3, 2); // шляпка T
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 14 * 3 + 1, 6, 3, 14); // ножка T (сдвиг 1/3 px!)

            // "E"
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 24 * 3, 6, 3, 16);     // вертикаль E
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 24 * 3, 20, 8 * 3, 2); // верх E
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 24 * 3, 13, 6 * 3, 2); // центр E
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 24 * 3, 6, 8 * 3, 2);  // низ E

            // "S"
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 36 * 3, 19, 8 * 3, 2);
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 36 * 3, 13, 3, 7);
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 36 * 3, 12, 8 * 3, 2);
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 43 * 3, 6, 3, 7);
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 36 * 3, 5, 8 * 3, 2);

            // "T" со сдвигом на 2/3 субпикселя
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 48 * 3, 20, 10 * 3, 2);
            DrawSubpixelRect(rawSubpixels, subWidth, pixelHeight, 52 * 3 + 2, 6, 3, 14); // ножка со сдвигом 2/3!

            // Применяем 5-точечный фильтр FreeType: [1/16, 4/16, 9/16, 4/16, 1/16]
            float[] filterWeights = new float[] { 0.0625f, 0.25f, 0.5625f, 0.25f, 0.0625f };
            float[,] filteredSubpixels = new float[subWidth, pixelHeight];

            for (int y = 0; y < pixelHeight; y++)
            {
                for (int s = 0; s < subWidth; s++)
                {
                    float sum = 0f;
                    for (int k = -2; k <= 2; k++)
                    {
                        int sampleX = Mathf.Clamp(s + k, 0, subWidth - 1);
                        sum += rawSubpixels[sampleX, y] * filterWeights[k + 2];
                    }
                    filteredSubpixels[s, y] = sum;
                }
            }

            // Упаковываем субпиксели в текстуру Texture2D RGBA32
            maskTexture = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false)
            {
                name = "SubpixelFilteredMask",
                filterMode = FilterMode.Point, // Point для чистоты пиксель-в-пиксель
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[pixelWidth * pixelHeight];

            for (int y = 0; y < pixelHeight; y++)
            {
                for (int x = 0; x < pixelWidth; x++)
                {
                    byte r = (byte)Mathf.Clamp(filteredSubpixels[x * 3 + 0, y] * 255f, 0f, 255f);
                    byte g = (byte)Mathf.Clamp(filteredSubpixels[x * 3 + 1, y] * 255f, 0f, 255f);
                    byte b = (byte)Mathf.Clamp(filteredSubpixels[x * 3 + 2, y] * 255f, 0f, 255f);

                    // Фон строго черный (0, 0, 0), маска в RGB
                    pixels[y * pixelWidth + x] = new Color32(r, g, b, 255);
                }
            }

            maskTexture.SetPixels32(pixels);
            maskTexture.Apply(false, false);
        }

        private void DrawSubpixelRect(float[,] buffer, int width, int height, int startX, int startY, int w, int h)
        {
            for (int y = startY; y < startY + h; y++)
            {
                if (y < 0 || y >= height) continue;
                for (int x = startX; x < startX + w; x++)
                {
                    if (x < 0 || x >= width) continue;
                    buffer[x, y] = 1f;
                }
            }
        }
    }
}