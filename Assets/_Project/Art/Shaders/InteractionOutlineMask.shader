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
                float2 stepSize = rcp(_ScreenParams.xy) * _OutlineWidthPixels;
                half center = SampleMask(uv);
                half expanded = 0;
                expanded = max(expanded, SampleMask(uv + float2( stepSize.x, 0)));
                expanded = max(expanded, SampleMask(uv + float2(-stepSize.x, 0)));
                expanded = max(expanded, SampleMask(uv + float2(0,  stepSize.y)));
                expanded = max(expanded, SampleMask(uv + float2(0, -stepSize.y)));
                expanded = max(expanded, SampleMask(uv + float2( stepSize.x,  stepSize.y) * 0.7071));
                expanded = max(expanded, SampleMask(uv + float2(-stepSize.x,  stepSize.y) * 0.7071));
                expanded = max(expanded, SampleMask(uv + float2( stepSize.x, -stepSize.y) * 0.7071));
                expanded = max(expanded, SampleMask(uv + float2(-stepSize.x, -stepSize.y) * 0.7071));

                half edge = saturate(expanded - center);
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                sceneColor.rgb = lerp(sceneColor.rgb, _OutlineColor.rgb, edge * _OutlineColor.a);
                return sceneColor;
            }
            ENDHLSL
        }
    }
}
