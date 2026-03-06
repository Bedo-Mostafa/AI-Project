Shader "Sun_Temple/Foliage" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Layer_A Albedo (RGB)", 2D) = "black" {} 
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _SelfIllum("Self Illumination", Range(0, 1)) = 0
        _NormalModification("Normal Modification", Range(0, 1)) = 1

        _WaveFreq("Wave Frequency", Float) = 20
        _WaveHeight("Wave Height", Float) = 0.1  
        _WaveScale("Wave Scale", Float) = 1
    }

    SubShader {        
        Tags {
            "Queue" = "AlphaTest" 
            "IgnoreProjector" = "True" 
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "VertexWind.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _SelfIllum;
                half _NormalModification;
                half _WaveFreq;
                half _WaveHeight;
                half _WaveScale;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                float4 color : COLOR;
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

            Varyings vert(Attributes input) {
                Varyings output = (Varyings)0;

                // Apply wind animation
                float3 windOffset = wind_simplified(input.positionOS.xyz, input.color, _WaveFreq, _WaveHeight, _WaveScale, unity_WorldToObject);
                float3 animatedPos = input.positionOS.xyz + windOffset;

                // Modify normal (push towards up direction)
                half3 modifiedNormal = half3(0, 2, 0);
                float3 finalNormal = lerp(input.normalOS, input.normalOS + modifiedNormal, _NormalModification);
                finalNormal = normalize(finalNormal);
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(animatedPos);
                VertexNormalInputs normInputs = GetVertexNormalInputs(finalNormal, input.tangentOS);
                
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
                
                // Alpha test
                clip(albedoTex.a - _Cutoff);

                half3 albedo = albedoTex.rgb * _Color.rgb;
                half3 emission = albedoTex.rgb * _SelfIllum;

                // Setup input data for lighting
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                // Setup surface data
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = 0;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = emission;
                surfaceData.occlusion = 1;
                surfaceData.alpha = albedoTex.a;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                
                return color;
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "VertexWind.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _SelfIllum;
                half _NormalModification;
                half _WaveFreq;
                half _WaveHeight;
                half _WaveScale;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes input) {
                Varyings output = (Varyings)0;

                // Apply wind animation
                float3 windOffset = wind_simplified(input.positionOS.xyz, input.color, _WaveFreq, _WaveHeight, _WaveScale, unity_WorldToObject);
                float3 animatedPos = input.positionOS.xyz + windOffset;

                // Modify normal
                half3 modifiedNormal = half3(0, 2, 0);
                float3 finalNormal = lerp(input.normalOS, input.normalOS + modifiedNormal, _NormalModification);
                finalNormal = normalize(finalNormal);

                float3 positionWS = TransformObjectToWorld(animatedPos);
                float3 normalWS = TransformObjectToWorldNormal(finalNormal);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target {
                half4 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(albedoTex.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // Depth pass
        Pass {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "VertexWind.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _SelfIllum;
                half _NormalModification;
                half _WaveFreq;
                half _WaveHeight;
                half _WaveScale;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings DepthVert(Attributes input) {
                Varyings output = (Varyings)0;

                // Apply wind animation
                float3 windOffset = wind_simplified(input.positionOS.xyz, input.color, _WaveFreq, _WaveHeight, _WaveScale, unity_WorldToObject);
                float3 animatedPos = input.positionOS.xyz + windOffset;

                VertexPositionInputs posInputs = GetVertexPositionInputs(animatedPos);
                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target {
                half4 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(albedoTex.a - _Cutoff);
                return input.positionCS.z;
            }
            ENDHLSL
        }
    } 

    Fallback Off
}