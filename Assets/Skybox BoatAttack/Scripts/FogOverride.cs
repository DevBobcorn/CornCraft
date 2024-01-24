using UnityEngine;

namespace BoatAttackSkybox
{
    [ExecuteInEditMode]
    public class FogOverride : MonoBehaviour
    {
        [ColorUsage(false, true)]
        public Color mFogColor = Color.white;

        private void LateUpdate()
        {
            RenderSettings.fogColor = mFogColor;
        }
    }
}