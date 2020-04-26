Shader "Effect/PostEffectLUT"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment farg
			//#pragma target 3.0
			#define ACEScc_MIDGRAY  0.4135884
			#define HALF_MAX        65504.0
			#define EPSILON         1.0e-4
			#include "UnityCG.cginc"

			struct ParamsLogC
			{
			    half cut;
			    half a, b, c, d, e, f;
			};
			
			static const ParamsLogC LogC =
			{
			    0.011361, // cut
			    5.555556, // a
			    0.047996, // b
			    0.244161, // c
			    0.386036, // d
			    5.301883, // e
			    0.092819  // f
			};
			struct v2f
			{
				float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
			};
			uniform half3 _Balance;
			uniform half _HueShift;
			uniform half _Saturation;
			uniform half _Contrast;
			uniform half4 _LutParams;

			static const half3x3 srgb_2_acse = {0.4397010, 0.3829780, 0.1773350,
												0.0897923, 0.8134230, 0.0967616,
												0.0175440, 0.1115440, 0.8707040};
			static const half3x3 acse_2_srgb = {1.70505, -0.62179, -0.08326,
												-0.13026, 1.14080, -0.01055,
												-0.02400, -0.12897, 1.15297,};
			static const half3x3 acse0_2_acse1 = {1.4514393161, -0.2365107469, -0.2149285693,
												  -0.0765537734,  1.1762296998, -0.0996759264,
												  0.0083161484, -0.0060324498,  0.9977163014};
			// 白平衡
			static const half3x3 line_2_lms = {3.90405e-1, 5.49941e-1, 8.92632e-3,
												  7.08416e-2, 9.63172e-1, 1.35775e-3,
												  2.31082e-2, 1.28021e-1, 9.36245e-1};

			half3 SRGB_TO_ACES(half3 col)
			{
				col = mul(srgb_2_acse, col);
				return col;
			}
			half3 ACES_TO_ACEScc(half3 col)
			{
				 col = clamp(col, 0.0, HALF_MAX);
				 return (col < 0.00003051757) ? (log2(0.00001525878 + col * 0.5) + 9.72) / 17.52 : (log2(col) + 9.72) / 17.52;
			}
			half3 Saturation(half3 col, half sat)
			{
			    half luma = dot(col, half3(0.2126, 0.7152, 0.0722));
			    return luma.xxx + sat * (col - luma.xxx);
			}
			half3 ContrastLog(half3 col, half con)
			{
			    return (col - ACEScc_MIDGRAY) * con + ACEScc_MIDGRAY;
			}
			half3 WhiteBalance(half3 col, half3 balance)
			{
			    half3 lms = mul(line_2_lms, col);
			    lms *= balance;
			    return mul(line_2_lms, lms);
			}
			half3 RgbToHsv(half3 c)
			{
			    half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			    half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
			    half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));
			    half d = q.x - min(q.w, q.y);
			    half e = EPSILON;
			    return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}
			
			half3 HsvToRgb(half3 c)
			{
			    half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
			    half3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
			    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
			}
			half RotateHue(half value, half low, half hi)
			{
			    return (value < low)? value + hi: (value > hi)? value - hi: value;
			}
			half3 LogCToLinear(half3 col)
			{
			    return (pow(10.0, (col - LogC.d) / LogC.c) - LogC.b) / LogC.a;
			}
			half acsecc_to_acse(half col)
			{
			    if (col < -0.3013698630) // (9.72 - 15) / 17.52
			        return (pow(2.0, col * 17.52 - 9.72) - pow(2.0, -16.0)) * 2.0;
			    else if (col < (log2(HALF_MAX) + 9.72) / 17.52)
			        return pow(2.0, col * 17.52 - 9.72);
			    else // (x >= (log2(HALF_MAX) + 9.72) / 17.52)
			        return HALF_MAX;
			}
			
			half3 ACEScc_TO_ACES(half3 col)
			{
			    return half3(acsecc_to_acse(col.r),acsecc_to_acse(col.g),acsecc_to_acse(col.b));
			}
			half3 ACES_TO_ACEScg(half3 col)
			{
			    return mul(acse0_2_acse1, col);
			}
			half3 ACES_TO_SRGB(half3 col)
			{
			    col = mul(acse_2_srgb, col);
			    return col;
			}
			half3 ColorGrade(half3 color)
			{
			    half3 aces = SRGB_TO_ACES(color);
			    half3 acescc = ACES_TO_ACEScc(aces);
			    acescc = Saturation(acescc, _Saturation);
			    acescc = ContrastLog(acescc, _Contrast);
			    aces = ACEScc_TO_ACES(acescc);
			    half3 acescg = ACES_TO_ACEScg(aces);
			    acescg = WhiteBalance(acescg, _Balance);
			    half3 hsv = RgbToHsv(max(acescg, 0.0));
			    hsv.x = RotateHue(hsv.x + _HueShift, 0.0, 1.0);
			    acescg = HsvToRgb(hsv);
			    color = ACES_TO_SRGB(acescg);
			    return color;
			}
			v2f vert (appdata_img i)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (i.vertex);
				o.uv = i.texcoord; 
				return o;
			}
			half4 farg(v2f i) : SV_Target
			{
			    half2 uv = i.uv - _LutParams.yz;
			    half3 color;
			    color.r = frac(uv.x * _LutParams.x);
			    color.b = uv.x - color.r / _LutParams.x;
			    color.g = uv.y;
			    half3 colorLogC = color * _LutParams.w;
			    half3 colorLinear = LogCToLinear(colorLogC);
			    half3 graded = ColorGrade(colorLinear);
			    return half4(graded, 1.0);
			}
			ENDCG
        }
    }

	Fallback Off
}
