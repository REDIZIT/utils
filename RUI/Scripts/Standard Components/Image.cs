using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace InGame.UI
{
	public class Image : CanvasComponent
	{
		public Color color = Color.white;
		public Material material;
		public float4 borderRadius = float4.zero;

		[Inject] public CanvasService canvasService;

		public override void OnAttached()
		{
			base.OnAttached();

			// Если материал не был задан явно в разметке — берем дефолтный из сервиса!
			if (material == null && canvasService != null)
			{
				material = canvasService.defaultCombinedMaterial;
			}
		}

		public void SetRadius(float radius)
		{
			borderRadius = new float4(radius, radius, radius, radius);
		}

		public override void GenerateMesh(CanvasGenerationContext ctx)
		{
			if (material == null) return;

			ctx.SetMaterial(material);
			ctx.AppendQuad(Transform.size, Element.LocalToRoot, color, borderRadius);
		}
	}
}