#nullable enable
using System;
using UnityEngine;
using TMPro;

using MinecraftClient.Mapping;
using MinecraftClient.Protocol;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (Animator))]
    public class EntityNameUI : FloatingUI
    {
        [SerializeField] private TMP_Text? nameText;
        [SerializeField] private TMP_Text? descriptionText;
        private Action? destroyCallback;

        public override void SetInfo(Entity entity)
        {
            this.entity = entity;

            if (nameText != null)
            {
                nameText.text = (entity.Name ?? entity.CustomName) ??
                        ChatParser.TranslateString(entity.Type.EntityId.GetTranslationKey("entity"));

            }

            if (descriptionText != null)
            {
                descriptionText.text = $"<{entity.Type.EntityId}>";
            }
        }

        public override void Destroy(Action callback)
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