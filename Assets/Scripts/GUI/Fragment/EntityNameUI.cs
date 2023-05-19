#nullable enable
using UnityEngine;
using TMPro;

using MinecraftClient.Mapping;
using MinecraftClient.Protocol;

namespace MinecraftClient.UI
{
    public class EntityNameUI : FloatingUI
    {
        [SerializeField] private TMP_Text? nameText;
        [SerializeField] private TMP_Text? descriptionText;

        public override void SetInfo(FloatingUIManager manager, Entity entity)
        {
            this.manager = manager;
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
    }
}