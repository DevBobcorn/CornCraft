#nullable enable
using UnityEngine;

using MinecraftClient.Control;


namespace MinecraftClient.Rendering
{
    public class PlayerAccessoryWidget : MonoBehaviour
    {
        [SerializeField] private Transform? mainHandBone;
        [SerializeField] public Transform? weaponRef;
        [SerializeField] public GameObject? meleeWeaponPrefab;

        private Transform? weaponTransform;
        private PlayerController? player;
        private MeleeWeapon? currentWeapon;

        private void HoldWeapon()
        {
            if (currentWeapon is not null)
            {
                currentWeapon.transform.SetParent(mainHandBone, true);
                currentWeapon.transform.localPosition = Vector3.zero;
                currentWeapon.transform.localRotation = Quaternion.identity;
                
            }
        }

        private void MountWeapon()
        {
            if (currentWeapon is not null)
            {
                currentWeapon.transform.SetParent(weaponTransform, true);
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
            if (weaponRef == null)
            {
                Debug.Log("Weapon transform reference not assigned!");
                
                weaponRef = transform;
            }

            // Create weapon transform
            var weaponTransformObject = new GameObject("Weapon Transform");
            weaponTransform = weaponTransformObject.transform;
            weaponTransform.SetParent(transform);

            if (meleeWeaponPrefab != null)
            {
                var weaponObj = GameObject.Instantiate(meleeWeaponPrefab);
                currentWeapon = weaponObj!.GetComponent<MeleeWeapon>();
            }

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

            weaponTransform.position = weaponRef.position;
            weaponTransform.rotation = weaponRef.rotation;

            MountWeapon();
        }

        void Update()
        {
            if (weaponRef == null || weaponTransform == null)
                return;

            weaponTransform.position = Vector3.Lerp(weaponTransform.position, weaponRef.position, Time.deltaTime * 10F);
            weaponTransform.rotation = weaponRef.rotation;
        }
    }
}