using UnityEngine;
using TMPro;

public class GlobalAmmo : MonoBehaviour
{
    public static int pistolClip = 0;
    public static int pistolReserve = 0;

    public static int rifleClip = 0;
    public static int rifleReserve = 0;

    public static int smgClip = 0;
    public static int smgReserve = 0;

    public static int shotgunClip = 0;
    public static int shotgunReserve = 0;

    public static int sniperClip = 0;
    public static int sniperReserve = 0;

    [SerializeField] private TMP_Text ammoDisplayText;

    // Resets all ammo statics to 0 every Play session so
    // ApplyConfig() always runs and Inspector values are respected.
    void Awake()
    {
        pistolClip = 0;
        pistolReserve = 0;
        rifleClip = 0;
        rifleReserve = 0;
        smgClip = 0;
        smgReserve = 0;
        shotgunClip = 0;
        shotgunReserve = 0;
        sniperClip = 0;
        sniperReserve = 0;
    }

    void Update()
    {
        if (ammoDisplayText == null) return;

        WeaponFire activeWeapon = WeaponFireUtility.FindEquippedWeapon();
        if (activeWeapon != null)
        {
            ammoDisplayText.gameObject.SetActive(true);
            activeWeapon.WriteAmmoToText(ammoDisplayText);
        }
        else
        {
            ammoDisplayText.gameObject.SetActive(false);
        }
    }

    public static int GetClip(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol: return pistolClip;
            case AmmoType.Rifle: return rifleClip;
            case AmmoType.SMG: return smgClip;
            case AmmoType.Shotgun: return shotgunClip;
            case AmmoType.Sniper: return sniperClip;
            default: return 0;
        }
    }

    public static int GetReserve(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol: return pistolReserve;
            case AmmoType.Rifle: return rifleReserve;
            case AmmoType.SMG: return smgReserve;
            case AmmoType.Shotgun: return shotgunReserve;
            case AmmoType.Sniper: return sniperReserve;
            default: return 0;
        }
    }

    public static void SetClip(AmmoType type, int value)
    {
        switch (type)
        {
            case AmmoType.Pistol: pistolClip = value; break;
            case AmmoType.Rifle: rifleClip = value; break;
            case AmmoType.SMG: smgClip = value; break;
            case AmmoType.Shotgun: shotgunClip = value; break;
            case AmmoType.Sniper: sniperClip = value; break;
        }
    }

    public static void SetReserve(AmmoType type, int value)
    {
        switch (type)
        {
            case AmmoType.Pistol: pistolReserve = value; break;
            case AmmoType.Rifle: rifleReserve = value; break;
            case AmmoType.SMG: smgReserve = value; break;
            case AmmoType.Shotgun: shotgunReserve = value; break;
            case AmmoType.Sniper: sniperReserve = value; break;
        }
    }

    public static void AddReserve(AmmoType type, int amount)
    {
        SetReserve(type, Mathf.Max(0, GetReserve(type) + amount));
    }
}

public static class WeaponFireUtility
{
    public static WeaponFire FindEquippedWeapon()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return null;

        Transform weaponContainer = null;
        Transform[] children = player.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == "Weapon_Container")
            {
                weaponContainer = child;
                break;
            }
        }

        if (weaponContainer == null) return null;

        WeaponFire[] weapons = weaponContainer.GetComponentsInChildren<WeaponFire>(true);
        foreach (WeaponFire weapon in weapons)
        {
            if (weapon.gameObject.activeInHierarchy && weapon.enabled)
                return weapon;
        }

        return null;
    }
}
