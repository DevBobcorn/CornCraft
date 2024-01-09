#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class LoadingScreen : BaseScreen
    {
        private bool isActive = false;

        // UI controls and objects
        [SerializeField] private Animator? screenAnimator;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenAnimator!.SetBool(SHOW_HASH, isActive);
            }

            get {
                return isActive;
            }
        }

        public override bool ReleaseCursor()
        {
            return false;
        }

        public override bool ShouldPause()
        {
            return true;
        }

        protected override void Initialize()
        {

        }

        void OnDestroy()
        {
            
        }

        public override void UpdateScreen()
        {
            
        }
    }
}
