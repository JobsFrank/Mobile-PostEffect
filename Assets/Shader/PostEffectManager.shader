Shader "Effect/PostEffectManager"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		ZTest Always Cull Off ZWrite Off
		Pass
		{
			CGPROGRAM
				#pragma multi_compile __ BLOOM 
				#pragma multi_compile __ DOF 
				#pragma multi_compile __ FXAA 
				#pragma multi_compile __ LUT 
				#pragma multi_compile __ MEANBLUR 
				#pragma multi_compile __ MOTIONBLUR 
				#pragma multi_compile __ RADIABLUR 
				#pragma multi_compile __ WINDSCREEN 
				#pragma multi_compile __ QUALITYBLOOM 
				#pragma multi_compile __ QUALITYDOF
				#pragma multi_compile __ MULTIPASS
				#pragma shader_feature CIRCLE HORIZONTAL VERTICAL
				#define blue_amount 10
				#include "UnityCG.cginc"
				#pragma vertex vert
				#pragma fragment farg

				uniform half4 _MainTex_ST;
				uniform half4 _MainTex_TexelSize;
				uniform sampler2D _BloomTex;
				uniform half4 _BloomColor;
				uniform half _BloomStrength;
				uniform sampler2D _MainTex;
				uniform sampler2D _DOFTex;
				uniform sampler2D _LogLut;
				uniform half3 _LogLut_Params;
				uniform half _ExposureEV;
				uniform sampler2D _MotionBlurTex;
				uniform sampler2D _QualityBloomTex;
				uniform half2 _Bloom_Settings;
				uniform sampler2D _QualityDOFTex;
				uniform sampler2D _CameraDepthTexture;
				uniform float _DOFDistance;
				uniform float _ClearDistance;
				uniform float _DOFRange;



				uniform half _FocusDistance;
				uniform half _NearBlurScale;
				uniform half _FarBlurScale;

				uniform half _Contrast;
				uniform half _Relative;
				uniform half _Subpixel;
				uniform half _BlurAmount;

				uniform half _SampleDist;
				uniform half _SampleStrength;

				uniform float _TimeStep;
				uniform float _ViewArea;
				uniform float _OpeningVal;
				uniform float _ViewAreaSmooth;
				uniform float4 _ScreenResolution;

				// redia blur some sample positions  
				static const float samples[6] =   
				{   
					-0.05,  
					-0.03,    
					-0.01,  
					0.01,    
					0.03,  
					0.05,  
				}; 


				struct v2f
				{
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
					float2 uvSPR : TEXCOORD1; // Single Pass Stereo UVs
					float2 uvFlipped : TEXCOORD2; // Flipped UVs (DX/MSAA/Forward)
					float2 uvFlippedSPR : TEXCOORD3; // Single Pass Stereo flipped UVs
					half2 offs[4]:TEXCOORD4;
				};

				//------------------------LUT------------------------------------
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
				half3 LinearToLogC(half3 x)
				{
				    return LogC.c * log10(LogC.a * x + LogC.b) + LogC.d;
				}
				half3 ApplyLut2d(sampler2D tex, half3 uvw, half3 scaleOffset)
				{
				    // Strip format where `height = sqrt(width)`
				    uvw.z *= scaleOffset.z;
				    half shift = floor(uvw.z);
				    uvw.xy = uvw.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
				    uvw.x += shift * scaleOffset.y;
				    uvw.xyz = lerp(tex2D(tex, uvw.xy).rgb, tex2D(tex, uvw.xy + half2(scaleOffset.y, 0)).rgb, uvw.z - shift);
				    return uvw;
				}
				//------------------------------------------------------------------------------------------------
				//------------------------FAXX------------------------------------
				struct LuminanceData
				{
					float m, n, e, s, w;
					float ne, nw, se, sw;
					float highest, lowest, contrast;
				};

				struct EdgeData 
				{
					bool isHorizontal;
					float pixelStep;
					float oppositeLuminance, gradient;
				};


				float SampleLuminance(float2 uv)
				{
					return LinearRgbToLuminance(saturate(tex2D(_MainTex, uv).rgb));
				}


				LuminanceData SampleLuminanceNeighborhood(float2 uv)
				{
					LuminanceData l;
					l.m = SampleLuminance(uv);
					l.n = SampleLuminance(uv + _MainTex_TexelSize.xy * float2(0, 1));
					l.s = SampleLuminance(uv + _MainTex_TexelSize.xy * float2(0, -1));
					l.e = SampleLuminance(uv + _MainTex_TexelSize.xy * float2(1, 0));
					l.w = SampleLuminance(uv + _MainTex_TexelSize.xy * float2(-1, 0));
					// 减少下面四次采样，会牺牲掉一点表现
					l.ne = SampleLuminance(uv + _MainTex_TexelSize.xy * float2(1, 1));
					l.nw = SampleLuminance(uv + _MainTex_TexelSize.xy * float2(-1, 1));
					l.se = SampleLuminance(uv + _MainTex_TexelSize.xy * float2(1, -1));
					l.sw = SampleLuminance(uv + _MainTex_TexelSize.xy * float2(-1, -1));
					l.highest = max(max(max(max(l.n, l.e), l.s), l.w), l.m);
					l.lowest = min(min(min(min(l.n, l.e), l.s), l.w), l.m);
					l.contrast = l.highest - l.lowest;
					return l;
				}


				bool ShouldSkipPixel(LuminanceData l)
				{
					float threshold = max(_Contrast, _Relative * l.highest);
					return l.contrast < threshold;
				}


				float DeterminePixelBlendFactor(LuminanceData l)
				{
					float filter = 2 * (l.n + l.e + l.s + l.w);
					filter += l.ne + l.nw + l.se + l.sw;
					filter *= 1.0 / 12;
					filter = abs(filter - l.m);
					filter = saturate(filter / max(0.0001, l.contrast));
					float blendFactor = smoothstep(0, 1, filter);
					return blendFactor * blendFactor * _Subpixel;
				}
				
				
				EdgeData DetermineEdge(LuminanceData l)
				{
					EdgeData e;
					float horizontal =
						abs(l.n + l.s - 2 * l.m) * 2 +
						//abs(2 * l.e)+abs(2 * l.w);
						abs(l.ne + l.se - 2 * l.e) +
						abs(l.nw + l.sw - 2 * l.w);
					float vertical =
						abs(l.e + l.w - 2 * l.m) * 2 +
						//abs(2 * l.n)+abs(2 * l.s);
						abs(l.ne + l.nw - 2 * l.n) +
						abs(l.se + l.sw - 2 * l.s);
					e.isHorizontal = horizontal >= vertical;
					e.pixelStep = e.isHorizontal ? _MainTex_TexelSize.y : _MainTex_TexelSize.x;

					float pLuminance = e.isHorizontal ? l.n : l.e;
					float nLuminance = e.isHorizontal ? l.s : l.w;
					float pGradient = abs(pLuminance - l.m);
					float nGradient = abs(nLuminance - l.m);
					if (pGradient < nGradient)
					{
						e.pixelStep = -e.pixelStep;
						e.oppositeLuminance = nLuminance;
						e.gradient = nGradient;
					}
					else
					{
						e.oppositeLuminance = pLuminance;
						e.gradient = pGradient;
					}
					return e;
				}


				#define EDGE_STEP_COUNT 2
				//#define EDGE_STEPS 1, 1.5, 2, 2, 2, 2, 2, 2, 2, 4
				#define EDGE_STEPS 1,4
				#define EDGE_GUESS 8
				static const float edgeSteps[EDGE_STEP_COUNT] = { EDGE_STEPS };

				float DetermineEdgeBlendFactor(LuminanceData l, EdgeData e, float2 uv) 
				{
					float2 uvEdge = uv;
					float2 edgeStep;
					if (e.isHorizontal) {
						uvEdge.y += e.pixelStep * 0.5;
						edgeStep = float2(_MainTex_TexelSize.x, 0);
					}
					else {
						uvEdge.x += e.pixelStep * 0.5;
						edgeStep = float2(0, _MainTex_TexelSize.y);
					}

					float edgeLuminance = (l.m + e.oppositeLuminance) * 0.5;
					float gradientThreshold = e.gradient * 0.25;

					float2 puv = uvEdge + edgeStep * edgeSteps[0];
					float pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
					bool pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
					UNITY_UNROLL
					for (int i = 1; i < EDGE_STEP_COUNT && !pAtEnd; i++)
					{
						puv += edgeStep * edgeSteps[i];
						pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
						pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
					}
					if (!pAtEnd) {
						puv += edgeStep * EDGE_GUESS;
					}

					float2 nuv = uvEdge - edgeStep * edgeSteps[0];
					float nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
					bool nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
					UNITY_UNROLL
					for (int i = 1; i < EDGE_STEP_COUNT && !nAtEnd; i++) {
						nuv -= edgeStep * edgeSteps[i];
						nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
						nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
					}
					if (!nAtEnd) {
						nuv -= edgeStep * EDGE_GUESS;
					}

					float pDistance, nDistance;
					if (e.isHorizontal) {
						pDistance = puv.x - uv.x;
						nDistance = uv.x - nuv.x;
					}
					else {
						pDistance = puv.y - uv.y;
						nDistance = uv.y - nuv.y;
					}

					float shortestDistance;
					bool deltaSign;
					if (pDistance <= nDistance) {
						shortestDistance = pDistance;
						deltaSign = pLuminanceDelta >= 0;
					}
					else {
						shortestDistance = nDistance;
						deltaSign = nLuminanceDelta >= 0;
					}

					if (deltaSign == (l.m - edgeLuminance >= 0)) {
						return 0;
					}

					return 0.5 - shortestDistance / (pDistance + nDistance);
				}
				//--------------------------------------------------------------------------




				v2f vert(appdata_img v)
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.uv = v.texcoord;
					o.uvSPR = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
					o.uvFlipped = v.texcoord;
				#if UNITY_UV_STARTS_AT_TOP
					if (_MainTex_TexelSize.y < 0.0)
						o.uvFlipped.y = 1.0 - o.uvFlipped.y;
				#endif 
					o.uvFlippedSPR = UnityStereoScreenSpaceUVAdjust(o.uvFlipped, _MainTex_ST);
					#if MEANBLUR
						o.offs[0] = o.uv + _BlurAmount * _MainTex_TexelSize * float2( blue_amount,  blue_amount);//右上
						o.offs[1] = o.uv + _BlurAmount * _MainTex_TexelSize * float2(-blue_amount,  blue_amount);//左上
						o.offs[2] = o.uv + _BlurAmount * _MainTex_TexelSize * float2(-blue_amount, -blue_amount);//左下
						o.offs[3] = o.uv + _BlurAmount * _MainTex_TexelSize * float2( blue_amount, -blue_amount);//右下
					#endif
					return o;
				}
				half4 farg(v2f i) : SV_Target
				{
					half3 finalCol = tex2D(_MainTex, i.uvSPR);
					#if BLOOM
						finalCol = GammaToLinearSpace(finalCol);
						finalCol += tex2D(_BloomTex, i.uvSPR)*_BloomStrength*_BloomColor;
						finalCol = saturate(finalCol);
						finalCol = LinearToGammaSpace(finalCol);
					#endif
					#if QUALITYBLOOM
						finalCol = GammaToLinearSpace(finalCol);
						float4 offsets = _MainTex_TexelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * ( _Bloom_Settings.x * 0.5);
						half3 col=  tex2D(_QualityBloomTex, i.uvFlippedSPR + offsets.xy).rgb * tex2D(_QualityBloomTex, i.uvFlippedSPR + offsets.xy).a * 8.0;
						col += tex2D(_QualityBloomTex, i.uvFlippedSPR + offsets.zy).rgb * tex2D(_QualityBloomTex, i.uvFlippedSPR + offsets.zy).a * 8.0;
						col += tex2D(_QualityBloomTex, i.uvFlippedSPR + offsets.xw).rgb * tex2D(_QualityBloomTex, i.uvFlippedSPR + offsets.xw).a * 8.0;
						col += tex2D(_QualityBloomTex, i.uvFlippedSPR + offsets.zw).rgb * tex2D(_QualityBloomTex, i.uvFlippedSPR + offsets.zw).a * 8.0;
						half3 bloom = col* (1.0 / 4.0) * _Bloom_Settings.y;
						finalCol += bloom;
						finalCol = saturate(finalCol);
						finalCol = LinearToGammaSpace(finalCol);
					#endif
					#if DOF
						fixed4 ori = fixed4(finalCol,1);
						fixed4 blur = tex2D(_DOFTex, i.uvSPR);
						float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
						depth = Linear01Depth(depth);
						fixed4 final = (depth <= _FocusDistance) ? ori : lerp(ori, blur, clamp((depth - _FocusDistance) * _FarBlurScale, 0, 1));
						#if MULTIPASS
							finalCol *= (depth > _FocusDistance) ? final : lerp(ori, blur, clamp((_FocusDistance - depth) * _NearBlurScale, 0, 1));
						#else
							finalCol = (depth > _FocusDistance) ? final : lerp(ori, blur, clamp((_FocusDistance - depth) * _NearBlurScale, 0, 1));
						#endif
					#endif
					#if QUALITYDOF
						half _depth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv));
						_depth -= _ClearDistance + _DOFDistance;
						_depth *= _DOFRange;
						_depth = saturate(_depth);
						half3 dof = tex2D(_QualityDOFTex, i.uvFlippedSPR);
						finalCol = lerp(finalCol, dof, _depth);
					#endif
					#if FXAA
						LuminanceData l = SampleLuminanceNeighborhood(i.uv);
						if (ShouldSkipPixel(l))
							return tex2D(_MainTex, i.uv);
						
						float pixelBlend = DeterminePixelBlendFactor(l);
						EdgeData e = DetermineEdge(l);
						float edgeBlend = DetermineEdgeBlendFactor(l, e, i.uv);
						float finalBlend = max(pixelBlend, edgeBlend);
						
						if (e.isHorizontal)
						{
							i.uv.y += e.pixelStep * finalBlend;
						}
						else
						{
							i.uv.x += e.pixelStep * finalBlend;
						}
						finalCol *= tex2Dlod(_MainTex, float4(i.uv, 0, 0));
					#endif
					#if LUT
						finalCol = GammaToLinearSpace(finalCol);
						finalCol *= _ExposureEV;
						half3 colorLogC = saturate(LinearToLogC(finalCol));
						finalCol = ApplyLut2d(_LogLut, colorLogC, _LogLut_Params);
						finalCol = saturate(finalCol);
						finalCol = LinearToGammaSpace(finalCol);
					#endif
					#if MEANBLUR
						finalCol += tex2D(_MainTex, i.offs[0]);
						finalCol += tex2D(_MainTex, i.offs[1]);
						finalCol += tex2D(_MainTex, i.offs[2]);
						finalCol += tex2D(_MainTex, i.offs[3]);
						finalCol*=0.2;
					#endif
					#if MOTIONBLUR
						finalCol = tex2D(_MotionBlurTex, i.uv);;
					#endif
					#if RADIABLUR
						//0.5,0.5屏幕中心
						float2 dir = float2(0.5, 0.5) - i.uv;//从采样中心到uv的方向向量
						float2 texcoord = i.uv;
						float dist = length(dir);  
						dir = normalize(dir); 
						float4 color = tex2D(_MainTex, texcoord);  
						float4 sum = color;
						//6次采样
						sum += tex2D(_MainTex, texcoord + dir * samples[0] * _SampleDist);
						sum += tex2D(_MainTex, texcoord + dir * samples[1] * _SampleDist);
						sum += tex2D(_MainTex, texcoord + dir * samples[2] * _SampleDist);
						sum += tex2D(_MainTex, texcoord + dir * samples[3] * _SampleDist);
						sum += tex2D(_MainTex, texcoord + dir * samples[4] * _SampleDist);
						sum += tex2D(_MainTex, texcoord + dir * samples[5] * _SampleDist);
						//求均值
						sum /= 7.0f;  
						//越离采样中心近的地方，越不模糊
						float t = saturate(dist * _SampleStrength);  
						//插值
						finalCol = lerp(color, sum, t);
					#endif
					#if WINDSCREEN
						float2 uv = i.uv/1.0;
						float4 tex = tex2D(_MainTex,uv);
						float dist2;
						#if CIRCLE
							dist2 = 1.0 - smoothstep(_ViewArea,_ViewArea-_ViewAreaSmooth, length(float2(0.5,0.5) - uv));
							dist2 += 1.0 - smoothstep(_OpeningVal,_OpeningVal-_ViewArea, length(float2(0.5,0.5) - uv.y));
						#elif HORIZONTAL
							dist2 = 1.0 - smoothstep(_ViewArea,_ViewArea-_ViewAreaSmooth, length(float2(0.5,0.5) - uv.y));
						#elif VERTICAL
							dist2 = 1.0 - smoothstep(_ViewArea,_ViewArea-_ViewAreaSmooth, length(float2(0.5,0.5) - uv.x));
						#endif
						float3 black=float3(0.0,0.0,0.0);
						float3 ret=lerp(tex,black,dist2);
						finalCol *= float4( ret, 1.0 );
					#endif
					
					return half4(finalCol, 1);
				} 
			ENDCG
		}
	}
	FallBack Off	
}
