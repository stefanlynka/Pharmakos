Shader "Pharmakos/CardEdgeMistGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.68, 0.9, 1, 0.78)
        _SecondaryColor ("Secondary Color", Color) = (0.9, 0.97, 1, 0.8)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _EmissionStrength ("Emission Strength", Range(0, 3)) = 0.8
        _MistSpeed ("Mist Speed", Range(0, 6)) = 0.85
        _MistScale ("Mist Scale", Range(0.5, 16)) = 3
        _DetailMistScale ("Detail Mist Scale", Range(1, 32)) = 8.5
        _FlickerSpeed ("Flicker Speed", Range(0, 12)) = 2.4
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.2
        _Drift ("Drift", Range(0, 2)) = 0.24
        _AlphaClip ("Alpha Clip", Range(0, 0.95)) = 0.02
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
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
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _GlowColor;
                half4 _SecondaryColor;
                half _Intensity;
                half _EmissionStrength;
                half _MistSpeed;
                half _MistScale;
                half _DetailMistScale;
                half _FlickerSpeed;
                half _FlickerStrength;
                half _Drift;
                half _AlphaClip;
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

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                clip(tex.a - _AlphaClip);

                float t = _Time.y * _MistSpeed;
                float2 flow = float2(t * (0.3 + _Drift), -t * (0.22 + _Drift * 0.7));

                float2 uvPrimary = input.uv * _MistScale + flow + _Seed;
                float2 uvDetail = input.uv * _DetailMistScale + flow * 1.8 + (_Seed * 3.17);
                float mistPrimary = FractalNoise(uvPrimary);
                float mistDetail = FractalNoise(uvDetail);
                float mist = saturate(mistPrimary * 0.7 + mistDetail * 0.3);

                float flickerNoise = ValueNoise(float2(_Time.y * _FlickerSpeed + _Seed * 2.1, _Seed * 7.3));
                float flicker = 1.0 + ((flickerNoise - 0.5) * 2.0) * _FlickerStrength;

                float alphaShaping = saturate(pow(tex.a, 0.65));
                float wispy = saturate((mist * 1.35 - 0.2) * alphaShaping);
                float intensity = _Intensity * flicker;
                float finalAlpha = _GlowColor.a * wispy * intensity;

                half3 glowTint = lerp(_GlowColor.rgb, _SecondaryColor.rgb, mist * 0.75);
                half3 color = glowTint * (0.55 + _EmissionStrength * mist) * intensity;
                return half4(color, finalAlpha);
            }
            ENDHLSL
        }
    }
}
