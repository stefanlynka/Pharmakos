Shader "Pharmakos/AltarContactShadow"
{
    // Soft dark skirt on the ground round a ritual altar (see AltarContactShadow.cs), so the altar sits on the
    // board instead of floating over it. The mesh is a flat ring round the footprint: uv.y = 0 at the altar's
    // edge, 1 at the outer rim. Alpha-blended, no depth write; the altar itself hides the inner edge.
    Properties
    {
        _Color ("Shadow Colour", Color) = (0.03, 0.02, 0.015, 1)
        _Strength ("Strength", Range(0, 1)) = 0.7
        _Falloff ("Falloff", Range(0.5, 4)) = 1.8
    }

    SubShader
    {
        Tags { "Queue"="Transparent-200" "IgnoreProjector"="True" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Strength;
                half _Falloff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half t = saturate(1.0h - input.uv.y);
                half alpha = pow(t, _Falloff) * _Strength;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
