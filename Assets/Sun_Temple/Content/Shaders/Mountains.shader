Shader "Sun_Temple/Mountains" {
    Properties {
        _TerrainNormal ("Terrain Normal map (overall)", 2D) = "bump" {}

        _MainTex ("Layer_A Albedo (RGB)", 2D) = "white" {}       
        _BumpMap ("LAYER_A Normal", 2D) = "bump" {}
        _baseTiling("LAYER_A Tiling", Float) = 1

        _layer1Tex ("LAYER_B Albedo (RGB) Smoothness (A)", 2D) = "white" {}       
        _layer1Norm("LAYER_B Normal", 2D) = "bump" {}
        _layer1Tiling("LAYER_B Tiling", Float) = 1

        _BlendMask("BLEND_Mask", 2D) = "white" {}
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
            TEXTURE2D(_TerrainNormal);  SAMPLER(sampler_TerrainNormal);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                float4 _BlendMask_ST;
                float4 _TerrainNormal_ST;
                half _baseTiling;
                half _layer1Tiling;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
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
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(posInputs);
                
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // Base layer textures
                half3 layerA_albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv * _baseTiling).rgb;
                half3 layerA_normal = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv * _baseTiling));

                half3 terrainNormal = UnpackNormal(SAMPLE_TEXTURE2D(_TerrainNormal, sampler_TerrainNormal, input.uv));

                // Layer1 Textures
                half3 layerB_albedo = SAMPLE_TEXTURE2D(_layer1Tex, sampler_layer1Tex, input.uv * _layer1Tiling).rgb;
                half3 layerB_normal = UnpackNormal(SAMPLE_TEXTURE2D(_layer1Norm, sampler_layer1Norm, input.uv * _layer1Tiling));

                // Blend Mask
                half blendMask = SAMPLE_TEXTURE2D(_BlendMask, sampler_BlendMask, input.uv).r;

                // Blended textures
                half3 blendedAlbedo = lerp(layerB_albedo, layerA_albedo, blendMask);
                half3 blendedNormal = lerp(layerB_normal, layerA_normal, blendMask);
                half3 finalNormal = terrainNormal + half3(blendedNormal.r, blendedNormal.g, 0);
                finalNormal = normalize(finalNormal);

                // Transform normal from tangent to world space
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tangentToWorld = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalWS = normalize(mul(finalNormal, tangentToWorld));

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
                surfaceData.smoothness = 0;
                surfaceData.normalTS = finalNormal;
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