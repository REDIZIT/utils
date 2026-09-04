using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace InGame.UI
{
	public class Label : CanvasComponent
	{
		public string text = "Label";
		public TMP_FontAsset font;
		public float fontSize = 18f;
		public Color color = Color.white;
		public TextAlignmentOptions alignment = TextAlignmentOptions.Center;

		public readonly List<FormattedGlyph> cachedGlyphs = new List<FormattedGlyph>();
		public Material fontMaterial;

		[Inject] public CanvasService canvasService;

		public override void OnAttached()
		{
			base.OnAttached();

			// Если шрифт и материал не заданы в .ui файле — берем дефолтные
			if (canvasService != null)
			{
				if (font == null) font = canvasService.defaultFont;
				if (fontMaterial == null) fontMaterial = canvasService.defaultTextMaterial;
			}
		}

		public float2 GetPreferredSize()
		{
			return TextEngine.MeasureSingleLine(text, font, fontSize);
		}

		public override void GenerateMesh(CanvasGenerationContext ctx)
		{
			if (string.IsNullOrEmpty(text) || font == null || font.atlasTexture == null)
				return;

			TextEngine.LayoutSingleLine(text, font, fontSize, Transform.size, alignment, cachedGlyphs);

			Material targetMat = fontMaterial != null ? fontMaterial : font.material;
			ctx.SetMaterial(targetMat);

			Matrix4x4 localToRoot = Element.LocalToRoot;

			for (int i = 0; i < cachedGlyphs.Count; i++)
			{
				var glyph = cachedGlyphs[i];
				ctx.AppendTextGlyph(glyph.position, glyph.size, glyph.uv, localToRoot, color);
			}
		}
	}
}