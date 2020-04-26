Shader "Effect/PostEffectDepthOfFiled"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
 
	CGINCLUDE
	#include "UnityCG.cginc"
	#define blue_amount 1
	#define blue_scale 10
	struct v2f_blur
	{
		float4 pos : SV_POSITION;
		float2 uv[5]  : TEXCOORD0;//0自身
	};
 
	struct v2f_dof
	{
		float4 pos : SV_POSITION;
		float2 uv  : TEXCOORD0;
		float2 uv1 : TEXCOORD1;
	};
 
	uniform sampler2D _MainTex;
	uniform half4 _MainTex_TexelSize;
	uniform half _DOF_Blue_Amount;
	
	v2f_blur vert_blur(appdata_img v)
	{
		v2f_blur o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv[0] = v.texcoord.xy;
		o.uv[1] = o.uv[0] + _DOF_Blue_Amount * _MainTex_TexelSize * float2( blue_amount,  blue_amount);//右上
		o.uv[2] = o.uv[0] + _DOF_Blue_Amount * _MainTex_TexelSize * float2(-blue_amount,  blue_amount);//左上
		o.uv[3] = o.uv[0] + _DOF_Blue_Amount * _MainTex_TexelSize * float2(-blue_amount, -blue_amount);//左下
		o.uv[4] = o.uv[0] + _DOF_Blue_Amount * _MainTex_TexelSize * float2( blue_amount, -blue_amount);//右下
 
		return o;
	}
 
	fixed4 frag_blur(v2f_blur i) : SV_Target
	{
		fixed4 col = fixed4(0,0,0,0);
		col += tex2D(_MainTex, i.uv[0]);
		col += tex2D(_MainTex, i.uv[1]);
		col += tex2D(_MainTex, i.uv[2]);
		col += tex2D(_MainTex, i.uv[3]);
		col += tex2D(_MainTex, i.uv[4]);
		return col*0.2;
	}
	ENDCG
	SubShader
	{
		//均值模糊
		Pass
		{
			ZTest Off
			Cull Off
			ZWrite Off
			Fog{ Mode Off }
 
			CGPROGRAM
			#pragma vertex vert_blur
			#pragma fragment frag_blur
			ENDCG
		}
	}
    Fallback Off  
}
