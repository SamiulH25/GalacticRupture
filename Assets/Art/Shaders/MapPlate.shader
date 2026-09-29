// The galaxy map's plotting plate: territory as a soft fill with glowing borders between powers,
// unexplored space hatched and dimmed, a faint plotting grid and dust, and an optional supply
// overlay. Systems arrive as arrays from MapView each frame.
Shader "GR/MapPlate"
{
    Properties
    {
        _Radius ("Territory radius", Float) = 850
        _Border ("Border width", Float) = 22
        _Grid ("Grid spacing", Float) = 500
        _SupplyRange ("Supply range", Float) = 1400
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-50" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Radius, _Border, _Grid, _SupplyRange;
            CBUFFER_END

            float4 _GR_Systems[32];  // xz position, owner (-1 none), visible (0/1)
            float4 _GR_SysFlags[32]; // x: supply source for the player, y: capital
            float4 _GR_Colors[8];
            float _GR_SystemCount;
            float _GR_ShowSupply;
            float4 _GR_MapCentre;    // xz centre, w radius

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.world);
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }
            float fbm(float2 p)
            {
                float s = 0.0, a = 0.5;
                for (int i = 0; i < 5; i++) { s += a * noise(p); p = p * 2.07 + 13.1; a *= 0.5; }
                return s;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.world.xz;
                int n = (int)_GR_SystemCount;

                // Each power's territory is a smooth union of its systems (a soft minimum of the
                // distances), so borders flow between systems instead of reading as circles.
                const float k = 1.0 / 240.0;
                float acc[8];
                for (int f = 0; f < 8; f++) acc[f] = 0.0;
                float visW = 0.0, visSum = 0.0, dSupply = 1e9;
                for (int j = 0; j < 32; j++)
                {
                    if (j >= n) break;
                    float4 s = _GR_Systems[j];
                    float d = distance(p, s.xy);
                    float w = exp(-d / 350.0);
                    visW += w;
                    visSum += w * s.w;
                    if (_GR_SysFlags[j].x > 0.5) dSupply = min(dSupply, d);
                    if (s.z >= 0.0) acc[(int)s.z] += exp(-d * k);
                }
                float vis = visW > 1e-6 ? visSum / visW : 0.0;
                float d1 = 1e9, d2 = 1e9;
                int o1 = -1;
                for (int f2 = 0; f2 < 8; f2++)
                {
                    if (acc[f2] <= 0.0) continue;
                    float d = -log(acc[f2]) / k;
                    if (d < d1) { d2 = d1; d1 = d; o1 = f2; }
                    else if (d < d2) d2 = d;
                }

                float3 col = 0;
                float alpha = 0;

                // Dust and a plotting grid, fading out at the edge of the map.
                float2 c = _GR_MapCentre.xy;
                float edge = 1.0 - smoothstep(_GR_MapCentre.w * 0.7, _GR_MapCentre.w, distance(p, c));
                float dust = fbm(p * 0.00045 + 3.7);
                col += float3(0.32, 0.26, 0.5) * pow(dust, 3.0) * 0.14 * edge;
                float2 g = abs(frac(p / _Grid) - 0.5) * _Grid;
                float grid = 1.0 - smoothstep(0.0, 4.0, min(g.x, g.y));
                col += float3(0.4, 0.55, 0.8) * grid * 0.035 * edge;
                alpha += grid * 0.02 * edge;

                if (o1 >= 0 && d1 < _Radius)
                {
                    float3 fc = _GR_Colors[o1].rgb;
                    // Fill: faint in the heart of a territory, stronger toward its edge.
                    float fill = lerp(0.035, 0.13, smoothstep(_Radius * 0.25, _Radius, d1));
                    // Borders: between powers (Voronoi edge) and at the frontier.
                    float between = d2 < 1e8 ? 1.0 - smoothstep(0.0, _Border, (d2 - d1) * 0.5) : 0.0;
                    float frontier = 1.0 - smoothstep(0.0, _Border, abs(d1 - _Radius));
                    float edgeLine = max(between, frontier);
                    float glow = (1.0 - smoothstep(0.0, _Border * 5.0, min((d2 - d1) * 0.5, abs(d1 - _Radius)))) * 0.25;
                    col += fc * (fill + edgeLine * 0.9 + glow * 0.35);
                    alpha += fill * 0.6 + edgeLine * 0.3;
                }

                // Unwatched space: dimmed, with a light hatch, fading in softly.
                float fog = 1.0 - smoothstep(0.25, 0.75, vis);
                float hatch = 1.0 - smoothstep(0.0, 6.0, abs(frac((p.x + p.y) / 90.0) - 0.5) * 90.0);
                col *= lerp(1.0, 0.5, fog);
                col += float3(0.25, 0.25, 0.32) * hatch * 0.05 * edge * fog;
                alpha = max(alpha, 0.18 * edge * fog);

                if (_GR_ShowSupply > 0.5)
                {
                    float inside = 1.0 - smoothstep(_SupplyRange - 30.0, _SupplyRange, dSupply);
                    float ring = 1.0 - smoothstep(0.0, 10.0, abs(dSupply - _SupplyRange));
                    col += float3(0.3, 0.9, 1.0) * (inside * 0.03 + ring * 0.5);
                    col += float3(1.0, 0.3, 0.2) * (1.0 - inside) * 0.02 * edge;
                }

                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
