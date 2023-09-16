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
        [HideInInspector] public Transform? mainHandRef;
        [HideInInspector] public Transform? spineRef;
        private Transform? mainHandSlot; // A slot fixed to mainHandRef transform (as a child)
        private Transform? itemMountPivot, itemMountSlot;
        private PlayerController? player;
        private PlayerActionItem? currentItem;
        private Animator? playerAnimator;

        public void SetRefTransforms(Transform mainHandRef, Transform spineRef)
        {
            this.mainHandRef = mainHandRef;
            // Create weapon slot transform
            var mainHandSlotObj = new GameObject("Main Hand Slot");
            mainHandSlot = mainHandSlotObj.transform;
            mainHandSlot.SetParent(mainHandRef);
            // Initialize position and rotation
            mainHandSlot.localPosition = Vector3.zero;
            mainHandSlot.localEulerAngles = Vector3.zero;

            this.spineRef = spineRef;

            // Create weapon mount slot transform
            var itemMountPivotObj = new GameObject("Item Mount Pivot");
            itemMountPivot = itemMountPivotObj.transform;
            itemMountPivot.SetParent(transform);

            var itemMountSlotObj = new GameObject("Item Mount Slot");
            itemMountSlot = itemMountSlotObj.transform;
            itemMountSlot.SetParent(itemMountPivot);
        }

        private void CreateActionItem(ItemStack itemStack, ItemActionType actionType)
        {
            if (currentItem != null)
            {
                Destroy(currentItem.gameObject);
            }

            //var itemObj = GameObject.Instantiate(itemPrefab);
            //currentItem = itemObj!.GetComponent<PlayerActionItem>();

            var itemObj = new GameObject("Action Item");
            itemObj.layer = this.gameObject.layer;

            currentItem = actionType switch
            {
                ItemActionType.MeleeWeaponSword => itemObj!.AddComponent<MeleeWeapon>(),

                _                               => itemObj!.AddComponent<UselessActionItem>()
            };

            (Mesh mesh, Material material, Dictionary<DisplayPosition, float3x3> transforms)? meshData = null;

            if (actionType == ItemActionType.None)
            {
                meshData = ItemMeshBuilder.BuildItem(itemStack, true);

                currentItem.slotEularAngles = new(0F, 90F, 0F);
                currentItem.slotPosition = new(0F, 0F, -0.5F);
                mainHandSlot!.localPosition = Vector3.zero;
                mainHandSlot.localEulerAngles = Vector3.zero;
            }
            else // Hand held melee weapon
            {
                meshData = ItemMeshBuilder.BuildItem(itemStack, false);

                currentItem.slotEularAngles = new(135F, 90F, 0F);
                currentItem.slotPosition = new(0F, 0F, -0.1F);
                mainHandSlot!.localPosition = new(-0.2F, 0F, 0.2F);
                mainHandSlot.localEulerAngles = new(-135F, 0F, 45F);

                if (player!.SwordTrailPrefab != null)
                {
                    var trailObj = GameObject.Instantiate(player!.SwordTrailPrefab);
                    trailObj.transform.parent = itemObj.transform;
                    trailObj.transform.localPosition = new(0.5F, 0.65F, 0.65F);

                    var currentMeleeWeapon = currentItem as MeleeWeapon;
                    currentMeleeWeapon!.SlashTrail = trailObj.GetComponent<TrailRenderer>();
                }
            }

            itemObj.transform.localScale = new(0.5F, 0.5F, 0.5F);

            if (meshData is not null)
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
            MountItem();
        }

        private void DestroyActionItem()
        {
            if (currentItem != null)
            {
                Destroy(currentItem.gameObject);
            }
        }

        private void HoldItem()
        {
            if (currentItem != null)
            {
                currentItem.transform.SetParent(mainHandSlot, false);
                currentItem.transform.localPosition = Vector3.zero;
                currentItem.transform.localRotation = Quaternion.identity;
            }
        }

        private void MountItem()
        {
            if (currentItem != null)
            {
                currentItem.transform.SetParent(itemMountSlot, false);
                currentItem.transform.localPosition = Vector3.zero;
                currentItem.transform.localRotation = Quaternion.identity;
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

        void Start()
        {
            player = GetComponentInParent<PlayerController>();
            playerAnimator = GetComponent<Animator>();

            player.OnItemStateChanged += (weaponState) => {
                switch (weaponState)
                {
                    case PlayerController.CurrentItemState.Hold:
                        HoldItem();
                        break;
                    case PlayerController.CurrentItemState.Mount:
                        MountItem();
                        break;
                }
            };

            player.OnCurrentItemChanged += (itemStack) => {
                if (itemStack is null)
                {
                    DestroyActionItem();
                }
                else
                {
                    var actionType = PlayerActionHelper.GetItemActionType(itemStack);
                    CreateActionItem(itemStack, actionType);
                }
            };

            player.OnMeleeDamageStart += () => {
                currentItem?.StartAction();
            };

            player.OnMeleeDamageEnd += () => {
                currentItem?.EndAction();
            };
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