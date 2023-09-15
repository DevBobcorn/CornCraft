#nullable enable
using UnityEngine;

using CraftSharp.Control;


namespace CraftSharp.Rendering
{
    public class PlayerAccessoryWidget : MonoBehaviour
    {
        [HideInInspector] public Transform? mainHandRef;
        [HideInInspector] public Transform? spineRef;
        private Transform? mainHandSlot; // A slot fixed to mainHandRef transform (as a child)
        private Transform? itemMountPivot, itemMountSlot;
        private PlayerController? player;
        private MeleeWeapon? currentItem;

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
            var weaponPivotObj = new GameObject("Item Mount Pivot");
            itemMountPivot = weaponPivotObj.transform;
            itemMountPivot.SetParent(transform);

            var weaponSlotObj = new GameObject("Item Mount Slot");
            itemMountSlot = weaponSlotObj.transform;
            itemMountSlot.SetParent(itemMountPivot);
        }

        public void CreateWeapon(GameObject weaponPrefab)
        {
            if (currentItem != null)
            {
                Destroy(currentItem);
            }

            var weaponObj = GameObject.Instantiate(weaponPrefab);
            currentItem = weaponObj!.GetComponent<MeleeWeapon>();

            // Set weapon slot position and rotation
            itemMountPivot!.localPosition = new(0F, 1.3F, 0F);

            itemMountSlot!.localPosition = currentItem.slotPosition;
            itemMountSlot!.localEulerAngles = currentItem.slotEularAngles;

            // Mount weapon on start
            MountItem();
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
        public void DamageStart()
        {
            currentItem?.StartSlash();
        }

        // Called by animator event
        public void DamageStop()
        {
            currentItem?.EndSlash();
        }

        // Called by animator event
        public void StageEnding()
        {
            
        }

        void Start()
        {
            player = GetComponentInParent<PlayerController>();
            player.OnWeaponStateChanged += (weaponState) => {
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
        }

        void Update()
        {
            if (spineRef == null || itemMountPivot == null)
                return;

            itemMountPivot.localEulerAngles = new Vector3(-spineRef.localEulerAngles.x, 0F, 0F);
        }
    }
}