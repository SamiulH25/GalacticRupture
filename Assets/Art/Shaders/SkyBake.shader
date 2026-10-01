// Bakes GR/SpaceSkybox's nebula into a lat-long texture (SkyController). Kept out of the sky
// shader itself: the skybox draws every pass it has.
Shader "Hidden/GR/SkyBake"
{
    Properties
    {
        _StarDensity ("Star density", Range(0, 1)) = 0.5
        _StarBrightness ("Star brightness", Float) = 2.5
        [HDR] _NebulaA ("Nebula colour A", Color) = (0.35, 0.12, 0.45, 1)
        [HDR] _NebulaB ("Nebula colour B", Color) = (0.05, 0.28, 0.42, 1)
        [HDR] _NebulaC ("Nebula colour C (hot)", Color) = (0.9, 0.38, 0.18, 1)
        _NebulaIntensity ("Nebula intensity", Float) = 0.55
        _NebulaSeed ("Nebula seed", Vector) = (13.1, 7.7, 3.3, 0)
        _BandNormal ("Galactic band normal", Vector) = (0.25, 0.9, 0.35, 0)
        _BandWidth ("Band width", Range(0.05, 1)) = 0.32
        [HDR] _SunColor ("Sun colour", Color) = (1.0, 0.86, 0.66, 1)
        _SunSize ("Sun angular radius (deg)", Float) = 0.9
        _SunIntensity ("Sun intensity", Float) = 40
        _Exposure ("Exposure", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        HLSLINCLUDE
        #include "SpaceSky.hlsl"
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float lon = (i.uv.x - 0.5) * 2.0 * PI;
                float lat = (i.uv.y - 0.5) * PI;
                float3 dir = float3(cos(lat) * sin(lon), sin(lat), cos(lat) * cos(lon));
                return Nebula(dir);
            }
            ENDHLSL
        }
    }
}
