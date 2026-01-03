using UnityEngine;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class LoadingScreen : BaseScreen
    {
        private bool isActive = false;

        // UI controls and objects
        [SerializeField] private Animator screenAnimator;

        public override bool IsActive
        {
            set {
                isActive = value;

                screenAnimator.SetBool(SHOW_HASH, isActive);
                
                // Show the screen immediately
                if (isActive)
                {
                    screenAnimator.Play("Shown");
                }
            }

            get => isActive;
        }

        public override bool ReleaseCursor()
        {
            return false;
        }

        public override bool ShouldPauseControllerInput()
        {
            return true;
        }

        protected override void Initialize()
        {

        }

        public override void UpdateScreen()
        {
            
        }
    }
}
