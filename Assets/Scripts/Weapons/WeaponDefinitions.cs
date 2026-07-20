using UnityEngine;

public static class WeaponDefinitions
{
    public struct WeaponConfig
    {
        public string[] objectNames;
        public AmmoType ammoType;
        public string displayName;
        public int maxClipSize;
        public int startClip;
        public int startReserve;
        public float fireRate;
        public float reloadDuration;
        public bool isAutomatic;
        public float weaponSpread; // Added for weapon spread
        public float weaponDamage; // Added for weapon damage
    }

    public static readonly WeaponConfig[] All =
    {
        new WeaponConfig
        {
            objectNames = new[] { "Weapon_M9", "Weapon_M9 1" },
            ammoType = AmmoType.Pistol,
            displayName = "M9",
            maxClipSize = 7,
            startClip = 7,
            startReserve = 30,
            fireRate = 1f,
            reloadDuration = 3.37f,
            isAutomatic = false,
            weaponSpread = 0.02f,
            weaponDamage = 20f
        },
        new WeaponConfig
        {
            objectNames = new[] { "Weapon_Ak47", "Weapon_AK47" },
            ammoType = AmmoType.Rifle,
            displayName = "AK47",
            maxClipSize = 30,
            startClip = 30,
            startReserve = 90,
            fireRate = 0.3f,
            reloadDuration = 3.2f,
            isAutomatic = true,
            weaponSpread = 0.05f,
            weaponDamage = 25f
        },
        new WeaponConfig
        {
            objectNames = new[] { "Weapon_Mac10", "Weapon_MAC10", "mac10" },
            ammoType = AmmoType.SMG,
            displayName = "Mac10",
            maxClipSize = 32,
            startClip = 32,
            startReserve = 96,
            fireRate = 0.08f,
            reloadDuration = 2.5f,
            isAutomatic = true,
            weaponSpread = 0.08f,
            weaponDamage = 15f
        },
        new WeaponConfig
        {
            objectNames = new[] { "Weapon_ChromeShotgun", "Weapon_Shotgun", "ChromeShotgun", "chromeshotgun" },
            ammoType = AmmoType.Shotgun,
            displayName = "Shotgun",
            maxClipSize = 8,
            startClip = 8,
            startReserve = 32,
            fireRate = 0.9f,
            reloadDuration = 3.5f,
            isAutomatic = false,
            
            // INCREASED TO 10f FOR PROPER PELLET SCATTER AND CROSSHAIR SCALING
            weaponSpread = 10f,

            weaponDamage = 50f
        },
        new WeaponConfig
        {
            objectNames = new[] { "Weapon_AWP", "Weapon_Awp", "awp" },
            ammoType = AmmoType.Sniper,
            displayName = "AWP",
            maxClipSize = 5,
            startClip = 5,
            startReserve = 20,
            fireRate = 1.2f,
            reloadDuration = 3.8f,
            isAutomatic = false,
            weaponSpread = 0.005f,
            weaponDamage = 100f
        }
    };

    public static bool TryGetConfig(string objectName, out WeaponConfig config)
    {
        foreach (WeaponConfig entry in All)
        {
            foreach (string alias in entry.objectNames)
            {
                if (string.Equals(objectName, alias, System.StringComparison.OrdinalIgnoreCase))
                {
                    config = entry;
                    return true;
                }
            }
        }

        config = default;
        return false;
    }
}