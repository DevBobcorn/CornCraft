using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Enviro
{
    [ExecuteInEditMode]
    [AddComponentMenu("Enviro 3/Volumetric Light")]
    public class EnviroVolumetricFogLight : MonoBehaviour
    {
        [Range(0f,2f)]
        public float intensity = 1.0f;
        [Range(0f,2f)]
        public float range = 1.0f;

        private Light myLight;
        private bool addedToMgr = false;
        private bool initialized = false;
        private CommandBuffer cascadeShadowCB;

        public bool isOn
            {
                get
                {
                    if (!isActiveAndEnabled)
                        return false;

                    Init();

                    return myLight.enabled;
                }

                private set{}
            }
    
        new public Light light {get{Init(); return myLight;} private set{}}


        void OnEnable()
        { 
            Init();
            AddToLightManager();
        }

        void OnDisable() 
        {
            if(cascadeShadowCB != null && myLight != null && myLight.type == LightType.Directional)
               myLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, cascadeShadowCB);

            RemoveFromLightManager();
        }

        void AddToLightManager()
        {
            if (!addedToMgr && EnviroManager.instance != null && EnviroManager.instance.Fog != null)
                addedToMgr = EnviroManager.instance.Fog.AddLight(this);
        }
            
        void RemoveFromLightManager()
        {
            if (addedToMgr && EnviroManager.instance != null && EnviroManager.instance.Fog != null)
            {
                EnviroManager.instance.Fog.RemoveLight(this);            
                addedToMgr = false;
                initialized = false;
            }
        } 

        private void Init()
        {
            if (initialized)
                return;

            myLight = GetComponent<Light>();
            
            if(myLight.type == LightType.Directional)
            {
                cascadeShadowCB = new CommandBuffer();
                cascadeShadowCB.name = "Dir Light Command Buffer";
                cascadeShadowCB.SetGlobalTexture("_CascadeShadowMapTexture", new UnityEngine.Rendering.RenderTargetIdentifier(UnityEngine.Rendering.BuiltinRenderTextureType.CurrentActive));  
                myLight.AddCommandBuffer(LightEvent.AfterShadowMap, cascadeShadowCB);
            } 

            initialized = true;
        }
    }
}
