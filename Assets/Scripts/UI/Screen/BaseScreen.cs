using UnityEngine;

namespace CraftSharp.UI
{
    public abstract class BaseScreen : MonoBehaviour
    {
        protected static readonly int SHOW_HASH = Animator.StringToHash("Show");

        public abstract bool IsActive { get; set; }

        public abstract bool ReleaseCursor();
        public abstract bool ShouldPauseControllerInput();

        protected bool initialized;
        protected abstract void Initialize();
        
        // Input System Fields & Methods
        public BaseActions BaseActions { get; private set; }

        public void EnsureInitialized()
        {
            if (!initialized)
            {
                // Initialize base actions...
                BaseActions = new BaseActions();
                
                Initialize();
                initialized = true;
            }
        }

        protected virtual void Start()
        {
            EnsureInitialized();
        }

        protected virtual void OnDestroy()
        {
            // Make sure base actions are disabled
            BaseActions?.Disable();
        }

        public abstract void UpdateScreen();
    }
}