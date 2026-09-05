using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Sprites;
using Zenject;

namespace InGame.UI
{
    public class Image : CanvasComponent
    {
        public Color color = Color.white;
        public Material material;
        public float4 borderRadius = float4.zero;

        // Поле для спрайта:
        private Sprite internalSprite;
        private Material spriteMaterialInstance;

        [Inject] public CanvasService canvasService;

        public Sprite sprite
        {
            get => internalSprite;
            set
            {
                if (internalSprite == value) return;
                internalSprite = value;
                UpdateSpriteMaterial();
                MarkDirty();
            }
        }

        public override void OnAttached()
        {
            base.OnAttached();

            if (material == null && canvasService != null)
            {
                material = canvasService.defaultCombinedMaterial;
            }

            if (internalSprite != null)
            {
                UpdateSpriteMaterial();
            }
        }

        private void UpdateSpriteMaterial()
        {
            if (internalSprite == null || canvasService == null)
            {
                spriteMaterialInstance = null;
                return;
            }

            // Создаем легковесный инстанс материала под текстуру спрайта
            Material baseMat = material != null ? material : canvasService.defaultCombinedMaterial;
            if (baseMat != null)
            {
                spriteMaterialInstance = new Material(baseMat);
                spriteMaterialInstance.mainTexture = internalSprite.texture;
            }
        }

        public void SetRadius(float radius)
        {
            borderRadius = new float4(radius, radius, radius, radius);
            MarkDirty();
        }

        public override void GenerateMesh(CanvasGenerationContext ctx)
        {
            Material targetMat = spriteMaterialInstance != null ? spriteMaterialInstance : material;
            if (targetMat == null) return;

            ctx.SetMaterial(targetMat);

            float4 uvRect = float4.zero;
            if (internalSprite != null)
            {
                Vector4 outer = DataUtility.GetOuterUV(internalSprite);
                uvRect = new float4(outer.x, outer.y, outer.z, outer.w);
            }

            ctx.AppendQuad(Transform.size, Element.LocalToRoot, color, borderRadius, uvRect);
        }
    }
}