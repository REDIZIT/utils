using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Sprites;
using Zenject;

namespace REDIZIT.RUI
{
    public class Image : CanvasComponent
    {
        public Color color = Color.white;
        public Material material;
        public float4 borderRadius = float4.zero;

        private ImageMode internalMode = ImageMode.Simple;
        private float2 internalTileSize = float2.zero;

        private Sprite internalSprite;
        private Material spriteMaterialInstance;

        [Inject] public CanvasService canvasService;

        public ImageMode mode
        {
            get => internalMode;
            set
            {
                if (internalMode == value) return;
                internalMode = value;
                MarkDirty();
            }
        }

        // Если не задан (0, 0), берется размер самого спрайта/текстуры
        public float2 tileSize
        {
            get => internalTileSize;
            set
            {
                if (math.all(internalTileSize == value)) return;
                internalTileSize = value;
                MarkDirty();
            }
        }

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

            Material baseMat = material != null ? material : canvasService.defaultCombinedMaterial;
            if (baseMat != null)
            {
                spriteMaterialInstance = new Material(baseMat);
                spriteMaterialInstance.mainTexture = internalSprite.texture;
            }
        }

        public override float2 GetPreferredSize()
        {
            if (internalSprite != null)
            {
                return internalTileSize.x > 0 && internalTileSize.y > 0
                    ? internalTileSize
                    : new float2(internalSprite.rect.width, internalSprite.rect.height);
            }
            return float2.zero;
        }

        public override void GenerateMesh(CanvasGenerationContext ctx)
        {
            Material targetMat = spriteMaterialInstance != null ? spriteMaterialInstance : material;
            if (targetMat == null) return;

            float2 totalSize = Transform.size;
            if (totalSize.x <= 0 || totalSize.y <= 0) return;

            ctx.SetMaterial(targetMat);
            Matrix4x4 localToRoot = Element.LocalToRoot;

            if (mode == ImageMode.Simple)
            {
                float4 uvRect = float4.zero;
                if (internalSprite != null)
                {
                    Vector4 outer = DataUtility.GetOuterUV(internalSprite);
                    uvRect = new float4(outer.x, outer.y, outer.z, outer.w);
                }

                ctx.AppendQuad(totalSize, localToRoot, color, borderRadius, uvRect);
            }
            else if (mode == ImageMode.Tiling)
            {
                float tileW = internalTileSize.x;
                float tileH = internalTileSize.y;

                float uMin = 0f, vMin = 0f, uMax = 1f, vMax = 1f;

                if (internalSprite != null)
                {
                    if (tileW <= 0) tileW = internalSprite.rect.width;
                    if (tileH <= 0) tileH = internalSprite.rect.height;

                    Vector4 outer = DataUtility.GetOuterUV(internalSprite);
                    uMin = outer.x;
                    vMin = outer.y;
                    uMax = outer.z;
                    vMax = outer.w;
                }
                else if (targetMat.mainTexture != null)
                {
                    if (tileW <= 0) tileW = targetMat.mainTexture.width;
                    if (tileH <= 0) tileH = targetMat.mainTexture.height;
                }
                else
                {
                    // Без текстуры тайлинг цвета неотличим от 1 квада
                    ctx.AppendQuad(totalSize, localToRoot, color, borderRadius);
                    return;
                }

                // Защита от деления на ноль и зависания цикла
                if (tileW <= 0.001f || tileH <= 0.001f) return;

                // Генерация сетки тайлов снизу-вверх и слева-направо
                for (float y = 0; y < totalSize.y; y += tileH)
                {
                    float currentH = math.min(tileH, totalSize.y - y);
                    float vFrac = currentH / tileH;
                    float tileV0 = vMin;
                    float tileV1 = math.lerp(vMin, vMax, vFrac);

                    for (float x = 0; x < totalSize.x; x += tileW)
                    {
                        float currentW = math.min(tileW, totalSize.x - x);
                        float uFrac = currentW / tileW;
                        float tileU0 = uMin;
                        float tileU1 = math.lerp(uMin, uMax, uFrac);

                        float4 tileUv = new float4(tileU0, tileV0, tileU1, tileV1);
                        ctx.AppendQuad(new float2(x, y), new float2(currentW, currentH), localToRoot, color, float4.zero, tileUv);
                    }
                }
            }
        }
    }
}