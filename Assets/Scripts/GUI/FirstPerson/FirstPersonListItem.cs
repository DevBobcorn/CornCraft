#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class FirstPersonListItem : MonoBehaviour
    {
        private CanvasGroup? canvasGroup;
        private TMP_Text? itemText;
        private Image?    itemIcon;

        private Sprite? normalIcon, selectedIcon;

        private bool initialized = false;

        void EnsureInitialized()
        {
            if (!initialized)
            {
                canvasGroup = GetComponent<CanvasGroup>();

                itemText = transform.Find("Text").GetComponent<TMP_Text>();
                itemIcon = transform.Find("Icon").GetComponent<Image>();
                initialized = true;
            }
        }

        void Start()
        {
            EnsureInitialized();
        }

        public void SetAlpha(float alpha)
        {
            EnsureInitialized();
            canvasGroup!.alpha = alpha;
        }

        public void SetContent(Sprite normal, Sprite selected, string text)
        {
            EnsureInitialized();

            normalIcon = normal;
            selectedIcon = selected;

            itemIcon!.sprite = normal;
            itemText!.text = text;

        }

    }
}
