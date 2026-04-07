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

                // G = current vision, R = ever explored
                if (fog.g > 0.22)
                    return scene;
                if (fog.r > 0.18)
                    return fixed4(scene.rgb * fixed3(0.48, 0.5, 0.58), scene.a);

                return fixed4(0.02, 0.025, 0.04, scene.a);
            }
            ENDCG
        }
    }
}
