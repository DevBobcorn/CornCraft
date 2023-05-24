using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enviro
{
    [ExecuteInEditMode]
    [AddComponentMenu("Enviro 3/Effect Removal Zone")]
    public class EnviroEffectRemovalZone : MonoBehaviour
    {
        [Range(-2f, 0f)]
        public float density = 0.0f;
        public float radius = 1.0f;
        public float stretch = 2.0f;
        [Range(0, 1)]
        public float feather = 0.7f;

        private bool addedToMgr = false;

        void OnEnable()
        {
            if(EnviroManager.instance != null)
               AddToZoneToManager();
        }
 
        void OnDisable() 
        {
            if(EnviroManager.instance != null)
               RemoveZoneFromManager();
        }

        
        void OnDestroy()
        {
            if(EnviroManager.instance != null)
               RemoveZoneFromManager();
        }

  
        void AddToZoneToManager()
        {
            if (!addedToMgr)
                addedToMgr = EnviroManager.instance.AddRemovalZone(this);
        }
         
        void RemoveZoneFromManager()
        {
            if (addedToMgr)
                EnviroManager.instance.RemoveRemovaleZone(this);

                addedToMgr = false;
        }

        void OnDrawGizmosSelected()
        {
            Matrix4x4 m = Matrix4x4.identity;
            Transform t = transform;
            m.SetTRS(t.position, t.rotation, new Vector3(1.0f, stretch, 1.0f));
            Gizmos.matrix =  m;
            Gizmos.DrawWireSphere(Vector3.zero, radius);
        }
    }
}
