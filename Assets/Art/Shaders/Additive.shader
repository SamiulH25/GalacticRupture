// Unlit HDR additive: particles, muzzle flashes, engine plumes, tracers.
// _Mode 0 samples _MainTex; _Mode 1 draws a tracer procedurally (u across, v tail->head).
Shader "GR/Additive"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Colour", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Float) = 1
        [Enum(Texture, 0, Tracer, 1)] _Mode ("Mode", Float) = 0
        _Softness ("Tracer core sharpness", Float) = 3
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Additive"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Intensity;
                float _Mode;
                float _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float a;
                if (_Mode > 0.5)
                {
                    float across = 1.0 - abs(i.uv.x * 2.0 - 1.0);
                    float core = pow(across, _Softness);
                    float along = smoothstep(0.0, 1.0, i.uv.y);
                    float head = smoothstep(0.85, 1.0, i.uv.y) * (1.0 - smoothstep(0.97, 1.0, i.uv.y));
                    a = core * (along * along + head * 1.5);
                }
                else
                {
                    a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r;
                }
                float3 c = _Color.rgb * i.color.rgb * (a * i.color.a * _Intensity);
                // Never let a bad input become an infinity that bloom smears across the screen.
                if (any(isnan(c))) c = 0;
                return half4(clamp(c, 0.0, 500.0), 1.0);
            }
            ENDHLSL
        }
    }
}
