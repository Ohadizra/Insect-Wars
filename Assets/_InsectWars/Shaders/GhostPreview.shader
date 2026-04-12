Shader "InsectWars/GhostPreview"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0, 1, 0, 0.4)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _LineSpeed ("Line Speed", Range(0, 10)) = 2.0
        _LineDensity ("Line Density", Range(1, 50)) = 10.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            float4 _BaseColor;
            float _RimPower;
            float _LineSpeed;
            float _LineDensity;

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 frag (Varyings input) : SV_Target
            {
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normal = normalize(input.normalWS);
                
                // Fresnel / Rim
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _RimPower);
                
                // Scrolling lines
                float lines = sin(input.positionWS.y * _LineDensity + _Time.y * _LineSpeed) * 0.5 + 0.5;
                lines = step(0.8, lines) * 0.3;
                
                float4 col = _BaseColor;
                col.rgb += rim * _BaseColor.rgb * 2.0;
                col.rgb += lines * _BaseColor.rgb;
                col.a = saturate(col.a + rim + lines);
                
                return col;
            }
            ENDHLSL
        }
    }
}
