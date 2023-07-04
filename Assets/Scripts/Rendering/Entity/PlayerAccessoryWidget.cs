#nullable enable
using UnityEngine;

using MinecraftClient.Control;


namespace MinecraftClient.Rendering
{
    public class PlayerAccessoryWidget : MonoBehaviour
    {
        [HideInInspector] public Transform? mainHandRef;
        [HideInInspector] public Transform? weaponMountRef;
        private Transform? mainHandSlot; // A slot fixed to mainHandRef transform (as a child)
        private Transform? weaponMountSlot; // A slot smooth following weaponMountRef transform
        private PlayerController? player;
        private MeleeWeapon? currentWeapon;

        public void SetRefTransforms(Transform mainHandRef, Transform weaponMountRef)
        {
            this.mainHandRef = mainHandRef;
            // Create weapon slot transform
            var mainHandSlotObj = new GameObject("Main Hand Slot");
            mainHandSlot = mainHandSlotObj.transform;
            mainHandSlot.SetParent(mainHandRef);
            // Initialize position and rotation
            mainHandSlot.localPosition = Vector3.zero;
            mainHandSlot.localEulerAngles = Vector3.zero;

            this.weaponMountRef = weaponMountRef;
            // Create weapon slot transform
            var weaponSlotObj = new GameObject("Weapon Slot");
            weaponMountSlot = weaponSlotObj.transform;
            weaponMountSlot.SetParent(transform);
            // Initialize position and rotation
            weaponMountSlot.position = weaponMountRef!.position;
            weaponMountSlot.rotation = weaponMountRef.rotation;
        }

        public void CreateWeapon(GameObject weaponPrefab)
        {
            if (currentWeapon != null)
            {
                Destroy(currentWeapon);
            }

            var weaponObj = GameObject.Instantiate(weaponPrefab);
            currentWeapon = weaponObj!.GetComponent<MeleeWeapon>();

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
            if (weaponMountRef == null || weaponMountSlot == null)
                return;

            weaponMountSlot.position = Vector3.Lerp(weaponMountSlot.position, weaponMountRef.position, Time.deltaTime * 10F);
            weaponMountSlot.rotation = weaponMountRef.rotation;
        }
    }
}