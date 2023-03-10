#nullable enable
using UnityEngine;

public class PlayerAccessoryWidget : MonoBehaviour
{
    public Transform? mainHandBone;

    public Transform? weaponRef;
    private Vector3 weaponPosition;
    public Transform? weaponSlotBone;

    public GameObject? currentWeapon;

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

    void Start()
    {
        if (weaponRef is null || weaponSlotBone is null)
        {
            Debug.Log("Weapon transforms are not assigned!");
            return;
        }

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
