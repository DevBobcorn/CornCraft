#if ENVIRO_URP

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;


namespace Enviro
{ 
    public class EnviroURPRenderFeature : ScriptableRendererFeature
    { 
        private EnviroURPRenderPass pass;


        public override void Create()
        {
            pass = new EnviroURPRenderPass("Enviro Render Pass");
        } 

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(pass != null && EnviroHelper.CanRenderOnCamera(renderingData.cameraData.camera))
            {
                pass.scriptableRenderer = renderer;
                renderer.EnqueuePass(pass);
            }
        }  
    }
}
#endif
