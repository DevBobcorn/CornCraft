#nullable enable
using UnityEngine;
using TMPro;

using MinecraftClient.Mapping;
using MinecraftClient.Protocol;

namespace MinecraftClient.UI
{
    public class FloatingUI : MonoBehaviour
    {
        [SerializeField] TMP_Text? nameText;
        [SerializeField] TMP_Text? descriptionText;

        private FloatingUIManager? manager;

        public void SetInfo(FloatingUIManager manager, Entity entity)
        {
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

        public void Destroy()
        {
            // TODO Fade out

            Destroy(gameObject);
        }
    }
}