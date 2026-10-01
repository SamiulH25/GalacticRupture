// Shared by GR/SpaceSkybox and Hidden/GR/SkyBake: the procedural sky.
#ifndef GR_SPACE_SKY
#define GR_SPACE_SKY
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float _StarDensity, _StarBrightness;
    float4 _NebulaA, _NebulaB, _NebulaC;
    float _NebulaIntensity;
    float4 _NebulaSeed, _BandNormal;
    float _BandWidth;
    float4 _SunColor;
    float _SunSize, _SunIntensity, _Exposure;
CBUFFER_END

float3 _GR_SunDir; // direction toward the star, world space

float3 hash33(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx);
}

float hash13(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

float noise3(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0 - 2.0 * f);
    float n000 = hash13(i);
    float n100 = hash13(i + float3(1, 0, 0));
    float n010 = hash13(i + float3(0, 1, 0));
    float n110 = hash13(i + float3(1, 1, 0));
    float n001 = hash13(i + float3(0, 0, 1));
    float n101 = hash13(i + float3(1, 0, 1));
    float n011 = hash13(i + float3(0, 1, 1));
    float n111 = hash13(i + float3(1, 1, 1));
    return lerp(lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y),
                lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y), u.z);
}

float fbm(float3 p, int octaves)
{
    float a = 0.5, s = 0.0;
    for (int i = 0; i < octaves; i++)
    {
        s += a * noise3(p);
        p = p * 2.03 + float3(1.7, 9.2, 4.1);
        a *= 0.5;
    }
    return s;
}

float3 starColour(float t)
{
    // Cool red-orange through white to blue-white.
    float3 warm = float3(1.0, 0.72, 0.48);
    float3 white = float3(1.0, 0.97, 0.92);
    float3 blue = float3(0.7, 0.8, 1.0);
    return t < 0.5 ? lerp(warm, white, t * 2.0) : lerp(white, blue, (t - 0.5) * 2.0);
}

float3 starLayer(float3 dir, float scale, float density, float size)
{
    float3 p = dir * scale;
    float3 cell = floor(p);
    float3 f = frac(p);
    float3 r = hash33(cell);
    float present = step(1.0 - density, hash13(cell + 17.0));
    float d = length(f - (0.25 + 0.5 * r));
    float core = saturate(1.0 - d / size);
    core = core * core * core;
    float mag = pow(r.x, 6.0) * 4.0 + 0.25;
    return starColour(r.y) * core * mag * present;
}

float Band(float3 dir)
{
    float bandDist = dot(dir, normalize(_BandNormal.xyz));
    return exp(-(bandDist * bandDist) / (_BandWidth * _BandWidth));
}

// The expensive, soft part of the sky: nebula, dust and unresolved starlight. rgb is the
// light, a is how much the dust dims the stars in front of it. Baked once at runtime.
float4 Nebula(float3 dir)
{
    float band = Band(dir);
    float3 q = dir * 2.1 + _NebulaSeed.xyz;
    float warp = fbm(q * 1.3, 3);
    float n = fbm(q + warp * 1.6, 5);
    float n2 = fbm(dir * 4.3 + _NebulaSeed.zxy + warp, 4);
    float cloud = saturate(pow(saturate(n * 1.5 - 0.42), 2.4) * 3.0);
    float3 neb = lerp(_NebulaA.rgb, _NebulaB.rgb, saturate(n2 * 1.6 - 0.3));
    neb = lerp(neb, _NebulaC.rgb, saturate(pow(n2, 4.0) * 3.0) * cloud);
    float dust = smoothstep(0.42, 0.68, fbm(dir * 7.0 + _NebulaSeed.yzx, 4));
    float nebMask = cloud * (0.25 + band);
    float3 col = neb * nebMask * _NebulaIntensity * (1.0 - dust * 0.8);
    col += float3(0.55, 0.55, 0.62) * band * 0.035 * (0.6 + fbm(dir * 30.0, 3)) * (1.0 - dust * 0.9);
    return float4(col, dust);
}

float3 Stars(float3 dir, float dust)
{
    float band = Band(dir);
    float3 stars = 0;
    stars += starLayer(dir, 60.0, 0.05 * _StarDensity, 0.10) * 3.0;
    stars += starLayer(dir, 160.0, 0.18 * _StarDensity, 0.14);
    stars += starLayer(dir, 420.0, (0.18 + band * 0.55) * _StarDensity, 0.22) * 0.45;
    return stars * _StarBrightness * (1.0 - dust * 0.85);
}

float3 Sun(float3 dir)
{
    float3 sunDir = normalize(_GR_SunDir);
    float sd = dot(dir, sunDir);
    float radius = radians(_SunSize);
    float disc = smoothstep(cos(radius * 1.15), cos(radius), sd);
    float glow = pow(saturate(sd), 1400.0) * 3.0 + pow(saturate(sd), 90.0) * 0.35 + pow(saturate(sd), 8.0) * 0.04;
    return _SunColor.rgb * (disc * _SunIntensity + glow);
}
#endif
