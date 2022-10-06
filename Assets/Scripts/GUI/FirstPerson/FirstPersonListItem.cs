#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup), typeof (Image), typeof (Button))]
    public class FirstPersonListItem : MonoBehaviour
    {
        private static readonly Color TRANSPARENT = new(0F, 0F, 0F, 0F);

        public Color normalForeColor  = Color.black;
        public Color focusedForeColor = Color.white;
        public Color normalBackColor  = Color.black;
        public Color focusedBackColor = Color.white;

        public FirstPersonMenu? SubMenu { get; set; } = null;

        private FirstPersonMenu? parentMenu;
        private CanvasGroup? canvasGroup;
        private TMP_Text? itemText;
        private Image?    itemIcon, itemBackground, arrow;

        private Sprite? normalIcon, selectedIcon;

        public Action? Callback = null;

        private bool initialized = false, focused = false, unfolded = false;
        public bool Focused { get { return focused; } }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                itemBackground = GetComponent<Image>();

                itemText = transform.Find("Text").GetComponent<TMP_Text>();
                itemIcon = transform.Find("Icon").GetComponent<Image>();
                arrow = transform.Find("Arrow").GetComponent<Image>();

                focused = false;
                unfolded = false;

                itemText!.color = normalForeColor;
                itemBackground!.color = normalBackColor;
                arrow!.color = TRANSPARENT;

                GetComponent<Button>().onClick.AddListener(() => parentMenu!.TryFocusItem(this));

                initialized = true;
            }
        }

        void Start() => EnsureInitialized();

        public void SetAlpha(float alpha)
        {
            EnsureInitialized();
            canvasGroup!.alpha = alpha;
        }

        // Should be only called once as initialization
        public void SetContent(FirstPersonMenu parent, Sprite normal, Sprite selected, string text)
        {
            EnsureInitialized();

            parentMenu = parent;
            normalIcon = normal;
            selectedIcon = selected;
            
            // Apply new values instantly
            itemText!.text = text;
            Unfocus();
        }

        public void Focus(bool unfold)
        {
            EnsureInitialized();
            // Update visuals
            itemIcon!.sprite = selectedIcon;
            itemText!.color = focusedForeColor;

            itemBackground!.color = focusedBackColor;

            arrow!.color = unfold ? focusedBackColor : TRANSPARENT;

            focused = true;
            unfolded = unfold;
        }

        public void Unfocus()
        {
            EnsureInitialized();
            // Update visuals
            itemIcon!.sprite = normalIcon;
            itemText!.color = normalForeColor;

            itemBackground!.color = normalBackColor;
            arrow!.color = TRANSPARENT;

            focused = false;
            unfolded = false;
        }

    }
}
