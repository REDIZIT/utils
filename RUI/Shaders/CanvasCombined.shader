Shader "REDIZIT/RUI/CanvasCombined"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane"
        }

        Cull Off 
        Lighting Off 
        ZWrite Off 
        ZTest Always 
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "UICombinedPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes 
            {
                float4 vertex          : POSITION;
                float4 color           : COLOR;
                float2 uv              : TEXCOORD0;
                float2 rectSize        : TEXCOORD1; // (Width, Height)
                float4 extra           : TEXCOORD2; // (TL, TR, BR, BL) или extra.x = -1 (Текст)
                float4 clipRect        : TEXCOORD3; // (minX, minY, maxX, maxY) в экранных пикселях
                float4 borderThickness : TEXCOORD4; // (Left, Bottom, Right, Top)
                float4 borderColor     : TEXCOORD5; // RGBA
            };

            struct Varyings 
            {
                float4 position        : SV_POSITION;
                half4 color            : COLOR;
                float2 uv              : TEXCOORD0;
                float2 rectSize        : TEXCOORD1;
                float4 extra           : TEXCOORD2;
                float4 clipRect        : TEXCOORD3;
                float2 screenPos       : TEXCOORD4; // Экранные координаты для маски
                float4 borderThickness : TEXCOORD5;
                half4 borderColor      : TEXCOORD6;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float sdRoundedBoxVarying(float2 p, float2 b, float4 r) 
            {
                float2 rad_pair = (p.x > 0.0) ? r.yz : r.xw;
                float final_rad = (p.y > 0.0) ? rad_pair.x : rad_pair.y;
                float2 q = abs(p) - b + final_rad;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - final_rad;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.vertex.xyz);
                output.color = input.color;
                output.uv = input.uv;
                output.rectSize = input.rectSize;
                output.extra = input.extra;
                output.clipRect = input.clipRect;
                output.screenPos = input.vertex.xy;
                output.borderThickness = input.borderThickness;
                output.borderColor = input.borderColor;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // --- 1. АНАЛИТИЧЕСКИЙ CLIP RECT (МАСКА) ---
                float2 dClip = min(input.screenPos - input.clipRect.xy, input.clipRect.zw - input.screenPos);
                float clipAlpha = saturate(min(dClip.x, dClip.y) + 0.5);

                if (clipAlpha <= 0.001)
                    discard;

                // --- 2. РЕЖИМ SDF ТЕКСТА ---
                if (input.extra.x < -0.5)
                {
                    float4 texSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    float dist = texSample.a;
                    float distPerPixel = fwidth(dist);
                    float scale = 1.0 / max(distPerPixel, 0.0001);
                    float weightOffset = -0.04; 
                    float alpha = saturate((dist - 0.5 + weightOffset) * scale + 0.5);

                    float4 col = input.color;
                    col.a *= alpha * clipAlpha;
                    return col;
                }

                float4 bt = input.borderThickness;
                bool hasBorder = (dot(bt, float4(1, 1, 1, 1)) > 0.001) && (input.borderColor.a > 0.001);
                bool hasRadius = dot(input.extra, float4(1, 1, 1, 1)) > 0.001;

                // --- 3. БЫСТРЫЙ ПУТЬ (ПРЯМОУГОЛЬНИК БЕЗ СКРУГЛЕНИЯ И БЕЗ ГРАНИЦЫ) ---
                if (!hasRadius && !hasBorder)
                {
                    float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    float4 col = tex * input.color;
                    col.a *= clipAlpha;
                    return col;
                }

                // --- 4. SDF ПУТЬ (СКРУГЛЕНИЕ И/ИЛИ ГРАНИЦА) ---
                float2 size = input.rectSize;
                float2 p = (input.uv - 0.5) * size;
                float aa = fwidth(length(p)) * 0.5;

                float2 outerHalfSize = size * 0.5;
                float distOuter = sdRoundedBoxVarying(p, outerHalfSize, input.extra);
                float shapeAlpha = 1.0 - smoothstep(-aa, aa, distOuter);

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float4 contentColor = texColor;

                if (hasBorder)
                {
                    // Смещение и размер внутреннего прямоугольника (L=x, B=y, R=z, T=w)
                    float2 shift = float2(
                        (bt.x - bt.z) * 0.5,
                        (bt.y - bt.w) * 0.5
                    );

                    float2 innerHalfSize = outerHalfSize - float2(
                        (bt.x + bt.z) * 0.5,
                        (bt.y + bt.w) * 0.5
                    );
                    innerHalfSize = max(innerHalfSize, 0.0);

                    // Уменьшение радиусов углов на толщину смежных сторон
                    float4 thicknessForRadii = float4(
                        max(bt.x, bt.w), // Top-Left: max(Left, Top)
                        max(bt.z, bt.w), // Top-Right: max(Right, Top)
                        max(bt.z, bt.y), // Bottom-Right: max(Right, Bottom)
                        max(bt.x, bt.y)  // Bottom-Left: max(Left, Bottom)
                    );
                    float4 innerRadii = max(0.0, input.extra - thicknessForRadii);

                    float distInner = sdRoundedBoxVarying(p - shift, innerHalfSize, innerRadii);
                    float fillMask = 1.0 - smoothstep(-aa, aa, distInner);

                    // Смешиваем цвет границы с заливкой (border снаружи, texColor внутри)
                    contentColor = lerp(input.borderColor, texColor, fillMask);
                }

                contentColor.a *= shapeAlpha * clipAlpha;
                return contentColor;
            }
            ENDHLSL
        }
    }
}