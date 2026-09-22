Shader "Hidden/Rubber/Interaction Outline Mask"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "InteractionMask"
            Cull Back
            ZWrite Off
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag() : SV_Target { return half4(1, 0, 0, 1); }
            ENDHLSL
        }

        Pass
        {
            Name "InteractionComposite"
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_InteractionOutlineMask);
            SAMPLER(sampler_InteractionOutlineMask);
            half4 _OutlineColor;
            float _OutlineWidthPixels;

            half SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_InteractionOutlineMask, sampler_InteractionOutlineMask, uv).r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 outlineRadius = rcp(_ScreenParams.xy) * _OutlineWidthPixels;
                half center = SampleMask(uv);
                half expanded = 0;
                bool wideOutline = _OutlineWidthPixels > 8.0;
                int outerSamples = wideOutline ? 32 : 8;
                float2 outerRotation = wideOutline
                    ? float2(0.98078528, 0.19509032)
                    : float2(0.70710678, 0.70710678);
                float2 direction = float2(1, 0);
                [loop]
                for (int i = 0; i < outerSamples; i++)
                {
                    expanded = max(expanded, SampleMask(uv + direction * outlineRadius));
                    direction = float2(direction.x * outerRotation.x - direction.y * outerRotation.y,
                                       direction.x * outerRotation.y + direction.y * outerRotation.x);
                }
                int innerSamples = wideOutline ? 16 : 4;
                float2 innerRotation = wideOutline
                    ? float2(0.92387953, 0.38268343)
                    : float2(0, 1);
                direction = float2(1, 0);
                [loop]
                for (int i = 0; i < innerSamples; i++)
                {
                    expanded = max(expanded, SampleMask(uv + direction * outlineRadius * 0.5));
                    direction = float2(direction.x * innerRotation.x - direction.y * innerRotation.y,
                                       direction.x * innerRotation.y + direction.y * innerRotation.x);
                }

                half edge = saturate(expanded - center);
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                sceneColor.rgb = lerp(sceneColor.rgb, _OutlineColor.rgb, edge * _OutlineColor.a);
                return sceneColor;
            }
            ENDHLSL
        }
    }
}
