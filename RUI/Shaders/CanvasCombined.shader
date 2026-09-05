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
                float4 extra    : TEXCOORD2; // (TL, TR, BR, BL) или extra.x = -1 (Текст)
                float4 clipRect : TEXCOORD3; // (minX, minY, maxX, maxY) в экранных пикселях
            };

            struct Varyings 
            {
                float4 position : SV_POSITION;
                half4 color     : COLOR;
                float2 uv       : TEXCOORD0;
                float2 rectSize : TEXCOORD1;
                float4 extra    : TEXCOORD2;
                float4 clipRect : TEXCOORD3;
                float2 screenPos : TEXCOORD4; // Координаты в экранных пикселях для маски
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
                
                // В нашем ортографическом холсте input.vertex.xy УЖЕ находятся в экранных пикселях!
                output.screenPos = input.vertex.xy;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // --- 1. АНАЛИТИЧЕСКИЙ CLIP RECT (МАСКА) ---
                // input.clipRect: xy = min (left, bottom), zw = max (right, top)
                float2 dClip = min(input.screenPos - input.clipRect.xy, input.clipRect.zw - input.screenPos);
                float clipAlpha = saturate(min(dClip.x, dClip.y) + 0.5);

                // Если пиксель за пределами маски — мгновенный discard
                if (clipAlpha <= 0.001)
                    discard;

                // --- 2. РЕЖИМ SDF ТЕКСТА ---
                if (input.extra.x < -0.5)
                {
                    float4 texSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    float dist = texSample.a;

                    // Вычисляем резкость границы по экранным пикселям
                    float distPerPixel = fwidth(dist);
                    
                    // Защита от деления на 0
                    float scale = 1.0 / max(distPerPixel, 0.0001);

                    // Смещение толщины: для мелких шрифтов слегка утончаем (-0.03 .. -0.05),
                    // чтобы отверстия внутри 'e', 'o', 'c' не заплывали!
                    float weightOffset = -0.04; 

                    // Расчет альфы с субпиксельным сглаживанием ровно в 1 пиксель
                    float alpha = saturate((dist - 0.5 + weightOffset) * scale + 0.5);

                    float4 col = input.color;
                    col.a *= alpha * clipAlpha;
                    return col;
                }

                // --- 3. ОБЫЧНЫЙ ПРЯМОУГОЛЬНИК БЕЗ СКРУГЛЕНИЯ ---
                if (dot(input.extra, float4(1, 1, 1, 1)) <= 0.001)
                {
                    float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    float4 col = tex * input.color;
                    col.a *= clipAlpha;
                    return col;
                }

                // --- 4. ПРЯМОУГОЛЬНИК СО СКРУГЛЕННЫМИ УГЛАМИ ---
                float2 size = input.rectSize;
                float2 p = (input.uv - 0.5) * size;
                float aa = fwidth(length(p)) * 0.5;

                float2 outerHalfSize = size * 0.5;
                float distOuter = sdRoundedBoxVarying(p, outerHalfSize, input.extra);
                float shapeAlpha = 1.0 - smoothstep(-aa, aa, distOuter);

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                texColor.a *= shapeAlpha * clipAlpha;

                return texColor;
            }
            ENDHLSL
        }
    }
}