Shader "Effect/PostEffectWideScreen"
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_TimeStep ("Time", Range(0.0, 1.0)) = 1.0
		_ScreenResolution ("_ScreenResolution", Vector) = (0.,0.,0.,0.)
	}
	SubShader
	{
		Pass
		{
			ZTest Always
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma shader_feature CIRCLE HORIZONTAL VERTICAL
			#include "UnityCG.cginc"
			uniform sampler2D _MainTex;
			uniform float _TimeStep;
			uniform float _ViewArea;
			uniform float _ViewAreaSmooth;
			uniform float4 _ScreenResolution;
			struct v2v
			{
				float4 pos   : POSITION;
				float2 uv : TEXCOORD0;
			};
			struct v2f
			{
				float4 pos   : SV_POSITION;
				half2 uv  : TEXCOORD0;
			};
			v2f vert(v2v i)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(i.pos);
				o.uv = i.uv;
				return o;
			}
			float4 frag (v2f i) : COLOR
			{
			    float2 uv = i.uv/1.0;
			    float4 tex = tex2D(_MainTex,uv);
				float dist2;
				#if CIRCLE
					dist2 = 1.0 - smoothstep(_ViewArea,_ViewArea-_ViewAreaSmooth, length(float2(0.5,0.5) - uv));
				#elif HORIZONTAL
					dist2 = 1.0 - smoothstep(_ViewArea,_ViewArea-_ViewAreaSmooth, length(float2(0.5,0.5) - uv.y));
				#elif VERTICAL
					dist2 = 1.0 - smoothstep(_ViewArea,_ViewArea-_ViewAreaSmooth, length(float2(0.5,0.5) - uv.x));
				#endif
			    float3 black=float3(0.0,0.0,0.0);
			    float3 ret=lerp(tex,black,dist2);
				return  float4( ret, 1.0 );
			}
			ENDCG
		}
	}
}
