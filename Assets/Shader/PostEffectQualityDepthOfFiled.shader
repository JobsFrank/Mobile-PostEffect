Shader "Effect/PostEffectQualityDepthOfFiled"
{
	Properties
    {
        _MainTex ("", 2D) = "black"
    }
	
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

        Pass 
        {
            Name "dof blur"
            CGPROGRAM
                #pragma target 3.0
                #pragma vertex vert
                #pragma fragment farg_blur
				#pragma fragmentoption ARB_precision_hint_fastest 
            ENDCG
        }

        Pass 
        {
            Name "post filter"
            CGPROGRAM
                #pragma target 3.0
                #pragma vertex vert
                #pragma fragment farg_postblur
				#pragma fragmentoption ARB_precision_hint_fastest 
            ENDCG
        }
	}
	
	CGINCLUDE
	#include "UnityCG.cginc"
	static const int sample_num = 16;
	static const float2 blur_sample[sample_num] = 
	{
		float2(0,0),
		float2(0.54545456,0),
		float2(0.16855472,0.5187581),
		float2(-0.44128203,0.3206101),
		float2(-0.44128197,-0.3206102),
		float2(0.1685548,-0.5187581),
		float2(1,0),
		float2(0.809017,0.58778524),
		float2(0.30901697,0.95105654),
		float2(-0.30901703,0.9510565),
		float2(-0.80901706,0.5877852),
		float2(-1,0),
		float2(-0.80901694,-0.58778536),
		float2(-0.30901664,-0.9510566),
		float2(0.30901712,-0.9510565),
		float2(0.80901694,-0.5877853),
	};

	uniform sampler2D _CameraDepthTexture;
	uniform sampler2D _CoCTex;
	uniform sampler2D _MainTex;
	uniform half4 _MainTex_ST;
	uniform half4 _MainTex_TexelSize;
	uniform float _MaxCoC;
	uniform float _RcpAspect;
	uniform float _DOFBlur;

	struct v2f
	{
		float4 pos : SV_POSITION;
		half2 uv : TEXCOORD0;
		half2 uvAlt : TEXCOORD1;
	};
	v2f vert(appdata_img v)
	{
		half2 uvAlt = v.texcoord;
	#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0.0) uvAlt.y = 1.0 - uvAlt.y;
	#endif
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
	#if defined(UNITY_SINGLE_PASS_STEREO)
		o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
		o.uvAlt = UnityStereoScreenSpaceUVAdjust(uvAlt, _MainTex_ST);
	#else
		o.uv = v.texcoord;
		o.uvAlt = uvAlt;
	#endif

		return o;
	}
	
	half4 farg_blur(v2f v) : SV_Target
	{
		half3 blur = 0;
		UNITY_LOOP for (int i = 0; i < sample_num; i++)
		{
			half2 disp = blur_sample[i] * _MaxCoC * _DOFBlur;
			half2 duv = half2(disp.x * _RcpAspect, disp.y);
			half4 samp = tex2D(_MainTex, v.uv + duv);
			blur += samp.rgb;
		}
		blur /= sample_num;
		return half4(blur, 1);
	}

	half4 farg_postblur(v2f i) : SV_Target
	{
		const float4 duv = _MainTex_TexelSize.xyxy * float4(0.5, 0.5, -0.5, 0);
		half3 dof;
		dof  = tex2D(_MainTex, i.uv - duv.xy);
		dof += tex2D(_MainTex, i.uv - duv.zy);
		dof += tex2D(_MainTex, i.uv + duv.zy);
		dof += tex2D(_MainTex, i.uv + duv.xy);
		dof /= 4.0;
		dof = GammaToLinearSpace(dof);
		return half4(dof, 1);
	}
	ENDCG
}
