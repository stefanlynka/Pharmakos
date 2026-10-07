Shader "Pharmakos/BrushStrokeReveal"
{
    // Animation state comes from vertex color (SpriteRenderer.color / particle color) so strokes keep batching:
    //   r = reveal progress (0 hidden -> 1 fully painted in)
    //   g = erase progress  (0 nothing wiped -> 1 fully wiped)
    //   b = per-stroke noise seed (shifts the bristle pattern)
    //   a = overall opacity
    // The stroke travels along _StrokeAngle in the sprite's UV space (0 = bottom to top, 45 = bottom-left to top-right).
    // Changing the angle per renderer needs a MaterialPropertyBlock, which breaks batching for that renderer, so for
    // large numbers of strokes keep the angle at 0 and rotate the transforms instead.
    // Requires sprites that are not packed into a Sprite Atlas, so UVs span 0-1 across the sprite.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        _NoiseTex ("Edge Noise (R)", 2D) = "gray" {}
        _EdgeNoise ("Edge Noise Amount", Range(0, 1)) = 0.3
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.06
        _StrokeAngle ("Stroke Angle (degrees clockwise from up)", Range(-180, 180)) = 0
        [Toggle(_USE_STROKE_ORDER)] _UseStrokeOrder ("Use Stroke Order Map", Float) = 0
        _StrokeOrderTex ("Stroke Order (R: 0 paints first, 1 paints last)", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _USE_STROKE_ORDER

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
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_StrokeOrderTex);
            SAMPLER(sampler_StrokeOrderTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float4 _NoiseTex_ST;
                half _EdgeNoise;
                half _EdgeSoftness;
                float _StrokeAngle;
            CBUFFER_END

            // 0 at the corner the stroke starts from, 1 at the opposite corner, for any angle.
            float StrokeCoordinate(float2 uv)
            {
                float angleRad = _StrokeAngle * (PI / 180.0);
                float2 dir = float2(sin(angleRad), cos(angleRad));
                float extent = abs(dir.x) + abs(dir.y);
                return dot(uv - 0.5, dir) / extent + 0.5;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                #if defined(_USE_STROKE_ORDER)
                    float t = SAMPLE_TEXTURE2D(_StrokeOrderTex, sampler_StrokeOrderTex, input.uv).r;
                #else
                    float t = StrokeCoordinate(input.uv);
                #endif

                float2 noiseUV = input.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw + float2(input.color.b * 7.31, input.color.b * 3.17);
                float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                float halfNoise = _EdgeNoise * 0.5;
                t += (n - 0.5) * _EdgeNoise;

                // Remap so progress 0 is fully hidden and 1 is fully shown regardless of noise and softness.
                float s = _EdgeSoftness;
                float reveal = lerp(-halfNoise - s, 1.0 + halfNoise, input.color.r);
                float erase = lerp(-halfNoise - s, 1.0 + halfNoise, input.color.g);
                float shown = smoothstep(t - s, t, reveal) * (1.0 - smoothstep(t - s, t, erase));

                half alpha = tex.a * _Color.a * input.color.a * shown;
                clip(alpha - 0.001);
                return half4(tex.rgb * _Color.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
