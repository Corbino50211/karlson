// MOMENTUM - world-space "prototype grid" surface shader for level architecture.
// Grid lines are computed from world position (triplanar), so any scaled box keeps a
// consistent 1 m grid. Supports color, emission and smoothness like the Standard shader.
Shader "Momentum/WorldGrid"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.9, 0.9, 0.92, 1)
        _LineColor ("Line Color", Color) = (0.62, 0.64, 0.68, 1)
        _MajorLineColor ("Major Line Color", Color) = (0.45, 0.47, 0.52, 1)
        _GridSize ("Grid Size (m)", Float) = 1
        _MajorEvery ("Major Line Every N", Float) = 4
        _LineWidth ("Line Width", Range(0.001, 0.2)) = 0.025
        _Glossiness ("Smoothness", Range(0, 1)) = 0.15
        _Metallic ("Metallic", Range(0, 1)) = 0
        _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        fixed4 _Color;
        fixed4 _LineColor;
        fixed4 _MajorLineColor;
        float _GridSize;
        float _MajorEvery;
        float _LineWidth;
        half _Glossiness;
        half _Metallic;
        fixed4 _EmissionColor;

        // Returns 1 on a grid line, 0 elsewhere, anti-aliased with screen-space derivatives.
        float GridLine(float2 coord, float width)
        {
            float2 cell = abs(frac(coord - 0.5) - 0.5);
            float2 fw = max(fwidth(coord), 1e-4);
            float2 lines = 1.0 - smoothstep(width - fw, width + fw, cell);
            return saturate(max(lines.x, lines.y));
        }

        float PlaneGrid(float2 p)
        {
            float minor = GridLine(p / max(_GridSize, 0.01), _LineWidth);
            float major = GridLine(p / max(_GridSize * _MajorEvery, 0.01), _LineWidth / max(_MajorEvery, 1.0) * 1.5);
            return max(minor * 0.6, major);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = abs(normalize(IN.worldNormal));
            float3 w = n / max(n.x + n.y + n.z, 1e-4);
            w = pow(w, 4.0);
            w /= max(w.x + w.y + w.z, 1e-4);

            float gx = PlaneGrid(IN.worldPos.zy);
            float gy = PlaneGrid(IN.worldPos.xz);
            float gz = PlaneGrid(IN.worldPos.xy);
            float g = gx * w.x + gy * w.y + gz * w.z;

            fixed3 lineCol = lerp(_LineColor.rgb, _MajorLineColor.rgb, step(0.99, g));
            fixed3 albedo = lerp(_Color.rgb, lineCol * _Color.rgb * 1.1, saturate(g));

            o.Albedo = albedo;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = _EmissionColor.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
