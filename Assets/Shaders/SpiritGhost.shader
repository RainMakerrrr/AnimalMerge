Shader "Voodoo_LaunchOps/FX/SpiritGhost"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.24, 0.62, 0.88, 1)
        _RimColor  ("Rim Color", Color) = (0.70, 0.94, 1.00, 1)
        _IridescenceA ("Iridescence A", Color) = (0.15, 0.85, 1.00, 1)
        _IridescenceB ("Iridescence B", Color) = (0.62, 0.42, 1.00, 1)

        _Alpha ("Alpha", Range(0, 1)) = 0.72
        _RimAlphaBoost ("Rim Alpha Boost", Range(0, 2)) = 0.5
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _RimGlow ("Rim Glow", Range(0, 4)) = 1.1

        _IridescenceAmount ("Iridescence Amount", Range(0, 1)) = 0.5
        _IridescenceViewShift ("Iridescence View Shift", Range(0, 2)) = 0.6

        _SweepAxis ("Sweep Axis (Object Space)", Vector) = (0, 1, 0, 0)
        _SweepAxisExtent ("Object Size Along Sweep Axis", Float) = 1
        _SweepTiling ("Bands Per Object", Float) = 1
        _SweepSpeed ("Sweep Speed", Float) = 0.7
        _SweepWidth ("Sweep Width (Fraction Of Band Spacing)", Range(0.01, 1)) = 0.35
        _SweepIntensity ("Sweep Intensity", Range(0, 4)) = 0.8

        _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"                = "Transparent"
            "RenderType"           = "Transparent"
            "IgnoreProjector"      = "True"
            "ForceNoShadowCasting" = "True"
            "DisableBatching"      = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
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
                float3 normalWS   : TEXCOORD0;
                float3 viewWS     : TEXCOORD1;
                float  sweepCoord : TEXCOORD2;
            };

            fixed4 _BaseColor;
            fixed4 _RimColor;
            fixed4 _IridescenceA;
            fixed4 _IridescenceB;

            float _Alpha;
            float _RimAlphaBoost;
            float _RimPower;
            float _RimGlow;

            float _IridescenceAmount;
            float _IridescenceViewShift;

            float4 _SweepAxis;
            float  _SweepAxisExtent;
            float  _SweepTiling;
            float  _SweepSpeed;
            float  _SweepWidth;
            float  _SweepIntensity;

            float _Fade;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normalWS = UnityObjectToWorldNormal(v.normal);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewWS = _WorldSpaceCameraPos - worldPos;

                float3 axis = _SweepAxis.xyz;
                float axisLen = max(length(axis), 1e-4);
                axis /= axisLen;

                o.sweepCoord = dot(v.vertex.xyz, axis) / max(_SweepAxisExtent, 1e-4);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);

                float fresnel = pow(1.0 - saturate(dot(n, v)), _RimPower);

                float phase = i.sweepCoord * _SweepTiling - _Time.y * _SweepSpeed;
                float band = 1.0 - smoothstep(0.0, _SweepWidth, abs(frac(phase) - 0.5) * 2.0);

                float shift = saturate(fresnel * _IridescenceViewShift);
                float iridT = 0.5 + 0.5 * cos(6.2831853 * shift);
                float3 iridescence = lerp(_IridescenceA.rgb, _IridescenceB.rgb, iridT);

                float3 fill = lerp(_BaseColor.rgb, _RimColor.rgb, fresnel);
                fill = lerp(fill, fill * iridescence * 2.0, _IridescenceAmount);
                fill += _RimColor.rgb * fresnel * _RimGlow;
                fill += iridescence * band * _SweepIntensity;

                float alpha = saturate(_Alpha + fresnel * _RimAlphaBoost + band * _SweepIntensity * 0.12);

                return fixed4(fill, alpha * _Fade);
            }
            ENDCG
        }
    }

    Fallback Off
}
