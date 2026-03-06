Shader "Sun_Temple/VertexBlend" {
    Properties {
        _Color("BASE Tint (RGB), Tint Fade (A)", Color) = (0.5, 0.5, 0.5, 0)
        _MainTex ("BASE Albedo (RGB) Tint Mask (A)", 2D) = "white" {}       
        [Normal]_BumpMap ("BASE Normal (RGB)", 2D) = "bump" {} 
        _Roughness("BASE Roughness", Range(0,1)) = 1

        [NoScaleOffset] _layer1Tex ("LAYER_B Albedo (RGB)", 2D) = "white" {}       
        [Normal][NoScaleOffset] _layer1Norm("LAYER_B Normal (RGB)", 2D) = "bump" {}
        _layer1Tiling("LAYER_B Tiling", Float) = 1
        _layer1Rough("LAYER_B Roughness", Range(0, 1)) = 1

        [NoScaleOffset] _BlendMask("BLEND_Mask (R)", 2D) = "white" {}
        _BlendTile("BLEND_Tiling", Float) = 1
        _Choke ("BLEND_Choke", Range(0, 60)) = 15
        _Crisp ("BLEND_Crispyness", Range(1, 20)) = 5
       
        [NoScaleOffset] _DetailAlbedo ("DETAIL_Albedo (R)", 2D) = "grey" {}
        [Normal][NoScaleOffset] _DetailNormal ("DETAIL_Normal (RGB)", 2D) = "bump" {}  
        _DetailNormalStrength ("DETAIL_Normal Strength", Range(0,1)) = 0.4    
        _DetailTiling("DETAIL_Tiling", Float) = 2              
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

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
            TEXTURE2D(_layer1Tex);      SAMPLER(sampler_layer1Tex);
            TEXTURE2D(_layer1Norm);     SAMPLER(sampler_layer1Norm);
            TEXTURE2D(_BlendMask);      SAMPLER(sampler_BlendMask);
            TEXTURE2D(_DetailAlbedo);   SAMPLER(sampler_DetailAlbedo);
            TEXTURE2D(_DetailNormal);   SAMPLER(sampler_DetailNormal);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                half4 _Color;
                half _Roughness;
                half _layer1Tiling;
                half _layer1Rough;
                half _BlendTile;
                half _Choke;
                half _Crisp;
                half _DetailNormalStrength;
                half _DetailTiling;
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
                float4 tangentWS : TEXCOORD3;
                float4 color : COLOR;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
                float fogFactor : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
            };

            Varyings vert(Attributes input) {
                Varyings output = (Varyings)0;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(posInputs);
                
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                
                return output;
            }

            half3 LerpWhiteTo(half3 b, half t) {
                half oneMinusT = 1 - t;
                return half3(oneMinusT, oneMinusT, oneMinusT) + b * t;
            }

            half4 frag(Varyings input) : SV_Target {
                // Base layer textures
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                mainTex = lerp(mainTex, _Color, mainTex.a * _Color.a);

                half3 baseNormal = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));

                // Layer1 Textures
                float2 layer1UV = input.uv * _layer1Tiling;
                half3 layer1Albedo = SAMPLE_TEXTURE2D(_layer1Tex, sampler_layer1Tex, layer1UV).rgb;
                half3 layer1Normal = UnpackNormal(SAMPLE_TEXTURE2D(_layer1Norm, sampler_layer1Norm, layer1UV));

                // Blend Mask
                half blendMask = SAMPLE_TEXTURE2D(_BlendMask, sampler_BlendMask, input.uv * _BlendTile).r;
                blendMask = clamp(blendMask, 0.2, 0.9);

                // Detail Textures
                float2 detailUV = input.uv * _DetailTiling;
                half detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedo, sampler_DetailAlbedo, detailUV).r;
                half3 detailNormal = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal, sampler_DetailNormal, detailUV));

                // Compute blend factor from vertex color
                half blend = (input.color.r * blendMask) * _Choke;
                blend = pow(blend, _Crisp);
                blend = saturate(blend);

                // Blended textures
                half3 blendedAlbedo = lerp(layer1Albedo, mainTex.rgb, blend);
                blendedAlbedo = blendedAlbedo * LerpWhiteTo(detailAlbedo * 2.0, 1.0);

                half3 blendedNormal = lerp(layer1Normal, baseNormal, blend);
                blendedNormal = blendedNormal + (detailNormal * half3(_DetailNormalStrength, _DetailNormalStrength, 0));
                blendedNormal = normalize(blendedNormal);

                half blendedSmoothness = lerp(_layer1Rough, _Roughness, blend);
                half smoothness = saturate(1 - blendedSmoothness);

                // Transform normal from tangent to world space
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tangentToWorld = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalWS = normalize(mul(blendedNormal, tangentToWorld));

                // Setup input data for lighting
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                // Setup surface data
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = blendedAlbedo;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = blendedNormal;
                surfaceData.emission = 0;
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;

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

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // Depth pass
        Pass {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // Depth Normals pass
        Pass {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}