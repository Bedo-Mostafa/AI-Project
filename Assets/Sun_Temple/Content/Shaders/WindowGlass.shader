Shader "Sun_Temple/WindowGlass" {
    Properties {
        _MainTex ("Albedo (RGB) Glass Mask(A)", 2D) = "white" {}  
        [NoScaleOffset]_RoughnessTexture ("Roughness (R)", 2D) = "white" {}       
        [Normal][NoScaleOffset]_BumpMap ("Normal", 2D) = "bump" {}
        [NoScaleOffset]_Emission("Emission(RGB)", 2D) = "black" {}
         
        _EmissionIntensity("Emission Intensity", Range(0, 8)) = 0
        _EmissionVertexMask("Emission Vertex Mask", Range(0, 1)) = 0
        _Reflection("Reflection (CUBE)", CUBE) = ""{}
        _SkyColor("Sky Color (RGB)", Color) = (1, 1, 1, 1)
    }

    SubShader {
        Tags { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry" 
            "ForceNoShadowCasting" = "True"
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

            TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_Emission);           SAMPLER(sampler_Emission);
            TEXTURE2D(_RoughnessTexture);   SAMPLER(sampler_RoughnessTexture);
            TEXTURECUBE(_Reflection);       SAMPLER(sampler_Reflection);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _EmissionIntensity;
                half _EmissionVertexMask;
                half3 _SkyColor;
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
                float3 viewDirWS : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
                float fogFactor : TEXCOORD6;
                float4 shadowCoord : TEXCOORD7;
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
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(posInputs);
                
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // Sample textures
                half4 colorTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                half roughness = SAMPLE_TEXTURE2D(_RoughnessTexture, sampler_RoughnessTexture, input.uv).r;
                
                // Emission with vertex color mask
                half emissionMask = lerp(0, 1, pow(input.color.r, 4));
                half3 emissionTex = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, input.uv).rgb;
                half3 emission = emissionTex * emissionMask * _EmissionIntensity * _SkyColor;

                // Transform normal from tangent to world space
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tangentToWorld = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, tangentToWorld));

                // Calculate fresnel
                half3 viewDirTS = normalize(mul(tangentToWorld, input.viewDirWS));
                half fresnel = 1.0 - saturate(dot(normalize(input.viewDirWS), normalWS));

                // Sample cubemap reflection
                float3 reflectDir = reflect(-input.viewDirWS, normalWS);
                half3 reflection = SAMPLE_TEXTURECUBE(_Reflection, sampler_Reflection, reflectDir).rgb;
                reflection = reflection * (1 - roughness * 2) * pow(fresnel, 2);

                // Setup input data for lighting
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = input.viewDirWS;
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                // Setup surface data
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = colorTex.rgb;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = roughness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = emission + saturate(reflection);
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                
                return color;
            }
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