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
        private Transform? weaponMountPivot, weaponMountSlot;
        private PlayerController? player;
        private MeleeWeapon? currentWeapon;

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
            var weaponPivotObj = new GameObject("Weapon Mount Pivot");
            weaponMountPivot = weaponPivotObj.transform;
            weaponMountPivot.SetParent(transform);

            var weaponSlotObj = new GameObject("Weapon Mount Slot");
            weaponMountSlot = weaponSlotObj.transform;
            weaponMountSlot.SetParent(weaponMountPivot);
        }

        public void CreateWeapon(GameObject weaponPrefab)
        {
            if (currentWeapon != null)
            {
                Destroy(currentWeapon);
            }

            var weaponObj = GameObject.Instantiate(weaponPrefab);
            currentWeapon = weaponObj!.GetComponent<MeleeWeapon>();

            // Set weapon slot position and rotation
            weaponMountPivot!.localPosition = new(0F, 1.3F, 0F);

            weaponMountSlot!.localPosition = currentWeapon.slotPosition;
            weaponMountSlot!.localEulerAngles = currentWeapon.slotEularAngles;

            // Mount weapon on start
            MountWeapon();
        }

        private void HoldWeapon()
        {
            if (currentWeapon != null)
            {
                currentWeapon.transform.SetParent(mainHandSlot, false);
                currentWeapon.transform.localPosition = Vector3.zero;
                currentWeapon.transform.localRotation = Quaternion.identity;
            }
        }

        private void MountWeapon()
        {
            if (currentWeapon != null)
            {
                currentWeapon.transform.SetParent(weaponMountSlot, false);
                currentWeapon.transform.localPosition = Vector3.zero;
                currentWeapon.transform.localRotation = Quaternion.identity;
            }
        }

        public void DamageStart()
        {
            player!.AttackDamage(true);
            
            currentWeapon?.StartSlash();
        }

        public void DamageStop()
        {
            player!.AttackDamage(false);
            
            currentWeapon?.EndSlash();
        }

        public void StageEnding()
        {
            player!.ClearAttackCooldown();
        }

        void Start()
        {
            player = GetComponentInParent<PlayerController>();
            player.OnWeaponStateChanged += (weaponState) => {
                switch (weaponState)
                {
                    case PlayerController.WeaponState.Hold:
                        HoldWeapon();
                        break;
                    case PlayerController.WeaponState.Mount:
                        MountWeapon();
                        break;
                }
            };
        }

        void Update()
        {
            if (spineRef == null || weaponMountPivot == null)
                return;

            weaponMountPivot.localEulerAngles = new Vector3(-spineRef.localEulerAngles.x, 0F, 0F);
        }
    }
}