using UnityEngine;

namespace MinecraftClient.UI
{
    public abstract class BaseScreen : MonoBehaviour
    {
        public abstract bool IsActive { get; set; }
        public abstract string ScreenName();

        public abstract bool ReleaseCursor();

        public abstract bool ShouldPause();

        private bool initialized;
        protected abstract void Initialize();

        protected virtual void EnsureInitialized()
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }
        }

        protected virtual void Start()
        {
            EnsureInitialized();
        }

    }
}