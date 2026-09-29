// A star on the galaxy map: churning granulation, limb darkening, hot HDR core.
Shader "GR/Star"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (1, 0.8, 0.6, 1)
        _Intensity ("Intensity", Float) = 3
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; float3 os : TEXCOORD2; };

            Varyings vert(Attributes a)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(a.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.n = TransformObjectToWorldNormal(a.normalOS);
                o.v = GetWorldSpaceViewDir(ws);
                o.os = a.positionOS.xyz;
                return o;
            }

            float hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float noise(float3 x)
            {
                float3 i = floor(x), f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash(i), hash(i + float3(1, 0, 0)), f.x), lerp(hash(i + float3(0, 1, 0)), hash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0, 0, 1)), hash(i + float3(1, 0, 1)), f.x), lerp(hash(i + float3(0, 1, 1)), hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float mu = saturate(dot(normalize(i.n), normalize(i.v)));
                float t = _Time.y * 0.15;
                float3 q = i.os * 9.0;
                float gran = noise(q + t) * 0.55 + noise(q * 2.3 - t * 1.7) * 0.3 + noise(q * 5.1 + t * 2.0) * 0.15;
                float limb = 0.35 + 0.65 * pow(mu, 0.45);
                float3 c = _Color.rgb * (0.7 + 0.6 * gran) * limb * _Intensity;
                c += float3(1, 0.95, 0.9) * pow(mu, 6.0) * _Intensity * 0.5;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
