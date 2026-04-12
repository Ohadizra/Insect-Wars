Shader "InsectWars/ConstructionBuilding"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _ConstructionProgress ("Construction Progress", Range(0, 1)) = 0
        _GlowColor ("Glow Color", Color) = (0.3, 0.7, 1, 1)
        _GlowWidth ("Glow Width", Range(0, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            sampler2D _BaseMap;
            float4 _BaseColor;
            float _ConstructionProgress;
            float4 _GlowColor;
            float _GlowWidth;

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag (Varyings input) : SV_Target
            {
                // Simple world-space Y dissolve
                // We'll use local space actually to make it consistent per object height
                float3 positionLS = TransformWorldToObject(input.positionWS);
                
                // Assuming object height is roughly 0 to 1 in local Y for simple visualization
                // Realistically we might need to pass bounds or just use a fixed range
                float height = positionLS.y + 0.5; // shift -0.5..0.5 to 0..1
                
                float mask = step(height, _ConstructionProgress * 1.5); // * 1.5 to ensure it fully appears
                
                float4 col = tex2D(_BaseMap, input.uv) * _BaseColor;
                
                // Darken/Transparent if above progress
                float alpha = lerp(0.2, col.a, mask);
                float3 finalRGB = col.rgb;
                
                if (mask < 1.0) {
                    finalRGB *= 0.4; // wireframe look
                }

                // Edge glow
                float edge = abs(height - _ConstructionProgress * 1.5);
                if (edge < _GlowWidth && _ConstructionProgress < 0.95) {
                    float glowIntensity = (1.0 - (edge / _GlowWidth)) * (1.0 - _ConstructionProgress);
                    finalRGB += _GlowColor.rgb * glowIntensity * 2.0;
                    alpha = max(alpha, glowIntensity);
                }

                return float4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
}
