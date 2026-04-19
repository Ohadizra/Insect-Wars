Shader "Hidden/InsectWars/FogOfWarComposite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Name "FogOfWarComposite"
            ZTest Always
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _SCREEN_COORD_OVERRIDE
            #pragma multi_compile _ UNITY_STEREO_INSTANCING_ENABLED

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_IW_FogWarTex);
            SAMPLER(sampler_IW_FogWarTex);
            float4 _IW_FogBounds;
            float _IW_FogActive;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_IW_FogActive < 0.5h)
                    return scene;

                float depth = SampleSceneDepth(uv);
#if UNITY_REVERSED_Z
                if (depth <= 1.0e-4)
                    return scene;
#else
                if (depth >= 1.0 - 1.0e-4)
                    return scene;
#endif

                float3 wpos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float2 xz = wpos.xz;
                float2 fuv = (xz - _IW_FogBounds.xy) * _IW_FogBounds.zw;
                if (fuv.x < -0.05 || fuv.x > 1.05 || fuv.y < -0.05 || fuv.y > 1.05)
                    return scene;

                // R = explored (memory), G = current vision. Bilinear soft edges → use lower thresholds.
                half2 fog = SAMPLE_TEXTURE2D(_IW_FogWarTex, sampler_IW_FogWarTex, saturate(fuv)).rg;
                half vis = fog.g;
                half exp = fog.r;
                // 1) Full vision — player sees terrain + units here
                if (vis > 0.22h)
                    return scene;
                // 2) Explored but no vision — terrain dimmed; enemies culled in C#
                if (exp > 0.18h)
                    return half4(scene.rgb * half3(0.48h, 0.5h, 0.58h), scene.a);
                // 3) Shroud — never explored: fully opaque black (SC2-style)
                return half4(0.0h, 0.0h, 0.0h, scene.a);
            }
            ENDHLSL
        }
    }
}
