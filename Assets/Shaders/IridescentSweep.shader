Shader "Voodoo_LaunchOps/FX/IridescentSweep"
{
    Properties
    {
        [HDR] _SweepColor  ("Sweep Color", Color) = (1, 1, 1, 1)
        [HDR] _SweepColorA ("Iridescence Color A", Color) = (0, 1, 1, 1)
        [HDR] _SweepColorB ("Iridescence Color B", Color) = (1, 0, 1, 1)

        _IridescenceAmount    ("Iridescence Amount", Range(0, 1)) = 0.6
        _IridescenceCycles    ("Iridescence Cycles", Range(0.25, 4)) = 1
        _IridescenceViewShift ("Iridescence View Shift", Range(0, 2)) = 0.5

        _SweepAxis       ("Sweep Axis (Object Space)", Vector) = (0, 1, 0, 0)
        _SweepAxisExtent ("Object Size Along Sweep Axis", Float) = 1
        _WorldSweepBlend ("Object To World Sweep Blend", Range(0, 1)) = 0
        _SweepTiling     ("Bands Per Object", Float) = 1
        _SweepSpeed      ("Sweep Speed", Float) = 0.6
        _SweepWidth      ("Sweep Width (Fraction Of Band Spacing)", Range(0.01, 1)) = 0.25
        _SweepIntensity  ("Sweep Intensity", Range(0, 20)) = 1.5
        _GlowSoftClip    ("Glow Soft Clip (0 = Hot White Core)", Range(0, 1)) = 1

        _SurfaceWeight ("Surface Weight", Range(0, 2)) = 0.35
        _RimWeight     ("Rim Weight", Range(0, 4)) = 1
        _RimPower      ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength   ("Constant Rim Strength", Range(0, 4)) = 0.15

        _PhaseOffset ("Phase Offset", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"                 = "Transparent+1"
            "RenderType"            = "Transparent"
            "IgnoreProjector"       = "True"
            "ForceNoShadowCasting"  = "True"
            "DisableBatching"       = "True"
        }

        Blend One One
        ZWrite Off
        ZTest LEqual
        Offset -1, -1
        Cull Back
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float  sweepCoord : TEXCOORD0;
                half3  worldNormal: TEXCOORD1;
                half3  viewDir    : TEXCOORD2;
            };

            half4  _SweepColor;
            half4  _SweepColorA;
            half4  _SweepColorB;

            half   _IridescenceAmount;
            float  _IridescenceCycles;
            half   _IridescenceViewShift;

            float4 _SweepAxis;
            float  _SweepAxisExtent;
            float  _WorldSweepBlend;
            float  _SweepTiling;
            float  _SweepSpeed;
            half   _SweepWidth;
            half   _SweepIntensity;
            half   _GlowSoftClip;

            half   _SurfaceWeight;
            half   _RimWeight;
            half   _RimPower;
            half   _RimStrength;

            float  _PhaseOffset;

            v2f vert (appdata v)
            {
                v2f o;

                float3 axisInput  = _SweepAxis.xyz;
                float3 objectAxis = normalize(dot(axisInput, axisInput) > 1e-6 ? axisInput : float3(0, 1, 0));

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 stretchedWorldAxis = mul((float3x3)unity_ObjectToWorld, objectAxis);
                float  worldUnitsPerObjectUnit = max(length(stretchedWorldAxis), 1e-4);
                float3 worldAxis = stretchedWorldAxis / worldUnitsPerObjectUnit;

                float objectSpan = max(_SweepAxisExtent, 1e-4);
                float objectSweepInObjectSpans = dot(v.vertex.xyz, objectAxis) / objectSpan;
                float worldSweepInObjectSpans  = dot(worldPos, worldAxis) / (objectSpan * worldUnitsPerObjectUnit);

                o.pos         = UnityObjectToClipPos(v.vertex);
                o.sweepCoord  = lerp(objectSweepInObjectSpans, worldSweepInObjectSpans, _WorldSweepBlend);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir     = _WorldSpaceCameraPos.xyz - worldPos;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float phase        = i.sweepCoord * _SweepTiling - _Time.y * _SweepSpeed + _PhaseOffset;
                float wrappedPhase = frac(phase);

                half distanceFromBandCenter = abs(wrappedPhase - 0.5) * 2.0;
                half safeSweepWidth = max(_SweepWidth, 1e-3);
                half band = 1.0 - smoothstep(0.0, safeSweepWidth, distanceFromBandCenter);

                half3 worldNormal = normalize(i.worldNormal);
                half3 viewDir     = normalize(i.viewDir);
                half  facing      = saturate(dot(worldNormal, viewDir));
                half  fresnel     = pow(1.0 - facing, _RimPower);

                float seamlessHueCycle = frac(phase * _IridescenceCycles + fresnel * _IridescenceViewShift);
                half  hue  = 0.5 + 0.5 * cos(6.2831853 * seamlessHueCycle);
                half3 tint = lerp(_SweepColor.rgb, lerp(_SweepColorA.rgb, _SweepColorB.rgb, hue), _IridescenceAmount);

                half unclampedGlow = band * (_SurfaceWeight + fresnel * _RimWeight) * _SweepIntensity
                                   + fresnel * _RimStrength;
                half rolledOffGlow = unclampedGlow / (1.0 + unclampedGlow);
                half glow          = lerp(unclampedGlow, rolledOffGlow, _GlowSoftClip);

                return half4(tint * glow, 0.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
