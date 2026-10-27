Shader "TanksKombat2/TiledAtlasUnlit"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1, 1, 1, 1)

        _Columns("Columns", Float) = 1
        _Rows("Rows", Float) = 1
        _Frame("Frame", Float) = 0
        [Toggle] _IgnoreObjectRotation("Ignore Object Rotation", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Columns;
                float _Rows;
                float _Frame;
                float _IgnoreObjectRotation;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _BaseMap_ST)
                    UNITY_DOTS_INSTANCED_PROP(float, _Frame)
                    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _BaseMap_ST UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseMap_ST)
                #define _Frame UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Frame)
                #define _BaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
            #endif

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                if (_IgnoreObjectRotation > 0.5)
                {
                    float4x4 objectToWorld = GetObjectToWorldMatrix();
                    float3 objectCenterWS = objectToWorld._m03_m13_m23;
                    float objectScale = length(objectToWorld._m00_m10_m20);

                    float3 positionWS = objectCenterWS + input.positionOS.xyz * objectScale;
                    output.positionCS = TransformWorldToHClip(positionWS);
                }
                else
                {
                    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                }

                // Keep the unwrapped UV here.  Applying frac() per vertex makes
                // all quad vertices identical for an integer tiling value, so the
                // interpolated UV becomes a single point (and renders one colour).
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // Integer frame math (rounded inputs): avoids fmod/floor float semantics
                // and SPIR-V translation quirks that misplace the atlas frame on some drivers.
                // Unsigned arithmetic: signed int modulus is emulated and slow on GPU.
                uint columns = max((uint)round(_Columns), 1u);
                uint rows = max((uint)round(_Rows), 1u);
                uint frame = (uint)max(round(_Frame), 0.0);
                float2 tileUV = frac(input.uv);
                uint row = frame / columns;
                uint column = frame - row * columns; // multiply-subtract instead of %

                // Frame numbering goes left-to-right, then top-to-bottom. Texture
                // UVs run bottom-to-top, hence the inverted Y offset.
                float2 atlasUV = (tileUV + float2(column, rows - 1u - row)) / float2(columns, rows);

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, atlasUV);
                color *= _BaseColor;
                return color;
            }
            ENDHLSL
        }
    }
}
