Shader "Rubber/UI/Menu Villa Blur"
{
    Properties { _MainTex("Background",2D)="black"{} _Frost("White frost",Range(0,1))=.36 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float _Frost;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float3 c=tex2D(_MainTex,i.uv).rgb;
                float edge=smoothstep(.18,.7,length((i.uv-.5)*float2(1,.8)));
                float grain=frac(sin(dot(floor(i.uv*_ScreenParams.xy),float2(12.9898,78.233)))*43758.5453)-.5;
                c=lerp(c,float3(.94,.965,.98),saturate(_Frost+edge*.12));
                return fixed4(saturate(c+grain*.008),i.color.a);
            }
            ENDCG
        }
    }
}
