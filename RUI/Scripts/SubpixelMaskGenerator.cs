using UnityEngine;
using UnityEngine.UI;

namespace InGame.UI
{
	public class SubpixelMaskGenerator : MonoBehaviour
	{
		[Header("Материал с шейдером SubpixelTest")]
		public Material testMaterial;

		public RawImage rawimage;

		[Header("Настройки")]
		public Color textColor = Color.white;

		private Texture2D generatedTexture;

		public void Start()
		{
			GenerateTestMask();
		}

		public void OnValidate()
		{
			if (Application.isPlaying && testMaterial != null)
			{
				testMaterial.SetColor("_TextColor", textColor);
			}
		}

		private void GenerateTestMask()
		{
			int width = 64;
			int height = 32;

			// Формат RGB24 или RGBA32, без сжатия!
			generatedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
			{
				name = "SubpixelTestMask",
				filterMode = FilterMode.Point, // Point для максимальной чистоты теста
				wrapMode = TextureWrapMode.Clamp
			};

			Color32[] pixels = new Color32[width * height];
			Color32 black = new Color32(0, 0, 0, 255); // 0 = фона нет, прозрачно

			// Заливаем фон нулями
			for (int i = 0; i < pixels.Length; i++) pixels[i] = black;

			// 1. Рисуем букву "I" (вертикальная линия со сглаженными LCD-субпикселями)
			// Полоса шириной 1 пиксель, но со сдвигом на 1/3 пикселя
			for (int y = 6; y < 26; y++)
			{
				// Пиксель слева: красный субпиксель выключен, зеленый и синий включены
				pixels[y * width + 10] = new Color32(0, 180, 255, 255);

				// Центральный пиксель: полоса покрывает все 3 субпикселя
				pixels[y * width + 11] = new Color32(255, 255, 255, 255);

				// Пиксель справа: красный включен, зеленый слабее, синий выключен
				pixels[y * width + 12] = new Color32(255, 120, 0, 255);
			}

			// 2. Рисуем горизонтальную перекладину (буква "T")
			for (int x = 6; x < 17; x++)
			{
				// Горизонтальные штрихи в LCD-рендере имеют одинаковое покрытие по R, G, B
				pixels[25 * width + x] = new Color32(255, 255, 255, 255);
				pixels[24 * width + x] = new Color32(180, 180, 180, 255);
			}

			// 3. Рисуем дугу буквы "C" (диагональные субпиксельные переходы)
			DrawSubpixelDot(pixels, width, 25, 16, new Color32(100, 200, 255, 255));
			DrawSubpixelDot(pixels, width, 26, 17, new Color32(255, 255, 200, 255));
			DrawSubpixelDot(pixels, width, 27, 18, new Color32(255, 150, 50, 255));

			generatedTexture.SetPixels32(pixels);
			generatedTexture.Apply(false, false);

			if (testMaterial != null)
			{
				testMaterial.mainTexture = generatedTexture;
				testMaterial.SetColor("_TextColor", textColor);
			}

			rawimage.material = testMaterial;
		}

		private void DrawSubpixelDot(Color32[] pixels, int width, int x, int y, Color32 color)
		{
			if (x >= 0 && x < width && y >= 0 && y < 32)
				pixels[y * width + x] = color;
		}

		private void OnDestroy()
		{
			if (generatedTexture != null)
			{
				Destroy(generatedTexture);
			}
		}
	}
}