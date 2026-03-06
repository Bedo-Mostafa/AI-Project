Shader "Sun_Temple/Cloth" {
    Properties {
        _MainTex ("Layer_A Albedo (RGB)", 2D) = "white" {} 
        _SelfIllum("Self Illumination", Range(0, 1)) = 0

        [NoScaleOffset] _DetailAlbedo ("DETAIL_Albedo", 2D) = "grey" {}
        _DetailTiling("DETAIL_Tiling", Float) = 2  

        _WaveFreq("Wave Frequency", Float) = 20
        _WaveHeight("Wave Height", Float) = 0.1  
        _WaveScale("Wave Scale", Float) = 1
    }

    SubShader {        
        Tags { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 300

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
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_DetailAlbedo);   SAMPLER(sampler_DetailAlbedo);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _DetailTiling;
                half _SelfIllum;
                half _WaveFreq;
                half _WaveHeight;
                half _WaveScale;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
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

            half3 windanim(half3 vertex_xyz, half2 color, half waveFreq, half waveHeight, half waveScale) {
                half phase_slow = _Time.y * waveFreq;
                half phase_med = _Time.y * 4 * waveFreq;
               
                half offset = (vertex_xyz.x + (vertex_xyz.z * waveScale)) * waveScale;
                half offset2 = (vertex_xyz.x + (vertex_xyz.z * waveScale * 2)) * waveScale * 2;
             
                half sin1 = sin(phase_slow + offset);
                half sin2 = sin(phase_med + offset2);          
     
                half sin_combined = (sin1 * 4) + sin2;
               
                half wind_x = sin_combined * waveHeight * 0.1;
                half3 wind_xyz = half3(wind_x, wind_x * 2, wind_x);

                wind_xyz = wind_xyz * pow(color.r, 2);      
                return wind_xyz;
            }

            half3 LerpWhiteTo(half3 b, half t) {
                half oneMinusT = 1 - t;
                return half3(oneMinusT, oneMinusT, oneMinusT) + b * t;
            }

            Varyings vert(Attributes input) {
                Varyings output = (Varyings)0;

                // Apply wind animation
                float3 windOffset = windanim(input.positionOS.xyz, input.color.rg, _WaveFreq, _WaveHeight, _WaveScale);
                float3 animatedPos = input.positionOS.xyz + windOffset;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(animatedPos);
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

                half3 albedo = albedoTex.rgb * LerpWhiteTo(half3(detailAlbedo, detailAlbedo, detailAlbedo), 1);
                half3 emission = albedo * _SelfIllum;

                float3 normalWS = normalize(input.normalWS);

                // Lambert diffuse lighting
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

                half3 finalColor = albedo * (diffuse + bakedGI) + emission;
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, 1);
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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _DetailTiling;
                half _SelfIllum;
                half _WaveFreq;
                half _WaveHeight;
                half _WaveScale;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
            };

            half3 windanim(half3 vertex_xyz, half2 color, half waveFreq, half waveHeight, half waveScale) {
                half phase_slow = _Time.y * waveFreq;
                half phase_med = _Time.y * 4 * waveFreq;
               
                half offset = (vertex_xyz.x + (vertex_xyz.z * waveScale)) * waveScale;
                half offset2 = (vertex_xyz.x + (vertex_xyz.z * waveScale * 2)) * waveScale * 2;
             
                half sin1 = sin(phase_slow + offset);
                half sin2 = sin(phase_med + offset2);          
     
                half sin_combined = (sin1 * 4) + sin2;
               
                half wind_x = sin_combined * waveHeight * 0.1;
                half3 wind_xyz = half3(wind_x, wind_x * 2, wind_x);

                wind_xyz = wind_xyz * pow(color.r, 2);      
                return wind_xyz;
            }

            Varyings ShadowVert(Attributes input) {
                Varyings output = (Varyings)0;

                // Apply wind animation
                float3 windOffset = windanim(input.positionOS.xyz, input.color.rg, _WaveFreq, _WaveHeight, _WaveScale);
                float3 animatedPos = input.positionOS.xyz + windOffset;

                float3 positionWS = TransformObjectToWorld(animatedPos);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target {
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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _DetailTiling;
                half _SelfIllum;
                half _WaveFreq;
                half _WaveHeight;
                half _WaveScale;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
            };

            half3 windanim(half3 vertex_xyz, half2 color, half waveFreq, half waveHeight, half waveScale) {
                half phase_slow = _Time.y * waveFreq;
                half phase_med = _Time.y * 4 * waveFreq;
               
                half offset = (vertex_xyz.x + (vertex_xyz.z * waveScale)) * waveScale;
                half offset2 = (vertex_xyz.x + (vertex_xyz.z * waveScale * 2)) * waveScale * 2;
             
                half sin1 = sin(phase_slow + offset);
                half sin2 = sin(phase_med + offset2);          
     
                half sin_combined = (sin1 * 4) + sin2;
               
                half wind_x = sin_combined * waveHeight * 0.1;
                half3 wind_xyz = half3(wind_x, wind_x * 2, wind_x);

                wind_xyz = wind_xyz * pow(color.r, 2);      
                return wind_xyz;
            }

            Varyings DepthVert(Attributes input) {
                Varyings output = (Varyings)0;

                // Apply wind animation
                float3 windOffset = windanim(input.positionOS.xyz, input.color.rg, _WaveFreq, _WaveHeight, _WaveScale);
                float3 animatedPos = input.positionOS.xyz + windOffset;

                VertexPositionInputs posInputs = GetVertexPositionInputs(animatedPos);
                output.positionCS = posInputs.positionCS;

                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target {
                return input.positionCS.z;
            }
            ENDHLSL
        }
    } 

    Fallback Off
}