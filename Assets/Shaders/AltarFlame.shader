Shader "Pharmakos/AltarFlame"
{
    // Flame skirt for ritual altars (see AltarFlame.cs). The mesh is a ring of quads round the altar's footprint:
    // uv.y = 0 at the base, 1 at the flame tips. Tongues come from 3D noise on the object-space position scrolling
    // upward, so the ring has no seams. Additive, no depth write, both sides drawn.
    Properties
    {
        _HotColor ("Hot (base)", Color) = (1, 0.92, 0.6, 1)
        _MidColor ("Body", Color) = (1, 0.45, 0.08, 1)
        _TipColor ("Tips", Color) = (0.55, 0.06, 0.01, 1)
        _Intensity ("Intensity", Float) = 1
        _NoiseScale ("Noise Scale", Float) = 6
        _RiseSpeed ("Rise Speed", Float) = 0.9
        _TongueSharpness ("Tongue Sharpness", Range(0.02, 0.6)) = 0.22
        _BaseGlow ("Base Glow", Range(0, 2)) = 0.6
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent+100" "IgnoreProjector"="True" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _HotColor;
                half4 _MidColor;
                half4 _TipColor;
                half _Intensity;
                half _NoiseScale;
                half _RiseSpeed;
                half _TongueSharpness;
                half _BaseGlow;
                float _Seed;
            CBUFFER_END

            float Hash31(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float Noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                float n000 = Hash31(i);
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));
                return lerp(lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y),
                            lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y), u.z);
            }

            float Fbm(float3 p)
            {
                float total = 0, amp = 0.55, norm = 0;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    total += Noise3(p) * amp;
                    norm += amp;
                    p = p * 2.07 + float3(13.1, 7.7, 19.3);
                    amp *= 0.5;
                }
                return total / norm;
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                o.positionOS = input.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float v = saturate(i.uv.y);
                float t = _Time.y * _RiseSpeed;
                // Sample along the ring (object x/z) and up the flame (v), scrolling upward.
                float3 p = float3(i.positionOS.x, i.positionOS.z, 0) * _NoiseScale;
                float body = Fbm(float3(p.x, p.y + _Seed, v * 2.2 - t * 1.6));
                float detail = Fbm(float3(p.x * 2.3 + 5.1, p.y * 2.3, v * 4.5 - t * 2.6));
                float n = body * 0.7 + detail * 0.3;

                // Tongues: noise must beat a threshold that rises with height, so flames thin out into tips.
                float threshold = lerp(0.18, 0.78, v);
                float tongue = smoothstep(threshold, threshold + _TongueSharpness, n);
                float baseFade = smoothstep(0.0, 0.08, v);              // soft where it meets the board
                float flame = tongue * baseFade;
                float baseGlow = exp(-v * 9.0) * _BaseGlow * (0.6 + 0.4 * n);

                half3 color = lerp(_HotColor.rgb, _MidColor.rgb, saturate(v * 1.8 + (1 - tongue) * 0.3));
                color = lerp(color, _TipColor.rgb, saturate((v - 0.45) * 1.6));
                float flicker = 0.85 + 0.3 * Noise3(float3(t * 3.1, _Seed, 0));
                half3 result = (color * flame + _MidColor.rgb * baseGlow) * _Intensity * flicker;
                return half4(result, 1);
            }
            ENDHLSL
        }
    }
}
