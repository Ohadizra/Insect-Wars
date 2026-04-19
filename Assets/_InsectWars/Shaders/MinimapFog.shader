Shader "InsectWars/MinimapFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            sampler2D _IW_FogWarTex;
            float _IW_FogActive;

            static const fixed3 ShroudColor = fixed3(0.012, 0.014, 0.028);
            static const fixed3 ExploredTint = fixed3(0.38, 0.42, 0.52);

            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color    = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 scene = tex2D(_MainTex, i.texcoord) * i.color;

                if (_IW_FogActive < 0.5)
                    return scene;

                fixed2 fog = tex2D(_IW_FogWarTex, i.texcoord).rg;

                float noise = hash12(i.texcoord * 200.0) * 0.12;

                fixed visFactor = smoothstep(0.10 + noise, 0.35 + noise, fog.g);
                fixed expFactor = smoothstep(0.08 + noise, 0.30 + noise, fog.r);

                fixed3 explored = scene.rgb * ExploredTint;
                fixed3 col = lerp(ShroudColor, explored, expFactor);
                col = lerp(col, scene.rgb, visFactor);

                return fixed4(col, scene.a);
            }
            ENDCG
        }
    }
}
