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

        public void EnsureInitialized()
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

        public abstract void UpdateScreen();
    }
}