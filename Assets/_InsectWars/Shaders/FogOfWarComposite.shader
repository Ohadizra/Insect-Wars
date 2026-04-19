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

            static const half3 ShroudColor = half3(0.012h, 0.014h, 0.028h);
            static const half3 ExploredTint = half3(0.38h, 0.42h, 0.52h);

            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

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

                half2 fog = SAMPLE_TEXTURE2D(_IW_FogWarTex, sampler_IW_FogWarTex, saturate(fuv)).rg;
                half vis = fog.g;
                half exp = fog.r;

                float noise = hash12(fuv * 200.0) * 0.12;

                half visFactor = smoothstep(0.10h + noise, 0.35h + noise, vis);
                half expFactor = smoothstep(0.08h + noise, 0.30h + noise, exp);

                half3 explored = scene.rgb * ExploredTint;
                half3 col = lerp(ShroudColor, explored, expFactor);
                col = lerp(col, scene.rgb, visFactor);

                return half4(col, scene.a);
            }
            ENDHLSL
        }
    }
}
