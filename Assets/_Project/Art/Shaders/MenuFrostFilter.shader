Shader "Hidden/Rubber/Menu Frost Filter"
{
    Properties { _MainTex("Source",2D)="black"{} _Radius("Diffusion",Range(1,8))=2.5 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex; float4 _MainTex_TexelSize; float _Radius;
        fixed4 gaussian(float2 uv,float2 axis)
        {
            float2 d=abs(_MainTex_TexelSize.xy)*axis*_Radius;
            fixed4 c=tex2D(_MainTex,uv)*.227027;
            c+=(tex2D(_MainTex,uv+d*1.384615)+tex2D(_MainTex,uv-d*1.384615))*.316216;
            c+=(tex2D(_MainTex,uv+d*3.230769)+tex2D(_MainTex,uv-d*3.230769))*.070270;
            return c;
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment horizontal
            fixed4 horizontal(v2f_img i):SV_Target{return gaussian(i.uv,float2(1,0));}
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment vertical
            fixed4 vertical(v2f_img i):SV_Target{return gaussian(i.uv,float2(0,1));}
            ENDCG
        }
    }
}
