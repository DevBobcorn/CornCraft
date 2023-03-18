#nullable enable
using UnityEngine;

using MinecraftClient.Control;

namespace MinecraftClient.Rendering
{
public class PlayerAccessoryWidget : MonoBehaviour
{
        private PlayerController? player;

        public Transform? mainHandBone;

        public Transform? weaponRef;
        private Vector3 weaponPosition;
        public Transform? weaponSlotBone;

        public GameObject? meleeWeaponPrefab;
        private MeleeWeapon? currentWeapon;

        public void HoldWeapon()
        {
            if (currentWeapon is not null)
            {
                currentWeapon.transform.SetParent(mainHandBone, true);
                currentWeapon.transform.localPosition = Vector3.zero;
                currentWeapon.transform.localRotation = Quaternion.identity;
                
            }
        }

        public void MountWeapon()
        {
            if (currentWeapon is not null)
            {
                currentWeapon.transform.SetParent(weaponSlotBone, true);
                currentWeapon.transform.localPosition = Vector3.zero;
                currentWeapon.transform.localRotation = Quaternion.identity;

            }
        }

        public void DamageStart()
        {
            player!.AttackDamage(true);
            
            currentWeapon!.StartSlash();
        }

        public void DamageStop()
        {
            player!.AttackDamage(false);

            player.DealDamage(currentWeapon!.EndSlash());
        }

        public void StageEnding()
        {
            player!.ClearAttackCooldown();
        }

        void Start()
        {
            if (weaponRef is null || weaponSlotBone is null)
            {
                Debug.Log("Weapon transforms are not assigned!");
                return;
            }

            var weaponObj = GameObject.Instantiate(meleeWeaponPrefab);
            currentWeapon = weaponObj!.GetComponent<MeleeWeapon>();

            player = GetComponentInParent<PlayerController>();
            weaponPosition = weaponSlotBone.position;

            weaponSlotBone.position = weaponPosition;
            weaponSlotBone.rotation = weaponRef.rotation;

            MountWeapon();
        }

        void Update()
        {
            if (weaponRef is null || weaponSlotBone is null)
                return;

            weaponPosition = Vector3.Lerp(weaponPosition, weaponRef.position, Time.deltaTime * 10F);

            weaponSlotBone.position = weaponPosition;
            weaponSlotBone.rotation = weaponRef.rotation;
        }
    }
}