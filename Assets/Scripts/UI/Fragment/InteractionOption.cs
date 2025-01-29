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
        [SerializeField] private Sprite enterLocationSprite;
        [SerializeField] private Sprite rideSprite;
        [SerializeField] private Sprite itemIconSprite;
        [SerializeField] private TMP_Text optionHintText;
        [SerializeField] private TMP_Text keyHintText;
        [SerializeField] private MeshFilter itemIconMeshFilter;
        [SerializeField] private MeshRenderer itemIconMeshRenderer;

        public Transform KeyHintTransform => keyHintText.transform;

        private Animator _optionAnimator;
        private int interactionKey;
        public int InteractionId => interactionKey;

        private bool usingItemIcon = false;

        public InteractionInfo interactionInfo;

        void Awake()
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
            HideItemIcon();

            if (info is BlockViewInteractionInfo viewInfo)
            {
                switch (viewInfo.IconType)
                {
                    case InteractionIconType.Dialog:
                        optionIconImage.overrideSprite = null;
                        usingItemIcon = false;
                        break;
                    case InteractionIconType.EnterLocation:
                        optionIconImage.overrideSprite = enterLocationSprite;
                        usingItemIcon = false;
                        break;
                    case InteractionIconType.Ride:
                        optionIconImage.overrideSprite = rideSprite;
                        break;
                    case InteractionIconType.ItemIcon:
                        optionIconImage.overrideSprite = itemIconSprite;
                        // Set up item mesh
                        UpdateItemMesh(viewInfo.IconItemId);
                        // Display item mesh
                        usingItemIcon = true;
                        ShowItemIcon();
                        break;
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
            keyHintText.text = keyHint;
        }

        public void ShowItemIcon()
        {
            itemIconMeshFilter.gameObject.SetActive(usingItemIcon);
        }

        public void HideItemIcon()
        {
            itemIconMeshFilter.gameObject.SetActive(false);
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
                EventManager.Instance.Broadcast(new ViewInteractionExecutionEvent(interactionInfo.Id));
            }
        }

        // Called by animator after hide animation ends...
        void Expire()
        {
            EventManager.Instance.Broadcast<NotificationExpireEvent>(new(interactionKey));
            Destroy(this.gameObject);
        }

        private void UpdateItemMesh(ResourceLocation itemId)
        {
            var result = ItemMeshBuilder.BuildItem(new ItemStack(ItemPalette.INSTANCE.GetById(itemId), 1), true);

            if (result != null) // If build suceeded
            {
                itemIconMeshFilter.sharedMesh = result.Value.mesh;
                itemIconMeshRenderer.sharedMaterial = result.Value.material;

                // Handle GUI display transform
                bool hasGUITransform = result.Value.transforms.TryGetValue(DisplayPosition.GUI, out float3x3 t);

                if (hasGUITransform) // Apply specified local transform
                {
                    // Apply local translation, '1' in translation field means 0.1 unit in local space, so multiply with 0.1
                    modelObject.transform.localPosition = t.c0 * 0.1F;
                    // Apply local rotation
                    modelObject.transform.localEulerAngles = Vector3.zero;
                    // - MC ROT X
                    modelObject.transform.Rotate(Vector3.back, t.c1.x, Space.Self);
                    // - MC ROT Y
                    modelObject.transform.Rotate(Vector3.down, t.c1.y, Space.Self);
                    // - MC ROT Z
                    modelObject.transform.Rotate(Vector3.left, t.c1.z, Space.Self);
                    // Apply local scale
                    modelObject.transform.localScale = t.c2;
                }
                else // Apply uniform local transform
                {
                    // Apply local translation, set to zero
                    modelObject.transform.localPosition = Vector3.zero;
                    // Apply local rotation
                    modelObject.transform.localEulerAngles = Vector3.zero;
                    // Apply local scale
                    modelObject.transform.localScale = Vector3.one;
                }
            }
            else // If build failed (item is empty or invalid)
            {
                itemIconMeshFilter.sharedMesh = null;
            }
        }
    }
}