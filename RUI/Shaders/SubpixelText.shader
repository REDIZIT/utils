Shader "InGame/UI/SubpixelText"
{
    Properties
    {
        _MainTex ("Subpixel Mask (RGB)", 2D) = "white" {}
        _TextColor ("Text Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
        }

        Cull Off 
        Lighting Off 
        ZWrite Off 
        ZTest Always 

        // -------------------------------------------------------------
        // PASS 0: Вычитаем фон по маске: Framebuffer * (1 - mask)
        // -------------------------------------------------------------
        Pass
        {
            Name "Subpixel_MaskPass"
            Blend Zero OneMinusSrcColor

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes 
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
                float4 clipRect : TEXCOORD3; // Границы маски (minX, minY, maxX, maxY)
            };

            struct Varyings 
            {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
                float4 clipRect : TEXCOORD3;
                float2 screenPos: TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _TextColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.vertex.xyz);
                output.uv = input.uv;
                output.color = input.color * _TextColor;
                output.clipRect = input.clipRect;
                output.screenPos = input.vertex.xy;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Отсечение по границам маски (Clipping)
                float2 dClip = min(input.screenPos - input.clipRect.xy, input.clipRect.zw - input.screenPos);
                float clipAlpha = saturate(min(dClip.x, dClip.y) + 0.5);

                if (clipAlpha <= 0.001)
                    discard;

                float3 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                mask *= input.color.a * clipAlpha;

                return float4(mask, 1.0);
            }
            ENDHLSL
        }

        // -------------------------------------------------------------
        // PASS 1: Добавляем цвет текста: Framebuffer + (textColor * mask)
        // -------------------------------------------------------------
        Pass
        {
            Name "Subpixel_ColorPass"
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes 
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
                float4 clipRect : TEXCOORD3;
            };

            struct Varyings 
            {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
                float4 clipRect : TEXCOORD3;
                float2 screenPos: TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _TextColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.vertex.xyz);
                output.uv = input.uv;
                output.color = input.color * _TextColor;
                output.clipRect = input.clipRect;
                output.screenPos = input.vertex.xy;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 dClip = min(input.screenPos - input.clipRect.xy, input.clipRect.zw - input.screenPos);
                float clipAlpha = saturate(min(dClip.x, dClip.y) + 0.5);

                if (clipAlpha <= 0.001)
                    discard;

                float3 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                mask *= input.color.a * clipAlpha;

                return float4(input.color.rgb * mask, 1.0);
            }
            ENDHLSL
        }
    }
}