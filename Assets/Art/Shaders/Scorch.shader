// Burn marks on hulls: dark, alpha-blended, with a fading hot core.
Shader "GR/Scorch"
{
    Properties
    {
        _MainTex ("Mask", 2D) = "white" {}
        _Color ("Soot", Color) = (0.02, 0.016, 0.012, 0.9)
        [HDR] _Hot ("Hot core", Color) = (4, 1.1, 0.25, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Offset -1, -1

        Pass
        {
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
                float4 _Hot;
            CBUFFER_END

            float _Heat; // per renderer: 1 when fresh, fades to 0

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float m = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r;
                float core = pow(m, 4.0) * _Heat;
                float3 c = _Color.rgb + _Hot.rgb * core;
                return half4(c, saturate(m * 1.6) * _Color.a);
            }
            ENDHLSL
        }
    }
}
