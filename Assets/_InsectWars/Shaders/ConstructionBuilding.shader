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
                // Local space height dissolve
                float3 positionLS = TransformWorldToObject(input.positionWS);
                
                // Assuming object height is roughly normalized around 0.5 center.
                // We use a range that ensures full visibility at 1.0 progress.
                float height = positionLS.y + 0.5; 
                
                // Noise dissolve for more "organic" building process
                float noise = frac(sin(dot(input.uv, float2(12.9898, 78.233))) * 43758.5453);
                float constructionThreshold = _ConstructionProgress * 1.2;
                float mask = smoothstep(constructionThreshold - 0.05, constructionThreshold, height - (noise * 0.05));
                
                float4 texCol = tex2D(_BaseMap, input.uv);
                float4 col = texCol * _BaseColor;
                
                // Built area (mask is near 0 for bottom)
                // Unbuilt area (mask is near 1 for top)
                float builtMask = 1.0 - mask;
                
                // Visibility staging:
                // Built: Full opacity (if texture has alpha)
                // Unbuilt: Very faint holographic ghost
                float alpha = lerp(0.02, col.a, builtMask);
                float3 finalRGB = lerp(col.rgb * 0.2, col.rgb, builtMask);
                
                // Growth edge glow
                float edge = abs(height - constructionThreshold);
                if (edge < _GlowWidth && _ConstructionProgress < 0.98 && _ConstructionProgress > 0.02) {
                    float glowIntensity = pow(1.0 - (edge / _GlowWidth), 2.0);
                    // Add a pulse to the glow
                    glowIntensity *= (sin(_Time.y * 10.0) * 0.2 + 0.8);
                    finalRGB += _GlowColor.rgb * glowIntensity * 3.0;
                    alpha = max(alpha, glowIntensity * 0.8);
                }

                return float4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
}
