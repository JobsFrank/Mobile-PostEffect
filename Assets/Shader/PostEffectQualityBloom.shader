Shader "Effect/PostEffectQualityBloom"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
	CGINCLUDE
	#include "UnityCG.cginc"
	#define HALF_MAX        65504.0
	uniform sampler2D _BaseTex;
	uniform sampler2D _MainTex;
	uniform half _Threshold;
	uniform half3 _Curve;
	uniform half2 _Bloom_Settings; // x: sampleScale, y: bloom.intensity
	uniform half4 _MainTex_ST;
	uniform half4 _MainTex_TexelSize;
	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 uvMain : TEXCOORD0;
		float2 uvBase : TEXCOORD1;
	};

	v2f vert(appdata_img i)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(i.vertex);
		o.uvMain = UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST);
		o.uvBase = o.uvMain;
	#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0.0)
			o.uvBase.y = 1.0 - o.uvBase.y;
	#endif
		return o;
	}
	half4 SafeHDR(half4 c) 
	{
		return min(c, HALF_MAX); 
	}
	half Brightness(half3 c)
	{
	    return max(max(c.r, c.g), c.b);
	}
	half4 EncodeHDR(float3 rgb)
	{
	    rgb *= 1.0 / 8.0;
	    float m = max(max(rgb.r, rgb.g), max(rgb.b, 1e-6));
	    m = ceil(m * 255.0) / 255.0;
	    return half4(rgb / m, m);
	}
	
	float3 DecodeHDR(half4 rgba)
	{
	    return rgba.rgb * rgba.a * 8.0;
	}
	
	// 3-tap median filter
	half3 Median(half3 a, half3 b, half3 c)
	{
	    return a + b + c - min(min(a, b), c) - max(max(a, b), c); 
	}
	// Downsample with a 4x4 box filter
	half3 DownsampleFilter(sampler2D tex, float2 uv, float2 texelSize)
	{
	    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);
	
	    half3 s;
	    s = DecodeHDR(tex2D(tex, uv + d.xy));
	    s += DecodeHDR(tex2D(tex, uv + d.zy));
	    s += DecodeHDR(tex2D(tex, uv + d.xw));
	    s += DecodeHDR(tex2D(tex, uv + d.zw));
	
	    return s * (1.0 / 4.0);
	}
	
	//mobile
	half3 UpsampleFilter(sampler2D tex, float2 uv, float2 texelSize, float sampleScale)
	{
	    // 4-tap bilinear upsampler
	    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);
	
	    half3 s;
	    s =  DecodeHDR(tex2D(tex, uv + d.xy));
	    s += DecodeHDR(tex2D(tex, uv + d.zy));
	    s += DecodeHDR(tex2D(tex, uv + d.xw));
	    s += DecodeHDR(tex2D(tex, uv + d.zw));
	
	    return s * (1.0 / 4.0);
	}
	half4 farg(v2f i) : SV_Target
	{
		half4 s0 = SafeHDR(tex2D(_MainTex, i.uvMain));
		half3 m = s0.rgb;
		m = GammaToLinearSpace(m);
		half br = Brightness(m);
		half rq = clamp(br - _Curve.x, 0.0, _Curve.y);
		rq = _Curve.z * rq * rq;
		m *= max(rq, br - _Threshold) / max(br, 1e-5);
		return EncodeHDR(m);
	} 
	half4 farg_downsample(v2f i) : SV_Target
	{
		return EncodeHDR(DownsampleFilter(_MainTex, i.uvMain, _MainTex_TexelSize.xy));
	}

	half4 farg_upsample(v2f i) : SV_Target
	{
		half3 base = DecodeHDR(tex2D(_BaseTex, i.uvBase));
		half3 blur = UpsampleFilter(_MainTex, i.uvMain, _MainTex_TexelSize.xy, _Bloom_Settings.x);
		return EncodeHDR(base + blur);
	}
    ENDCG
    SubShader
    {
        ZTest Always Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment farg
				#pragma fragmentoption ARB_precision_hint_fastest 
            ENDCG
        }
        Pass
        {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment farg_downsample
				#pragma fragmentoption ARB_precision_hint_fastest 
            ENDCG
        }
        Pass
        {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment farg_upsample
				#pragma fragmentoption ARB_precision_hint_fastest 
            ENDCG
        }
    }

    
}
