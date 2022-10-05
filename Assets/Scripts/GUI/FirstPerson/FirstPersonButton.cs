#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (Image), typeof (Button))]
    public class FirstPersonButton : MonoBehaviour
    {
        public bool itemsOnLeftSide = false;
        public Vector2 panelSize = Vector2.zero;
        public bool avatarOnPanel = false;
        public string panelTitle = "???";

        public Sprite? normalIcon  = null;
        public Sprite? focusedIcon = null;

        private Image? buttonImage;
        private bool initialzed = false, focused = false;
        public bool Focused { get { return focused; } }

        void Start() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (!initialzed)
            {
                buttonImage = GetComponent<Image>();

                initialzed = true;
            }
        }

        public void Focus()
        {
            EnsureInitialized();

            focused = true;
            buttonImage!.sprite = focusedIcon;
        }

        public void Unfocus()
        {
            EnsureInitialized();

            focused = false;
            buttonImage!.sprite = normalIcon;
        }

    }
}
