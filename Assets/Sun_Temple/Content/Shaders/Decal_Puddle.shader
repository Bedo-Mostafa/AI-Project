Shader "Sun_Temple/Decal_Puddle" {
    Properties {
        _Color ("Color", Color) = (0.5, 0.5, 0.5, 0)
        _Mask("Mask (R)", 2D) = "black" {}
        _MaskFade("Mask Fade", Range(0, 1)) = 0
        _BumpMap("Normal (RGB)", 2D) = "bump"{}

        _Roughness("Roughness", Range(0, 1)) = 0

        _ScrollSpeed("ScrollSpeed", Range(0, 4)) = 2
    }

    SubShader {
        Tags {
            "Queue" = "Transparent" 
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

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

            TEXTURE2D(_Mask);       SAMPLER(sampler_Mask);
            TEXTURE2D(_BumpMap);    SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _Mask_ST;
                float4 _BumpMap_ST;
                half4 _Color;
                half _MaskFade;
                half _Roughness;
                half _ScrollSpeed;
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
                float2 uvMask : TEXCOORD0;
                float2 uvBump : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
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
                output.uvMask = TRANSFORM_TEX(input.uv, _Mask);
                output.uvBump = TRANSFORM_TEX(input.uv, _BumpMap);
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(posInputs);
                
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // Scrolling UVs for animated normals
                half scrollX = _ScrollSpeed * _Time.y;
                half scrollY = _ScrollSpeed * _Time.y;

                half2 uv1 = input.uvBump + half2(scrollX, scrollY);
                half2 uv2 = input.uvBump - half2(scrollX, scrollY);

                half3 albedo = _Color.rgb;
                half3 normal_a = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv1));
                half3 normal_b = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv2));

                half3 normalCombined = normalize(normal_a + normal_b);

                half maskAlpha = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.uvMask).r;
                half alpha = lerp(maskAlpha, 0, _MaskFade);

                // Transform normal from tangent to world space
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tangentToWorld = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalWS = normalize(mul(normalCombined, tangentToWorld));

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
                surfaceData.albedo = albedo;
                surfaceData.metallic = _Color.a;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = saturate(1 - _Roughness);
                surfaceData.normalTS = normalCombined;
                surfaceData.emission = 0;
                surfaceData.occlusion = 1;
                surfaceData.alpha = alpha;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                
                return color;
            }
            ENDHLSL
        }
    } 

    Fallback "Universal Render Pipeline/Lit"
}