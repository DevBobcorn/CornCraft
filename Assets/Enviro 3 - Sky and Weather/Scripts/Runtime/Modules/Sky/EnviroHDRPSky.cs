#if ENVIRO_HDRP 
using System;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Sky/Enviro 3 Skybox")]
    [SkyUniqueID(990)]  
    public class EnviroHDRPSky : SkySettings
    {
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                
            }

            return hash;
        }

        public override int GetHashCode(Camera camera)
        {
            // Implement if your sky depends on the camera settings (like position for instance)
            return GetHashCode();
        }
 
        public override Type GetSkyRendererType() { return typeof(EnviroHDRPSkyRenderer); }
    }
}
#endif
