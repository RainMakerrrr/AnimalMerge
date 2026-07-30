Shader "Voodoo_LaunchOps/GridCellBorder"
{
    Properties
    {
        _Color     ("Fill Color", Color) = (0.7, 0.7, 0.7, 0.15)
        _LineColor ("Line Color", Color) = (1, 1, 1, 0.75)
        _LineWidth ("Seam Line Width (world units)", Range(0, 0.5)) = 0.04
        _Softness  ("Edge Softness", Range(0.01, 4)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
            // Batching pre-transforms vertices to world space and leaves unity_ObjectToWorld
            // as identity, which would break the world-unit line width below.
            // This does NOT disable GPU instancing.
            "DisableBatching" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float2 scale : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            float4 _LineColor;
            float  _LineWidth;
            float  _Softness;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;

                // Column lengths of the object-to-world matrix give the quad's world extent
                // along its local X / Y axes. Rotation-agnostic, so the quad can be oriented freely.
                o.scale = float2(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Distance from this fragment to the nearest quad edge, in world units.
                // min() across both axes produces a correctly mitred corner.
                float2 toEdge = min(i.uv, 1.0 - i.uv) * i.scale;
                float  dist   = min(toEdge.x, toEdge.y);

                // Each cell insets HALF the width from its own edge, so two adjacent
                // cells add up to exactly _LineWidth at the shared seam.
                float halfWidth = _LineWidth * 0.5;
                float aa        = max(fwidth(dist) * _Softness, 1e-5);
                float lineMask  = 1.0 - smoothstep(halfWidth - aa, halfWidth + aa, dist);

                fixed4 fill = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                return lerp(fill, _LineColor, lineMask);
            }
            ENDCG
        }
    }

    Fallback Off
}
