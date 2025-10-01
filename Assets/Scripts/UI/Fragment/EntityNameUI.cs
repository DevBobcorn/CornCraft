using System;
using UnityEngine;
using TMPro;

using CraftSharp.Protocol;
using CraftSharp.Rendering;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Animator))]
    public class EntityNameUI : FloatingUI
    {
        private static readonly int EXPIRED = Animator.StringToHash("Expired");
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        private Action destroyCallback;

        public override void SetInfo(EntityRender entityRender)
        {
            this.entityRender = entityRender;

            if (nameText)
            {
                nameText.text = entityRender.GetDisplayName();
            }

            if (descriptionText)
            {
                // descriptionText.text = $"<{entityRender.Type.TypeId}>";
            }
        }

        private void Update()
        {
            if (!entityRender) return;

            descriptionText.text = entityRender.GetDebugText();
        }

        public override void Destroy(Action callback)
        {
            var animator = GetComponent<Animator>();
            animator.SetBool(EXPIRED, true);

            // Store this for later invocation
            destroyCallback = callback;
        }

        // Called by animator aftering fading out
        void Expire()
        {
            destroyCallback?.Invoke();
            Destroy(gameObject);
        }
    }
}