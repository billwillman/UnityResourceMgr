// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

// - Unlit
// - Per-vertex (virtual) camera space specular light
// - SUPPORTS lightmap

Shader "XProject/Lightmap + Wind" {
Properties {
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_Wind("Wind params",Vector) = (1,1,1,1)
	_WindEdgeFlutter("Wind edge fultter factor", float) = 0.5
	_WindEdgeFlutterFreqScale("Wind edge fultter freq scale",float) = 0.5
}

SubShader {
	Tags {"Queue"="Transparent" "RenderType"="Transparent" "LightMode"="ForwardBase"}
	LOD 100
	
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Off ZWrite Off
	
	
	CGINCLUDE
	#include "UnityCG.cginc"
	#include "TerrainEngine.cginc"
	sampler2D _MainTex;
	float4 _MainTex_ST;
	samplerCUBE _ReflTex;
	
	#ifndef LIGHTMAP_OFF
	// float4 unity_LightmapST;
	// sampler2D unity_Lightmap;
	#endif
	
	float _WindEdgeFlutter;
	float _WindEdgeFlutterFreqScale;

	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		#ifndef LIGHTMAP_OFF
		float2 lmap : TEXCOORD1;
		#endif
		fixed3 spec : TEXCOORD2;
	};

inline float4 AnimateVertex2(float4 pos, float3 normal, float4 animParams,float4 wind,float2 time)
{	
	// animParams stored in color
	// animParams.x = branch phase
	// animParams.y = edge flutter factor
	// animParams.z = primary factor
	// animParams.w = secondary factor

	float fDetailAmp = 0.1f;
	float fBranchAmp = 0.3f;
	
	// Phases (object, vertex, branch)
	float fObjPhase = dot(_Object2World[3].xyz, 1);
	float fBranchPhase = fObjPhase + animParams.x;
	
	float fVtxPhase = dot(pos.xyz, animParams.y + fBranchPhase);
	
	// x is used for edges; y is used for branches
	float2 vWavesIn = time  + float2(fVtxPhase, fBranchPhase );
	
	// 1.975, 0.793, 0.375, 0.193 are good frequencies
	float4 vWaves = (frac( vWavesIn.xxyy * float4(1.975, 0.793, 0.375, 0.193) ) * 2.0 - 1.0);
	
	vWaves = SmoothTriangleWave( vWaves );
	float2 vWavesSum = vWaves.xz + vWaves.yw;

	// Edge (xz) and branch bending (y)
	float3 bend = animParams.y * fDetailAmp * normal.xyz;
	bend.y = animParams.w * fBranchAmp;
	pos.xyz += ((vWavesSum.xyx * bend) + (wind.xyz * vWavesSum.y * animParams.w)) * wind.w; 

	// Primary bending
	// Displace position
	pos.xyz += animParams.z * wind.xyz;
	
	return pos;
}


	
	v2f vert (appdata_full v)
	{
		v2f o;
		
		float4	wind;
		
		float			bendingFact	= v.color.a;
		
		wind.xyz	= mul((float3x3)_World2Object,_Wind.xyz);
		wind.w		= _Wind.w  * bendingFact;
		
		
		float4	windParams	= float4(0,_WindEdgeFlutter,bendingFact.xx);
		float 		windTime 		= _Time.y * float2(_WindEdgeFlutterFreqScale,1);
		float4	mdlPos			= AnimateVertex2(v.vertex,v.normal,windParams,wind,windTime);
		
		o.pos = mul(UNITY_MATRIX_MVP,mdlPos);
		o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
		
		o.spec = v.color;
		
		#ifndef LIGHTMAP_OFF
		o.lmap = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
		#endif
		
		return o;
	}
	ENDCG


	Pass {
		CGPROGRAM
		#pragma debug
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest		
		fixed4 frag (v2f i) : COLOR
		{
			fixed4 tex = tex2D (_MainTex, i.uv);
			
			fixed4 c;
			c.rgb = tex.rgb;
			c.a = tex.a;
			
			#ifndef LIGHTMAP_OFF
			fixed3 lm = DecodeLightmap (UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap));
			c.rgb *= lm;
			#endif
			
			return c;
		}
		ENDCG 
	}	
}
}


