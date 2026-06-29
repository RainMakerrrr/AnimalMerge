// Procedural cheetah-spot shader for the merge system.
//
// Why this exists: animals in this project are colored by incompatible systems
// (the leopard uses a UV-unwrapped Standard texture, the elephant uses the
// Malbers/Color4x3 flat-palette shader). Swapping the leopard material onto an
// elephant fails because the elephant mesh has no UV unwrap that can show the
// spotted texture. This shader sidesteps UVs entirely: it projects a procedural
// spot pattern using OBJECT-SPACE triplanar mapping, so the spots render on any
// mesh the cheetah merges into, and stay locked to the animal as it moves.
//
// Built-in Render Pipeline (CG surface shader). See Malbers/Color4x3 for the
// in-repo example of a built-in surface shader.
Shader "Malbers/CheetahSpotsTriplanar"
{
    Properties
    {
        [Header(Cheetah Colors)]
        _BaseColor ("Base Color (fur)", Color) = (0.85, 0.70, 0.45, 1)
        _SpotColor ("Spot Color", Color) = (0.12, 0.07, 0.03, 1)

        [Header(Spot Pattern)]
        _SpotScale ("Spot Scale (frequency)", Float) = 8
        _SpotDensity ("Spot Size", Range(0, 1)) = 0.42
        _SpotEdge ("Spot Edge Softness", Range(0.001, 0.5)) = 0.06
        _BlendSharpness ("Triplanar Blend Sharpness", Range(1, 16)) = 6

        [Header(Surface)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _Metallic ("Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0

        struct Input
        {
            float3 objPos;
            float3 objNormal;
        };

        half4 _BaseColor;
        half4 _SpotColor;
        float _SpotScale;
        float _SpotDensity;
        float _SpotEdge;
        float _BlendSharpness;
        half _Smoothness;
        half _Metallic;

        // Pass object-space position/normal to the fragment stage so the pattern
        // is independent of the mesh UVs and of the world transform.
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.objPos = v.vertex.xyz;
            o.objNormal = v.normal;
        }

        // Stable 2D hash -> feature point inside a cell, in [0,1].
        float2 Hash2(float2 p)
        {
            p = float2(dot(p, float2(127.1, 311.7)),
                       dot(p, float2(269.5, 183.3)));
            return frac(sin(p) * 43758.5453);
        }

        // Voronoi F1: distance to nearest feature point. Small near spot centers.
        float VoronoiF1(float2 uv)
        {
            float2 cell = floor(uv);
            float2 frc = frac(uv);
            float minDist = 8.0;

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    float2 lattice = float2(x, y);
                    float2 toFeature = lattice + Hash2(cell + lattice) - frc;
                    minDist = min(minDist, dot(toFeature, toFeature));
                }
            }
            return sqrt(minDist);
        }

        // 1 inside a spot (near a cell center), 0 in the gaps between spots.
        float SpotMask(float2 uv)
        {
            float d = VoronoiF1(uv);
            return 1.0 - smoothstep(_SpotDensity - _SpotEdge,
                                    _SpotDensity + _SpotEdge, d);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Triplanar weights from the object-space normal, sharpened so
            // diagonal faces don't show two overlapping spot patterns.
            float3 blend = pow(abs(normalize(IN.objNormal)), _BlendSharpness);
            blend /= (blend.x + blend.y + blend.z + 1e-5);

            float3 p = IN.objPos * _SpotScale;
            float spot = SpotMask(p.zy) * blend.x
                       + SpotMask(p.xz) * blend.y
                       + SpotMask(p.xy) * blend.z;
            spot = saturate(spot);

            o.Albedo = lerp(_BaseColor.rgb, _SpotColor.rgb, spot);
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = 1;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
