Shader "Teaching/Outline_Safe"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.05)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        // ---------- OUTLINE PASS ----------
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            float  _OutlineWidth;
            float4 _OutlineColor;

            v2f vert (appdata v)
            {
                v2f o;

                float3 normal = normalize(v.normal);
                float4 displaced = v.vertex;
                displaced.xyz += normal * _OutlineWidth;

                o.position = UnityObjectToClipPos(displaced);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Clamp to guarantee GI safety
                float3 color = saturate(_OutlineColor.rgb);
                float  alpha = saturate(_OutlineColor.a);

                return float4(color, alpha);
            }
            ENDHLSL
        }

        // ---------- BASE PASS ----------
        Pass
        {
            Name "BASE"
            Cull Back
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);

                float3 color = tex.rgb * _Color.rgb;
                color = saturate(color);

                float alpha = saturate(tex.a * _Color.a);

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
