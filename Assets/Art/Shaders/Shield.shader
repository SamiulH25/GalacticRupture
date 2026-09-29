// Shield field: an ellipsoid shell that is invisible at rest and shows where it is hit.
// Up to 8 recent hits (object-space position, age in seconds) drive expanding ripples.
// _Strength (0..1) is the shield's remaining fraction: a weak shield flickers and reads thin.
Shader "GR/Shield"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (0.35, 0.7, 1.4, 1)
        _Rim ("Rim glow while active", Float) = 0.35
        _RippleSpeed ("Ripple speed (m/s)", Float) = 60
        _RippleWidth ("Ripple width (m)", Float) = 5
        _HitRadius ("Hit flare radius (m)", Float) = 9
        _HexScale ("Hex cells per metre", Float) = 0.35
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Shield"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Rim, _RippleSpeed, _RippleWidth, _HitRadius, _HexScale;
            CBUFFER_END

            // Per renderer, through a MaterialPropertyBlock.
            float4 _Hits[8];    // xyz object space, w age (s); w < 0 = unused
            float _Strength;
            float _Activity;    // 0..1, how recently the shield was struck at all

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.positionOS = v.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(ws);
                o.worldPos = ws;
                return o;
            }

            float hexEdge(float2 p)
            {
                // Distance to the nearest hex cell edge, 0 at the edge.
                const float2 s = float2(1.0, 1.7320508);
                float4 hc = floor(float4(p, p - float2(0.5, 1.0)) / s.xyxy) + 0.5;
                float4 h = float4(p - hc.xy * s, p - (hc.zw + 0.5) * s);
                float2 q = dot(h.xy, h.xy) < dot(h.zw, h.zw) ? h.xy : h.zw;
                q = abs(q);
                return 0.5 - max(dot(q, s * 0.5), q.x);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                float fres = pow(1.0 - saturate(abs(dot(n, v))), 3.0);

                // World-scale distances: object space is scaled, so measure in world units.
                float glow = 0.0;
                float ripple = 0.0;
                [unroll] for (int k = 0; k < 8; k++)
                {
                    float4 h = _Hits[k];
                    if (h.w < 0.0) continue;
                    float3 hitWS = TransformObjectToWorld(h.xyz);
                    float d = distance(i.worldPos, hitWS);
                    float fade = saturate(1.0 - h.w / 1.1);
                    fade *= fade;
                    glow += exp(-(d * d) / (_HitRadius * _HitRadius)) * fade * 2.5;
                    float r = h.w * _RippleSpeed;
                    float x = (d - r) / _RippleWidth;
                    ripple += exp(-x * x) * fade;
                }

                float3 p = i.positionOS * 40.0;
                float2 uv = float2(atan2(i.positionOS.x, i.positionOS.z) * 18.0, i.positionOS.y * 30.0);
                float hex = 1.0 - smoothstep(0.0, 0.06, hexEdge(uv * _HexScale * 6.0));

                float weak = lerp(1.0, 0.55 + 0.45 * sin(_Time.y * 37.0 + p.x), step(_Strength, 0.25));
                float body = fres * _Rim * _Activity + ripple * (0.6 + hex * 0.8) + glow;
                float3 c = _Color.rgb * body * weak * (0.35 + 0.65 * _Strength);
                return half4(max(c, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
}
