Shader "Effect/PostEffectBloom"
{
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
	CGINCLUDE
		#pragma shader_feature ELEMENTARY ADVANCED
		#include "UnityCG.cginc"
		#define blue_amount 1
		uniform sampler2D _MainTex;
		uniform half4 _MainTex_TexelSize;
		uniform half2 _ParamData;	// x模糊大小 y 初始采样亮度阈值	


		static const half curve[4] = { 0.0205, 0.0855, 0.232, 0.324};  
	
		struct v2f {
			half4 pos : SV_POSITION;
			half2 uv : TEXCOORD0;
		};
		struct v2f_blue 
		{
			float4 pos : SV_POSITION;
			half2 offs[7] : TEXCOORD0;//低级降采样到5次
		};


		

		v2f vert (appdata_img v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos (v.vertex);
			o.uv = v.texcoord ;
			return o; 
		}		
		fixed4 frag ( v2f i ) : COLOR
		{				
			fixed4 color = tex2D(_MainTex, i.uv);	
			return saturate(color - _ParamData.y);
		} 

		v2f_blue vertblurH (appdata_img v)
		{
			v2f_blue o;
			o.pos = UnityObjectToClipPos (v.vertex);
			#if ADVANCED
				half2 netFilterWidth = _MainTex_TexelSize.xy * half2(1.0, 0.0) * _ParamData.x; 
				o.offs[0] = v.texcoord;
				o.offs[1] = v.texcoord + netFilterWidth;
				o.offs[2] = v.texcoord + netFilterWidth*2.0;
				o.offs[3] = v.texcoord + netFilterWidth*3.0;
				o.offs[4] = v.texcoord - netFilterWidth;
				o.offs[5] = v.texcoord - netFilterWidth*2.0;
				o.offs[6] = v.texcoord - netFilterWidth*3.0;
			#elif ELEMENTARY
				half2 netFilterWidth = _MainTex_TexelSize.xy * _ParamData.x; 
				o.offs[0] = v.texcoord;
				o.offs[1] = v.texcoord + netFilterWidth * float2( blue_amount,  blue_amount);//右上
				o.offs[2] = v.texcoord + netFilterWidth * float2(-blue_amount,  blue_amount);//左上
				o.offs[3] = v.texcoord + netFilterWidth * float2(-blue_amount, -blue_amount);//左下
				o.offs[4] = v.texcoord + netFilterWidth * float2( blue_amount, -blue_amount);//右下
			#endif
			return o; 
		}		
		v2f_blue vertblurV (appdata_img v)
		{
			v2f_blue o;
			o.pos = UnityObjectToClipPos (v.vertex);
			#if ADVANCED
				half2 netFilterWidth = _MainTex_TexelSize.xy * half2(0.0, 1.0) * _ParamData.x;
				o.offs[0] = v.texcoord;
				o.offs[1] = v.texcoord + netFilterWidth;
				o.offs[2] = v.texcoord + netFilterWidth*2.0;
				o.offs[3] = v.texcoord + netFilterWidth*3.0;
				o.offs[4] = v.texcoord - netFilterWidth;
				o.offs[5] = v.texcoord - netFilterWidth*2.0;
				o.offs[6] = v.texcoord - netFilterWidth*3.0;
			#elif ELEMENTARY
				half2 netFilterWidth = _MainTex_TexelSize.xy * _ParamData.x; 
				o.offs[0] = v.texcoord;
				o.offs[1] = v.texcoord + netFilterWidth * float2( blue_amount, -blue_amount);//右下
				o.offs[2] = v.texcoord + netFilterWidth * float2(-blue_amount, -blue_amount);//左下
				o.offs[3] = v.texcoord + netFilterWidth * float2(-blue_amount,  blue_amount);//左上
				o.offs[4] = v.texcoord + netFilterWidth * float2( blue_amount,  blue_amount);//右上
			#endif
			return o; 
		}	
		fixed4 fragblur ( v2f_blue i ) : COLOR
		{
			fixed4 color = tex2D(_MainTex, i.offs[0]) * curve[3];
			#if ADVANCED
				color += tex2D(_MainTex, i.offs[1])*curve[2];
				color += tex2D(_MainTex, i.offs[2])*curve[1];
				color += tex2D(_MainTex, i.offs[3])*curve[0];
				color += tex2D(_MainTex, i.offs[4])*curve[2];
				color += tex2D(_MainTex, i.offs[5])*curve[1];
				color += tex2D(_MainTex, i.offs[6])*curve[0];
			#elif ELEMENTARY
				color += tex2D(_MainTex, i.offs[1])*curve[3];
				color += tex2D(_MainTex, i.offs[2])*curve[2];
				color += tex2D(_MainTex, i.offs[3])*curve[1];
				color += tex2D(_MainTex, i.offs[4])*curve[0];
			#endif
			return color;
		}	
	ENDCG
	
	SubShader 
	{
		ZTest Always  ZWrite Off Cull Off Blend Off

		Fog { Mode off } 
		//0  
		Pass 
		{ 
			CGPROGRAM	
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest 
			ENDCG	 
		}	
		//1	
		Pass 
		{	
			CGPROGRAM 
			#pragma vertex vertblurV
			#pragma fragment fragblur
			#pragma fragmentoption ARB_precision_hint_fastest 
			ENDCG
		}	
		//2
		Pass 
		{		
			CGPROGRAM
			#pragma vertex vertblurH
			#pragma fragment fragblur
			#pragma fragmentoption ARB_precision_hint_fastest 
			ENDCG
		}
	}	

	FallBack Off
}
