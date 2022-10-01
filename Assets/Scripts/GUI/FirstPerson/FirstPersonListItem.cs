#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup), typeof (Button))]
    public class FirstPersonListItem : MonoBehaviour
    {
        public Color normalTextColor   = Color.black;
        public Color selectedTextColor = Color.white;

        private FirstPersonMenu? parentMenu;
        private CanvasGroup? canvasGroup;
        private TMP_Text? itemText;
        private Image?    itemIcon;

        private Sprite? normalIcon, selectedIcon;

        private bool initialized = false, selected = false;

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                canvasGroup = GetComponent<CanvasGroup>();

                itemText = transform.Find("Text").GetComponent<TMP_Text>();
                itemIcon = transform.Find("Icon").GetComponent<Image>();

                selected = false;
                itemText!.color = normalTextColor;

                GetComponent<Button>().onClick.AddListener(() => Select());

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

        public void SetContent(FirstPersonMenu parent, Sprite normal, Sprite selected, string text)
        {
            EnsureInitialized();

            parentMenu = parent;
            normalIcon = normal;
            selectedIcon = selected;
            
            // Apply new values instantly
            itemIcon!.sprite = normal;
            itemText!.text = text;
        }

        public void Select()
        {
            parentMenu!.Select(this);
            // Update visuals
            itemIcon!.sprite = selectedIcon;
            itemText!.color = selectedTextColor;
            GetComponent<Button>().Select();
        }

        public void Deselect()
        {
            // Update visuals
            itemIcon!.sprite = normalIcon;
            itemText!.color = normalTextColor;
        }

    }
}
