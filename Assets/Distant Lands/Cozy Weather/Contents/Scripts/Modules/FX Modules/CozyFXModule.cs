using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DistantLands.Cozy
{
    [System.Serializable]
    public abstract class CozyFXModule
    {

        public Transform parent;
        public bool isEnabled
        {
            get { return _IsEnabled; }
            set
            {
                if (_IsEnabled != isEnabled)
                {

                    _IsEnabled = isEnabled;

                    if (isEnabled == true)
                        OnFXEnable();
                    else
                        OnFXDisable();

                }
            }
        }

        [SerializeField]
        private bool _IsEnabled = true;

        [SerializeField]
        private bool _OpenTab;

        public VFXModule vfx;

        public abstract void OnFXEnable();
        public abstract void OnFXDisable();
        public abstract void OnFXUpdate();
        public abstract void SetupFXParent(); 


    }
}