Shader "Teaching/ForceField_Safe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        // Non-HDR base colour (safe by default)
        _Color ("Color", Color) = (0.2, 0.6, 1.0, 1.0)

        // Controls brightness safely instead of HDR colour
        _Intensity ("Intensity", Range(0, 5)) = 1.0

        _FresnelPower ("Fresnel Power", Range(0.1, 8)) = 3.0
        _ScrollDirection ("Scroll Direction (XY)", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Lighting Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float  fresnel  : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _Color;
            float  _Intensity;
            float  _FresnelPower;
            float2 _ScrollDirection;

            v2f vert (appdata v)
            {
                v2f o;

                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv += _ScrollDirection * _Time.y;

                float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
                float  ndv = saturate(dot(viewDir, normalize(v.normal)));

                // Safe Fresnel (never negative, never >1)
                o.fresnel = pow(1.0 - ndv, _FresnelPower);

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);

                // Core colour calculation
                float3 color = tex.rgb * _Color.rgb * i.fresnel * _Intensity;

                // Explicit clamping — critical for GI safety
                color = saturate(color);

                // Alpha fades with fresnel for force-field feel
                float alpha = saturate(i.fresnel * _Color.a);

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
