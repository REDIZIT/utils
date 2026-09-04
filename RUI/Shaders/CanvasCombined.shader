Shader "InGame/UI/CanvasCombined"
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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float2 rectSize : TEXCOORD1; // (Width, Height)
                float4 extra    : TEXCOORD2; // Для UI: (TL, TR, BR, BL). Для Текста: extra.x = -1 (флаг текста)
            };

            struct Varyings 
            {
                float4 position : SV_POSITION;
                half4 color     : COLOR;
                float2 uv       : TEXCOORD0;
                float2 rectSize : TEXCOORD1;
                float4 extra    : TEXCOORD2;
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
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // -------------------------------------------------------------
                // РЕЖИМ 1: РЕНДЕРИНГ ТЕКСТА (SDF)
                // Если extra.x == -1.0, значит это символ текста
                // -------------------------------------------------------------
                if (input.extra.x < -0.5)
                {
                    float4 texSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    
                    // В TMP дистанция хранится в Альфа-канале (или в R для некоторых типов атласов)
                    float dist = texSample.a;

                    // Аналитическое субпиксельное сглаживание через fwidth
                    // fwidth(dist) дает точную скорость изменения дистанции на один пиксель экрана!
                    float delta = fwidth(dist);
                    float alpha = smoothstep(0.5 - delta, 0.5 + delta, dist);

                    float4 col = input.color;
                    col.a *= alpha;
                    return col;
                }

                // -------------------------------------------------------------
                // РЕЖИМ 2: РЕНДЕРИНГ ПРЯМОУГОЛЬНИКОВ / СКРУГЛЕННЫХ УГЛОВ
                // -------------------------------------------------------------
                if (dot(input.extra, float4(1, 1, 1, 1)) <= 0.001)
                {
                    float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    return tex * input.color;
                }

                float2 size = input.rectSize;
                float2 p = (input.uv - 0.5) * size;
                float aa = fwidth(length(p)) * 0.5;

                float2 outerHalfSize = size * 0.5;
                float distOuter = sdRoundedBoxVarying(p, outerHalfSize, input.extra);
                float shapeAlpha = 1.0 - smoothstep(-aa, aa, distOuter);

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                texColor.a *= shapeAlpha;

                return texColor;
            }
            ENDHLSL
        }
    }
}