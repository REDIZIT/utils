using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace InGame.UI
{
    public class CustomCanvasFeature : ScriptableRendererFeature
    {
        private CustomCanvasPass _canvasPass;

        public override void Create()
        {
            _canvasPass = new CustomCanvasPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview || 
                renderingData.cameraData.cameraType == CameraType.Reflection)
                return;

            // Теперь проверяем единственный синглтон-инстанс
            if (CustomCanvas.Instance == null) return;

            renderer.EnqueuePass(_canvasPass);
        }

        private class CustomCanvasPass : ScriptableRenderPass
        {
            private class PassData
            {
                public Matrix4x4 ProjectionMatrix;
            }

            public CustomCanvasPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                float screenW = cameraData.camera.pixelWidth;
                float screenH = cameraData.camera.pixelHeight;

                Matrix4x4 proj = Matrix4x4.Ortho(0, screenW, 0, screenH, -1f, 1f);
                proj = GL.GetGPUProjectionMatrix(proj, false);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("CustomCanvas_Scene_Pass", out var passData))
                {
                    passData.ProjectionMatrix = proj;
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetViewProjectionMatrices(Matrix4x4.identity, data.ProjectionMatrix);

                        var canvas = CustomCanvas.Instance;
                        if (canvas == null) return;

                        var batches = canvas.context.batches;
                        for (int b = 0; b < batches.Count; b++)
                        {
                            var batch = batches[b];
                            if (batch.verts.Count == 0 || batch.material == null || batch.mesh == null)
                                continue;

                            context.cmd.DrawMesh(batch.mesh, Matrix4x4.identity, batch.material, 0, 0);
                        }
                    });
                }
            }
        }
    }
}