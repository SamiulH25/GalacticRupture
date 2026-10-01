// Procedural deep-space backdrop: three star layers, a galactic band, nebula with dust lanes,
// and the system's star. _SunDir is set globally from the key light (SkyController).
Shader "GR/SpaceSkybox"
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
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off

        HLSLINCLUDE
        #include "SpaceSky.hlsl"
        ENDHLSL

        // The sky as drawn. With GR_SKY_BAKED (set by SkyController in play) the nebula comes
        // from a lat-long texture baked once (Hidden/GR/SkyBake), instead of ~20 octaves of
        // noise per pixel.
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ GR_SKY_BAKED

            TEXTURE2D(_GR_SkyNebula);
            SAMPLER(sampler_GR_SkyNebula);

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 dir = normalize(i.dir);
            #if defined(GR_SKY_BAKED)
                // Straight up or down, atan2(0, 0) is NaN on some GPUs; a NaN in the sky poisons
                // the reflection probe and turns every lit surface black.
                float flat = length(dir.xz);
                float lon = flat > 1e-5 ? atan2(dir.x, dir.z) : 0.0;
                float2 uv = float2(lon / (2.0 * PI) + 0.5, asin(clamp(dir.y, -1.0, 1.0)) / PI + 0.5);
                float4 neb = max(SAMPLE_TEXTURE2D_LOD(_GR_SkyNebula, sampler_GR_SkyNebula, uv, 0), 0.0);
            #else
                float4 neb = Nebula(dir);
            #endif
                float3 col = neb.rgb + Stars(dir, neb.a) + Sun(dir);
                col = max(col, 0.0);
                return half4(col * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
