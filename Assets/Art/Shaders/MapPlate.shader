// The galaxy map's plotting plate: territory as a soft fill with glowing borders between powers,
// unexplored space hatched and dimmed, a faint plotting grid and dust, and an optional supply
// overlay. Systems arrive as arrays from MapView. The territory, fog and dust are baked into
// textures (passes 1 and 2) only when what the player knows changes; the plate as drawn (pass 0)
// reads those and adds the crisp grid and hatching.
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _Radius, _Border, _Grid, _SupplyRange;
        CBUFFER_END

        float4 _GR_Systems[128];  // xz position, owner (-1 none), visible (0/1)
        float4 _GR_SysFlags[128]; // x: supply source for the player, y: capital
        float4 _GR_Colors[16];
        float _GR_SystemCount;
        float _GR_ShowSupply;
        float4 _GR_MapCentre;    // xz centre, w half the plate's span

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

        float Edge(float2 p)
        {
            return 1.0 - smoothstep(_GR_MapCentre.w * 0.7, _GR_MapCentre.w, distance(p, _GR_MapCentre.xy));
        }

        // How unwatched a point is: 0 seen, 1 unseen.
        float Fog(float2 p)
        {
            int n = (int)_GR_SystemCount;
            float visW = 0.0, visSum = 0.0;
            for (int j = 0; j < 128; j++)
            {
                if (j >= n) break;
                float4 s = _GR_Systems[j];
                float w = exp(-distance(p, s.xy) / 350.0);
                visW += w;
                visSum += w * s.w;
            }
            float vis = visW > 1e-6 ? visSum / visW : 0.0;
            return 1.0 - smoothstep(0.25, 0.75, vis);
        }

        // Dust, territory and supply, dimmed by the fog; a is the territory's own cover.
        float4 Territory(float2 p)
        {
            int n = (int)_GR_SystemCount;
            // Each power's territory is a smooth union of its systems (a soft minimum of the
            // distances), so borders flow between systems instead of reading as circles.
            const float k = 1.0 / 240.0;
            float acc[16];
            for (int f = 0; f < 16; f++) acc[f] = 0.0;
            float dSupply = 1e9;
            for (int j = 0; j < 128; j++)
            {
                if (j >= n) break;
                float4 s = _GR_Systems[j];
                float d = distance(p, s.xy);
                if (_GR_SysFlags[j].x > 0.5) dSupply = min(dSupply, d);
                if (s.z >= 0.0 && d < 4000.0) acc[(int)s.z] += exp(-d * k);
            }
            float d1 = 1e9, d2 = 1e9;
            int o1 = -1;
            for (int f2 = 0; f2 < 16; f2++)
            {
                if (acc[f2] <= 0.0) continue;
                float d = -log(acc[f2]) / k;
                if (d < d1) { d2 = d1; d1 = d; o1 = f2; }
                else if (d < d2) d2 = d;
            }

            float edge = Edge(p);
            float3 col = float3(0.32, 0.26, 0.5) * pow(fbm(p * 0.00045 + 3.7), 3.0) * 0.14 * edge;
            float alpha = 0;
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
            col *= lerp(1.0, 0.5, Fog(p));

            if (_GR_ShowSupply > 0.5)
            {
                float inside = 1.0 - smoothstep(_SupplyRange - 30.0, _SupplyRange, dSupply);
                float ring = 1.0 - smoothstep(0.0, 10.0, abs(dSupply - _SupplyRange));
                col += float3(0.3, 0.9, 1.0) * (inside * 0.03 + ring * 0.5);
                col += float3(1.0, 0.3, 0.2) * (1.0 - inside) * 0.02 * edge;
            }
            return float4(col, alpha);
        }

        struct BakeAttributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
        struct BakeVaryings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; };

        BakeVaryings BakeVert(BakeAttributes v)
        {
            BakeVaryings o;
            o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
            o.world = _GR_MapCentre.xy + (v.uv - 0.5) * 2.0 * _GR_MapCentre.w;
            return o;
        }
        ENDHLSL

        Pass
        {
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_GR_PlateTex);
            SAMPLER(sampler_GR_PlateTex);
            TEXTURE2D(_GR_PlateFog);
            SAMPLER(sampler_GR_PlateFog);

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.world);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.world.xz;
                float2 uv = (p - _GR_MapCentre.xy) / (2.0 * _GR_MapCentre.w) + 0.5;
                float4 terr = SAMPLE_TEXTURE2D(_GR_PlateTex, sampler_GR_PlateTex, uv);
                float fog = SAMPLE_TEXTURE2D(_GR_PlateFog, sampler_GR_PlateFog, uv).r;
                float edge = Edge(p);

                // A faint plotting grid, and a light hatch over unwatched space.
                float2 g = abs(frac(p / _Grid) - 0.5) * _Grid;
                float grid = 1.0 - smoothstep(0.0, 4.0, min(g.x, g.y));
                float hatch = 1.0 - smoothstep(0.0, 6.0, abs(frac((p.x + p.y) / 90.0) - 0.5) * 90.0);
                float3 col = terr.rgb + float3(0.4, 0.55, 0.8) * grid * 0.035 * edge * lerp(1.0, 0.5, fog)
                           + float3(0.25, 0.25, 0.32) * hatch * 0.05 * edge * fog;
                float alpha = max(terr.a + grid * 0.02 * edge, 0.18 * edge * fog);
                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }

        // Bake: territory (Graphics.Blit, pass 1). The LightMode keeps the renderer from
        // drawing it with the plate.
        Pass
        {
            Tags { "LightMode" = "GRBake" }
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex BakeVert
            #pragma fragment frag
            float4 frag(BakeVaryings i) : SV_Target { return Territory(i.world); }
            ENDHLSL
        }

        // Bake: fog (pass 2).
        Pass
        {
            Tags { "LightMode" = "GRBake" }
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex BakeVert
            #pragma fragment frag
            float4 frag(BakeVaryings i) : SV_Target { return Fog(i.world).xxxx; }
            ENDHLSL
        }
    }
}
