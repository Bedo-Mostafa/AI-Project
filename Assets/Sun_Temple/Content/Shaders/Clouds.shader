Shader "Sun_Temple/Clouds" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		
		_DistortionTexture("Distortion Texture", 2D) = "black" {}
		_DistortionIntensity("Distortion Intensity", Range(0, 1)) = 0.5
		_ScrollSpeed("Scroll Speed", Float) = 0.5
	}

	SubShader { 
		Tags { 
			"RenderType" = "Transparent" 
			"Queue" = "Overlay" 
			"IgnoreProjector" = "True" 
			"RenderPipeline" = "UniversalPipeline"
		}
		LOD 200
		Cull Off		
		ZWrite Off
		Blend OneMinusDstColor One

		Pass {
			Name "Unlit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_DistortionTexture);
			SAMPLER(sampler_DistortionTexture);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _DistortionTexture_ST;
				half4 _Color;
				half _ScrollSpeed;
				half _DistortionIntensity;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings vert(Attributes input) {
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return output;
			}

			half4 frag(Varyings input) : SV_Target {
				// Scrolling UV for distortion texture
				half scrollX = _ScrollSpeed * _Time.y;
				half2 uv_scrolled = input.uv + half2(scrollX, 0);

				// Sample distortion
				half distortion = SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, uv_scrolled).r;

				// Apply distortion to main texture UV
				half uv_distorted_x = (distortion * _DistortionIntensity * 0.1) - 0.05;
				half2 uv_distorted_xy = input.uv + half2(uv_distorted_x, 0);

				// Sample main texture and apply color
				half3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv_distorted_xy).rgb;
				half3 finalAlbedo = saturate(col * _Color.rgb);

				return half4(finalAlbedo, 1.0);
			}
			ENDHLSL
		}
	}

	FallBack "Universal Render Pipeline/Unlit"
}

