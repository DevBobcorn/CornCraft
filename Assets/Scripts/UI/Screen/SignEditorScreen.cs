using System.Collections.Generic;
using CraftSharp.Protocol.Message;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using CraftSharp.Rendering;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class SignEditorScreen : BaseScreen
    {
        // UI controls and objects
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image signBackgroundImage;
        [SerializeField] private TMP_InputField text1Input;
        [SerializeField] private TMP_InputField text2Input;
        [SerializeField] private TMP_InputField text3Input;
        [SerializeField] private TMP_InputField text4Input;
        [SerializeField] private Button submitButton;
        
        private readonly List<TMP_InputField> inputFields = new();
        private CanvasGroup screenGroup;
        
        private BlockLoc? signLoc = null; // null for none
        private bool front = true;

        public void SetSignInfo(BlockLoc blockLoc, bool isFrontText)
        {
            EnsureInitialized();

            signLoc = blockLoc;
            front = isFrontText;
            
            var client = CornApp.CurrentClient;
            if (!client) return;

            var block = client.ChunkRenderManager.GetBlock(blockLoc);
            var variant = block.BlockId.Path;

            bool isHanging = false;

            if (variant.EndsWith("_wall_hanging_sign"))
            {
                variant = variant[..^"_wall_hanging_sign".Length];
                isHanging = true;
            }
            else if (variant.EndsWith("_hanging_sign"))
            {
                variant = variant[..^"_hanging_sign".Length];
                isHanging = true;
            }
            else if (variant.EndsWith("_wall_sign")) variant = variant[..^"_wall_sign".Length];
            else if (variant.EndsWith("_sign")) variant = variant[..^"_sign".Length];

            int widthPixel = isHanging ? 14 : 24;
            int heightPixel = isHanging ? 10 : 12;
            signBackgroundImage.rectTransform.sizeDelta = new Vector2(widthPixel * 20F, heightPixel * 20F);
            
            var matManager = client.EntityMaterialManager;
            var textureId = new ResourceLocation(isHanging ? $"entity/signs/hanging/{variant}" : $"entity/signs/{variant}");
            titleText.text = ChatParser.TranslateString(isHanging ? "hanging_sign.edit" : "sign.edit");
            
            matManager.ApplyTextureOrSkin(textureId, texture =>
            {
                var sprite = EntityMaterialManager.CreateSpriteFromTexturePart(
                    texture, 2 * texture.width / 64, (isHanging ? 14 : 2) * texture.height / 32,
                    widthPixel * texture.width / 64, heightPixel * texture.height / 32);

                signBackgroundImage.sprite = sprite;
            });
        }

        private bool isActive = false;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenGroup.alpha = value ? 1F : 0F;
                screenGroup.blocksRaycasts = value;
                screenGroup.interactable   = value;
                
                // Clear all texts
                text1Input.text = string.Empty;
                text2Input.text = string.Empty;
                text3Input.text = string.Empty;
                text4Input.text = string.Empty;
                
                // Select this text input
                text1Input.Select();
            }

            get => isActive;
        }

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPauseControllerInput()
        {
            return true;
        }

        private void Submit()
        {
            var client = CornApp.CurrentClient;
            if (!client || !signLoc.HasValue) return;

            client.UpdateSign(signLoc.Value, front, text1Input.text,
                text2Input.text, text3Input.text, text4Input.text);
            
            // Clear all texts
            text1Input.text = string.Empty;
            text2Input.text = string.Empty;
            text3Input.text = string.Empty;
            text4Input.text = string.Empty;
            
            client.ScreenControl.TryPopScreen();
        }

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();

            submitButton.onClick.AddListener(Submit);
            
            // Add inputs to list for navigation
            inputFields.Add(text1Input);
            inputFields.Add(text2Input);
            inputFields.Add(text3Input);
            inputFields.Add(text4Input);
        }

        private void NavigateToPrevious(TMP_InputField current)
        {
            int currentIndex = inputFields.IndexOf(current);
            if (currentIndex > 0)
            {
                inputFields[currentIndex - 1].Select();
                inputFields[currentIndex - 1].ActivateInputField();
            }
        }

        private void NavigateToNext(TMP_InputField current)
        {
            int currentIndex = inputFields.IndexOf(current);
            if (currentIndex < inputFields.Count - 1)
            {
                inputFields[currentIndex + 1].Select();
                inputFields[currentIndex + 1].ActivateInputField();
            }
        }

        public override void UpdateScreen()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Submit();
            }
            
            // Check if any TMP_InputField is currently focused
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            if (currentSelected)
            {
                TMP_InputField currentInputField = currentSelected.GetComponent<TMP_InputField>();
                if (currentInputField && currentInputField.lineType == TMP_InputField.LineType.SingleLine)
                {
                    if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                    {
                        NavigateToPrevious(currentInputField);
                    }
                    else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                    {
                        NavigateToNext(currentInputField);
                    }
                }
            }
        }
    }
}
