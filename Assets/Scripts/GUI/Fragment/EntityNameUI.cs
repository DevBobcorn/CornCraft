#nullable enable
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
        [SerializeField] private TMP_Text? nameText;
        [SerializeField] private TMP_Text? descriptionText;
        private Action? destroyCallback;

        public override void SetInfo(EntityRender entityRender)
        {
            this.entityRender = entityRender;

            if (nameText != null)
            {
                nameText.text = (entityRender.Name ?? entityRender.CustomName) ??
                        ChatParser.TranslateString(entityRender.Type!.EntityId.GetTranslationKey("entity"));

            }

            if (descriptionText != null)
            {
                descriptionText.text = $"<{entityRender.Type!.EntityId}>";
            }
        }

        public override void Destroy(Action? callback)
        {
            GetComponent<Animator>()?.SetBool("Expired", true);

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