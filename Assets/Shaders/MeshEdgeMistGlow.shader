Shader "Pharmakos/MeshEdgeMistGlow"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (0.68, 0.9, 1, 0.78)
        _SecondaryColor ("Secondary Color", Color) = (0.9, 0.97, 1, 0.8)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _EmissionStrength ("Emission Strength", Range(0, 3)) = 0.8
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _ShellExtrusion ("Shell Extrusion", Range(0, 0.05)) = 0.012
        _MistSpeed ("Mist Speed", Range(0, 6)) = 0.85
        _MistScale ("Mist Scale", Range(0.5, 16)) = 3
        _DetailMistScale ("Detail Mist Scale", Range(1, 32)) = 8.5
        _FlickerSpeed ("Flicker Speed", Range(0, 12)) = 2.4
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.2
        _Drift ("Drift", Range(0, 2)) = 0.24
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+100"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Back
        Lighting Off
        ZWrite Off
        Blend One One

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
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half4 _SecondaryColor;
                half _Intensity;
                half _EmissionStrength;
                half _RimPower;
                half _ShellExtrusion;
                half _MistSpeed;
                half _MistScale;
                half _DetailMistScale;
                half _FlickerSpeed;
                half _FlickerStrength;
                half _Drift;
                float _Seed;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                float ab = lerp(a, b, u.x);
                float cd = lerp(c, d, u.x);
                return lerp(ab, cd, u.y);
            }

            float FractalNoise(float2 p)
            {
                float n = 0.0;
                n += ValueNoise(p) * 0.6;
                p = p * 2.03 + 17.7;
                n += ValueNoise(p) * 0.3;
                p = p * 2.11 + 39.3;
                n += ValueNoise(p) * 0.1;
                return n;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 extrudedPositionOS = input.positionOS.xyz + input.normalOS * _ShellExtrusion;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(extrudedPositionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float rim = pow(saturate(1.0 - saturate(dot(normalWS, viewDirWS))), _RimPower);

                float t = _Time.y * _MistSpeed;
                float2 flow = float2(t * (0.3 + _Drift), -t * (0.22 + _Drift * 0.7));
                float2 mistCoord = input.positionWS.xz * _MistScale + flow + _Seed;
                float2 mistDetailCoord = input.positionWS.xz * _DetailMistScale + flow * 1.8 + (_Seed * 3.17);
                float mist = saturate(FractalNoise(mistCoord) * 0.7 + FractalNoise(mistDetailCoord) * 0.3);

                float flickerNoise = ValueNoise(float2(_Time.y * _FlickerSpeed + _Seed * 2.1, _Seed * 7.3));
                float flicker = 1.0 + ((flickerNoise - 0.5) * 2.0) * _FlickerStrength;

                float wispy = saturate((mist * 1.35 - 0.15) * rim);
                float intensity = _Intensity * flicker;
                half3 glowTint = lerp(_GlowColor.rgb, _SecondaryColor.rgb, mist * 0.75);
                half3 color = glowTint * (0.45 + _EmissionStrength * mist) * intensity * wispy * _GlowColor.a;
                return half4(color, saturate(wispy * intensity));
            }
            ENDHLSL
        }
    }
}
