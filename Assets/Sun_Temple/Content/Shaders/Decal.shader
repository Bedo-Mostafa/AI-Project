Shader "Sun_Temple/Decal" {
    Properties {
        _Color ("Color Tint (RGB), Fade (A)", Color) = (0.5, 0.5, 0.5, 0)
        _MainTex ("Albedo (RGB), Alpha (A)", 2D) = "white" {} 
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.7

        [NoScaleOffset] _DetailAlbedo ("DETAIL_Albedo", 2D) = "grey" {}
        _DetailTiling("DETAIL_Tiling", Float) = 2  
    }

    SubShader {
        Tags {
            "Queue" = "AlphaTest" 
            "RenderType" = "TransparentCutout"  
            "ForceNoShadowCasting" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 400

        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back           
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_DetailAlbedo);   SAMPLER(sampler_DetailAlbedo);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _DetailTiling;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
                float fogFactor : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
            };

            half3 LerpWhiteTo(half3 b, half t) {
                half oneMinusT = 1 - t;
                return half3(oneMinusT, oneMinusT, oneMinusT) + b * t;
            }

            Varyings vert(Attributes input) {
                Varyings output = (Varyings)0;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = normInputs.normalWS;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(posInputs);
                
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                half4 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                half detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedo, sampler_DetailAlbedo, input.uv * _DetailTiling).r * 2.0;
                half3 albedo = albedoTex.rgb * _Color.rgb * LerpWhiteTo(half3(detailAlbedo, detailAlbedo, detailAlbedo), 1);

                half alphaMask = albedoTex.a * detailAlbedo * _Color.a;

                // Alpha cutoff
                clip(alphaMask - _Cutoff);

                half3 finalAlbedo = lerp(_Color.rgb, albedo, alphaMask);

                // Simple Lambert-style lighting
                float3 normalWS = normalize(input.normalWS);
                
                // Get main light
                Light mainLight = GetMainLight(input.shadowCoord);
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = mainLight.color * mainLight.shadowAttenuation * NdotL;

                // Add ambient/GI
                half3 bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                
                // Additional lights
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex) {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    half addNdotL = saturate(dot(normalWS, light.direction));
                    diffuse += light.color * light.distanceAttenuation * light.shadowAttenuation * addNdotL;
                }
                #endif

                half3 finalColor = finalAlbedo * (diffuse + bakedGI);
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, alphaMask);
            }
            ENDHLSL
        }

        // Depth pass for proper depth sorting
        Pass {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_DetailAlbedo);   SAMPLER(sampler_DetailAlbedo);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _DetailTiling;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings DepthVert(Attributes input) {
                Varyings output = (Varyings)0;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target {
                half4 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedo, sampler_DetailAlbedo, input.uv * _DetailTiling).r * 2.0;
                half alphaMask = albedoTex.a * detailAlbedo * _Color.a;
                clip(alphaMask - _Cutoff);
                return input.positionCS.z;
            }
            ENDHLSL
        }
    } 

    Fallback "Universal Render Pipeline/Lit"
}