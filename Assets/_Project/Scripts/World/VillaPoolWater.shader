Shader "Rubber/Pool Water"
{
    Properties
    {
        _BaseColor("Shallow water tint",Color)=(.025,.28,.32,.34)
        _DeepColor("Deep water tint",Color)=(.008,.085,.13,1)
        _DepthDistance("Depth color distance (m)",Range(.2,4))=1.8
        _Density("Water absorption",Range(.1,2))=.8
        _WaveStrength("Ripple strength",Range(0,.1))=.035
        _WaveSpeed("Ripple speed",Range(0,2))=.65
        _RefractionPixels("Maximum refraction (pixels)",Range(0,8))=3
        _ReflectionStrength("Reflection strength",Range(0,1))=1
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Tags{"LightMode"="UniversalForwardOnly"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _DeepColor;
            float _DepthDistance, _Density, _WaveStrength, _WaveSpeed, _RefractionPixels;
            half _ReflectionStrength;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;};
            struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half fog:TEXCOORD1;};
            V Vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.positionCS.z);return o;}

            float3 SceneWorldPosition(float2 uv)
            {
                float raw=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    raw=lerp(UNITY_NEAR_CLIP_VALUE,1,raw);
                #endif
                return ComputeWorldSpacePosition(uv,raw,UNITY_MATRIX_I_VP);
            }

            float2 RippleSlope(float2 p,float t)
            {
                // Two long ripples and two small ripples cross at different speeds.
                float4 phase=float4(dot(p,float2(2.3,.9))+t*.62,
                    dot(p,float2(-1.1,3.1))-t*.47,
                    dot(p,float2(7.7,4.2))-t*.91,
                    dot(p,float2(-5.3,10.1))+t*1.13);
                // Suppress subpixel waves in distant/grazing views without another texture.
                float4 filtered=sin(phase)*exp2(-fwidth(phase)*fwidth(phase));
                float2 broad=float2(.93,.36)*filtered.x+float2(-.33,.94)*filtered.y*.7;
                float2 fine=float2(.88,.48)*filtered.z*.24+float2(-.46,.89)*filtered.w*.18;
                return (broad+fine)*_WaveStrength;
            }

            half4 Frag(V i):SV_Target
            {
                float2 slope=RippleSlope(i.world.xz,_Time.y*_WaveSpeed);
                half3 n=normalize(half3(slope.x,1,slope.y));
                half3 v=GetWorldSpaceNormalizeViewDir(i.world);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord=ComputeScreenPos(TransformWorldToHClip(i.world));
                #else
                    float4 shadowCoord=TransformWorldToShadowCoord(i.world);
                #endif
                Light light=GetMainLight(shadowCoord);

                float2 screenUV=GetNormalizedScreenSpaceUV(i.positionCS);
                float3 originalWorld=SceneWorldPosition(screenUV);
                // Vertical depth gives consistent color at different viewing angles.
                float depth=clamp(i.world.y-originalWorld.y,0,8);
                float shoreline=smoothstep(.015,.3,depth);
                float2 borderPixels=min(screenUV,1-screenUV)*_ScaledScreenParams.xy;
                float edgeFade=saturate(min(borderPixels.x,borderPixels.y)/8);
                float2 offset=clamp(slope/.035,-1,1)*_RefractionPixels*shoreline*edgeFade/_ScaledScreenParams.xy;
                float2 refractedUV=saturate(screenUV+offset);
                float3 refractedWorld=SceneWorldPosition(refractedUV);
                float surfaceEyeDepth=-TransformWorldToView(i.world).z;
                // Do not pull an above-water duck or a pool rim into the water image.
                if(-TransformWorldToView(refractedWorld).z<surfaceEyeDepth+.02 || refractedWorld.y>i.world.y-.01)
                {
                    refractedUV=screenUV;
                }
                half3 underneath=SampleSceneColor(refractedUV);
                half3 tint=lerp(_BaseColor.rgb,_DeepColor.rgb,smoothstep(0,max(.1,_DepthDistance),depth));
                half3 transmission=exp2(-depth*_Density*half3(.85,.36,.22));
                half3 color=tint*(.025+.85*_ReflectionStrength+light.color*.3*saturate(dot(n,light.direction))*light.shadowAttenuation);

                // Existing environment reflection and highlight only; no new reflection feature.
                half fresnel=.2+.65*pow(1-saturate(dot(n,v)),5);
                half3 reflection=GlossyEnvironmentReflection(reflect(-v,n),i.world,.08,1,screenUV)*_ReflectionStrength;
                half spec=pow(saturate(dot(n,SafeNormalize(light.direction+v))),180)*light.shadowAttenuation;
                color=lerp(color,reflection,fresnel)+light.color*spec*.65;
                half3 finalColor=underneath*transmission+MixFog(color,i.fog)*(1-transmission);
                return half4(finalColor,1);
            }
            ENDHLSL
        }
    }
}
