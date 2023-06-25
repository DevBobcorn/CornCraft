#nullable enable
using UnityEngine;

namespace MinecraftClient.UI
{
    public abstract class BaseScreen : MonoBehaviour
    {
        public abstract bool IsActive { get; set; }

        public abstract bool ReleaseCursor();
        public abstract bool ShouldPause();
        public virtual bool AbsorbMouseScroll() => false;

        protected bool initialized;
        protected abstract bool Initialize();

        public virtual bool EnsureInitialized()
        {
            if (!initialized)
                return (initialized = Initialize());
            return true;
        }

        protected virtual void Start()
        {
            EnsureInitialized();
        }

    }
}