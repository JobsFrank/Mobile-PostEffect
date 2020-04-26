// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Effect/PostEffectMotionBlur"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader 
	{
		ZTest Always 
        Cull Off 
        ZWrite Off
        CGINCLUDE
        #include "UnityCG.cginc"
        uniform sampler2D _MainTex;
        uniform fixed _BlurAmount;

        struct v2f {
            float4 pos : SV_POSITION;
            half2 uv : TEXCOORD0;
        };

        v2f vert(appdata_img v) {
            v2f o;
            // 顶点转换到剪裁空间
            o.pos = UnityObjectToClipPos(v.vertex);
            // uv坐标
            o.uv = v.texcoord;
            return o;
        }
        ENDCG
        // RGB 通道
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
            CGPROGRAM
            #pragma vertex vert  
            #pragma fragment fragRGB  
            fixed4 fragRGB (v2f i) : SV_Target 
			{
                return fixed4(tex2D(_MainTex, i.uv).rgb, _BlurAmount);
            }
            ENDCG
        }
        // A 通道
   //     Pass {   
   //         Blend One Zero
   //         ColorMask A
   //         CGPROGRAM  
   //         #pragma vertex vert  
   //         #pragma fragment fragA
   //         half4 fragA (v2f i) : SV_Target 
			//{
   //             return tex2D(_MainTex, i.uv);
   //         }
   //         ENDCG
   //     }
    }
    FallBack Off
}
