using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;

using CraftSharp.Event;
using CraftSharp.Control;
using CraftSharp.Rendering;
using CraftSharp.Resource;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Animator))]
    public class InteractionOption : MonoBehaviour
    {
        private static readonly int SELECTED = Animator.StringToHash("Selected");
        private static readonly int EXPIRED  = Animator.StringToHash("Expired");
        private static readonly int EXECUTED = Animator.StringToHash("Executed");

        [SerializeField] private GameObject modelObject;
        [SerializeField] private Image optionIconImage;
        [SerializeField] private TMP_Text optionHintText;
        [SerializeField] private TMP_Text keyHintText;

        public Transform KeyHintTransform => keyHintText.transform;

        private Animator _optionAnimator;
        private int interactionKey;
        public int InteractionId => interactionKey;

        private bool usingItemIcon = false;

        public InteractionInfo interactionInfo;

        private void Awake()
        {
            _optionAnimator = GetComponent<Animator>();
        }

        public void SetId(int id)
        {
            interactionKey = id;
        }

        public void SetInfo(InteractionInfo info)
        {
            interactionInfo = info;

            var paramTexts = info.ParamTexts;
            var hintText = Translations.Get(info.HintKey, paramTexts);

            usingItemIcon = false;
            UpdateItemIconVisibility(false);

            if (info is IIconProvider iconProvider)
            {
                var spriteType = SpriteTypePalette.INSTANCE.GetById(iconProvider.IconTypeId);

                optionIconImage.overrideSprite = spriteType.Sprite;
                if (spriteType.UseItemModel)
                {
                    // Set up item mesh
                    UpdateItemMesh(iconProvider.IconItemId);
                    // Display item mesh
                    usingItemIcon = true;
                    UpdateItemIconVisibility(true);
                }
                else
                {
                    usingItemIcon = false;
                }
            }

            optionHintText.text = hintText;
            gameObject.name = info.HintKey;
        }

        public void UpdateInfoText()
        {
            var hintText = Translations.Get(interactionInfo.HintKey, interactionInfo.ParamTexts);

            optionHintText.text = hintText;
            gameObject.name = interactionInfo.HintKey;
        }

        public void UpdateKeyHintText(string keyHint)
        {
            // For some special keys, translations are used
            keyHintText.text = Translations.Get(keyHint);
        }

        public void UpdateItemIconVisibility(bool visible)
        {
            modelObject.SetActive(visible && usingItemIcon);
        }

        public void SetSelected(bool selected)
        {
            // Set selected param in animator
            _optionAnimator.SetBool(SELECTED, selected);
        }

        public void Remove()
        {
            // Play fade away animation...
            _optionAnimator.SetBool(EXPIRED, true);
        }

        public void Execute()
        {
            _optionAnimator.SetTrigger(EXECUTED); // Execution visual feedback

            if (interactionInfo != null)
            {
                EventManager.Instance.Broadcast(new TriggerInteractionExecutionEvent(interactionInfo.Id));
            }
        }

        // Called by animator after hide animation ends...
        private void Expire()
        {
            Destroy(gameObject);
        }

        private void UpdateItemMesh(ResourceLocation itemId)
        {
            var itemStack = new ItemStack(ItemPalette.INSTANCE.GetById(itemId), 1);
            
            ItemMeshBuilder.BuildItemGameObject(modelObject, itemStack, DisplayPosition.GUI, true);
            
            foreach (Transform t in modelObject.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = gameObject.layer; // Make sure all children are in UI layer
            }
        }
    }
}