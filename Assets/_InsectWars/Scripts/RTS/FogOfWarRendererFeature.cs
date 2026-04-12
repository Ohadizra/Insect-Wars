using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace InsectWars.RTS
{
    /// <summary>
    /// Fullscreen fog-of-war composite (shroud / explored / visible). Requires depth texture on the URP asset.
    /// </summary>
    public class FogOfWarRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] Material fogMaterial;
        [SerializeField] RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

        FogOfWarPass _pass;
        static Material s_runtimeMat;

        public override void Create()
        {
            _pass = new FogOfWarPass();
            _pass.renderPassEvent = injectionPoint;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cam = renderingData.cameraData.camera;
            if (cam == null || !cam.CompareTag("MainCamera"))
                return;

            var mat = fogMaterial;
            if (mat == null)
            {
                if (s_runtimeMat == null)
                {
                    var sh = Shader.Find("Hidden/InsectWars/FogOfWarComposite");
                    if (sh != null)
                        s_runtimeMat = new Material(sh) { name = "FogOfWarRuntime" };
                }
                mat = s_runtimeMat;
            }
            if (mat == null) return;

            _pass.Setup(mat);
            renderer.EnqueuePass(_pass);
        }

        sealed class FogOfWarPass : ScriptableRenderPass
        {
            const string PassName = "InsectWars FogOfWar";
            Material _mat;

            public FogOfWarPass()
            {
                profilingSampler = new ProfilingSampler(PassName);
            }

            public void Setup(Material m)
            {
                _mat = m;
                ConfigureInput(ScriptableRenderPassInput.Depth);
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_mat == null) return;
                if (Shader.GetGlobalFloat(Shader.PropertyToID("_IW_FogActive")) < 0.5f)
                    return;
                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                var source = resourceData.activeColorTexture;
                var destinationDesc = renderGraph.GetTextureDesc(source);
                destinationDesc.name = "FogOfWarColor";
                destinationDesc.clearBuffer = false;
                var destination = renderGraph.CreateTexture(destinationDesc);

                var para = new RenderGraphUtils.BlitMaterialParameters(source, destination, _mat, 0);
                renderGraph.AddBlitPass(para, PassName);
                resourceData.cameraColor = destination;
            }
        }
    }
}
