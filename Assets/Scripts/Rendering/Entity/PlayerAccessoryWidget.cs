#nullable enable
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using CraftSharp.Control;
using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class PlayerAccessoryWidget : MonoBehaviour
    {
        [SerializeField] private Transform? mainHandRef;
        [SerializeField] private Transform? offHandRef;
        [SerializeField] private Transform? spineRef;

        private Transform? mainHandSlot; // A slot fixed to mainHandRef transform (as a child)
        private Transform? offHandSlot; // A slot fixed to offHandRef transform (as a child)
        private Transform? itemMountPivot, itemMountSlot;
        private PlayerController? player;
        private PlayerActionItem? currentItem;
        private Animator? playerAnimator;

        public void SetRefTransforms(Transform mainHandRef, Transform offHandRef, Transform spineRef)
        {
            this.mainHandRef = mainHandRef;
            // Create main hand slot transform
            var mainHandSlotObj = new GameObject("Main Hand Slot");
            mainHandSlot = mainHandSlotObj.transform;
            mainHandSlot.SetParent(mainHandRef);
            // Initialize position and rotation
            mainHandSlot.localPosition = Vector3.zero;
            mainHandSlot.localEulerAngles = Vector3.zero;

            this.offHandRef = offHandRef;
            // Create off hand slot transform
            var offHandSlotObj = new GameObject("Off Hand Slot");
            offHandSlot = offHandSlotObj.transform;
            offHandSlot.SetParent(offHandRef);
            // Initialize position and rotation
            offHandSlot.localPosition = Vector3.zero;
            offHandSlot.localEulerAngles = Vector3.zero;

            this.spineRef = spineRef;

            // Create weapon mount slot transform
            var itemMountPivotObj = new GameObject("Item Mount Pivot");
            itemMountPivot = itemMountPivotObj.transform;
            itemMountPivot.SetParent(transform);

            var itemMountSlotObj = new GameObject("Item Mount Slot");
            itemMountSlot = itemMountSlotObj.transform;
            itemMountSlot.SetParent(itemMountPivot);
        }

        private void CreateActionItem(ItemStack? itemStack, ItemActionType actionType, PlayerSkillItemConfig psi)
        {
            if (currentItem != null)
            {
                // See https://forum.unity.com/threads/editor-and-destroyimmediate.1261745/
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(currentItem.gameObject);
                }
                else
#endif
                {
                    Destroy(currentItem.gameObject);
                }
            }

            var itemObj = new GameObject($"Action Item ({itemStack?.DisplayName})")
            {
                layer = gameObject.layer
            };

            (Mesh mesh, Material material, Dictionary<DisplayPosition, float3x3> transforms)? meshData = null;

            switch (actionType)
            {
                case ItemActionType.MeleeWeaponSword:
                    currentItem = itemObj!.AddComponent<MeleeWeapon>();
                    meshData = ItemMeshBuilder.BuildItem(itemStack, false);
                    // Use dummy material and mesh if failed to build for item
                    meshData ??= (psi.DummySwordItemMesh!, psi.DummyItemMaterial!, new());

                    currentItem.slotEularAngles = new(135F, 90F, -20F);
                    currentItem.slotPosition = new(0F, 0.2F, -0.25F);

                    mainHandSlot!.localPosition = new(0F, -0.1F, 0.05F);
                    mainHandSlot.localEulerAngles = new(-135F, 0F, 45F);

                    var trailObj = GameObject.Instantiate(psi.SwordTrailPrefab);
                    trailObj.transform.parent = itemObj.transform;
                    trailObj.transform.localPosition = new(0.5F, 0.65F, 0.65F);

                    var sword = currentItem as MeleeWeapon;
                    sword!.SlashTrail = trailObj.GetComponent<TrailRenderer>();

                    itemObj.transform.localScale = new(0.5F, 0.5F, 0.5F);
                    break;
                case ItemActionType.RangedWeaponBow:
                    currentItem = itemObj!.AddComponent<UselessActionItem>();
                    meshData = ItemMeshBuilder.BuildItem(itemStack, false);
                    // Use dummy material and mesh if failed to build for item
                    meshData ??= (psi.DummyBowItemMesh!, psi.DummyItemMaterial!, new());

                    currentItem.slotEularAngles = new(-40F, 90F, 20F);
                    currentItem.slotPosition = new(0F, -0.4F, -0.4F);
                    
                    offHandSlot!.localPosition = new(-0.21F, 0.12F, 0.38F);
                    offHandSlot.localEulerAngles = new(5F, -150F, 115F);

                    itemObj.transform.localScale = new(0.6F, 0.6F, 0.6F);
                    break;
                default:
                    currentItem = itemObj!.AddComponent<UselessActionItem>();
                    meshData = ItemMeshBuilder.BuildItem(itemStack, true);
                    // Use dummy material and mesh if failed to build for item
                    meshData ??= (psi.DummySwordItemMesh!, psi.DummyItemMaterial!, new());

                    currentItem.slotEularAngles = new(0F, 90F, 0F);
                    currentItem.slotPosition = new(0F, 0F, -0.5F);

                    itemObj.transform.localScale = new(0.5F, 0.5F, 0.5F);
                    break;
            }

            if (meshData is not null) // In case of invalid items, meshData can be null
            {
                var mesh = itemObj.AddComponent<MeshFilter>();
                mesh.mesh = meshData.Value.mesh;

                var renderer = itemObj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = meshData.Value.material;
            }

            // Set weapon slot position and rotation
            itemMountPivot!.localPosition = new(0F, 1.3F, 0F);

            itemMountSlot!.localPosition = currentItem.slotPosition;
            itemMountSlot!.localEulerAngles = currentItem.slotEularAngles;

            // Mount weapon on start
            MoveItemToWidgetSlot(itemMountSlot!);
        }

        private void DestroyActionItem()
        {
            if (currentItem != null)
            {
                Destroy(currentItem.gameObject);
            }
        }

        private void MoveItemToWidgetSlot(Transform slot)
        {
            if (currentItem != null)
            {
                currentItem.transform.SetParent(slot, false);
                currentItem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        // Called by animator event
        public void DamageStart() { }

        // Called by animator event
        public void DamageStop() { }

        // Called by animator event
        public void StageEnding() { }

        // Called by animator event
        public void FootL() { }

        public void FootR() { }

        public void Hit() { }

        public void UpdateActiveItem(ItemStack? itemStack, ItemActionType actionType, PlayerSkillItemConfig? psi = null)
        {
            if (actionType == ItemActionType.None)
            {
                DestroyActionItem();
            }
            else
            {
                if ((psi != null) || ((psi = player?.SkillItemConf) != null))
                {
                    CreateActionItem(itemStack, actionType, psi);
                }
                else
                {
                    Debug.LogWarning("Player skill item config is neither passed in nor present in player controller!");
                }
            }
            
            // Mount weapon on start
            MoveItemToWidgetSlot(itemMountSlot!);
        }

        public void UpdateActionItemState(PlayerController.CurrentItemState state)
        {
            switch (state)
            {
                case PlayerController.CurrentItemState.HoldInMainHand:
                    MoveItemToWidgetSlot(mainHandSlot!);
                    break;
                case PlayerController.CurrentItemState.HoldInOffhand:
                    MoveItemToWidgetSlot(offHandSlot!);
                    break;
                case PlayerController.CurrentItemState.Mount:
                    MoveItemToWidgetSlot(itemMountSlot!);
                    break;
            }
        }

        public void Initialize()
        {
            player = GetComponentInParent<PlayerController>();
            playerAnimator = GetComponent<Animator>();

            // If this is used by an actual player object, instead of a preview object etc.
            if (player)
            {
                // These subscriptions will be cleared when the player render is replaced/destroyed,
                // so it is not necessary to manually unregister them
                player.OnItemStateChanged += UpdateActionItemState;

                player.OnCurrentItemChanged += (i, iat) => UpdateActiveItem(i, iat, player.SkillItemConf);

                player.OnMeleeDamageStart += () => {
                    currentItem?.StartAction();
                };

                player.OnMeleeDamageEnd += () => {
                    currentItem?.EndAction();
                };
            }
        }

        void Update()
        {
            if (spineRef == null || itemMountPivot == null)
                return;

            itemMountPivot.localEulerAngles = new Vector3(-spineRef.localEulerAngles.x, 0F, 0F);
        }

        void OnAnimatorMove()
        {
            if (player is not null && player.UseRootMotion)
            {
                var rb = player.PlayerRigidbody;

                rb.position += playerAnimator!.deltaPosition;
            }
        }
    }
}