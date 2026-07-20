using UnityEngine;

[System.Serializable]
public class WeaponFloorPrefabPair
{
    public string weaponObjectName;
    public GameObject floorPickupPrefab;
}

public class WeaponContainerSetup : MonoBehaviour
{
    [SerializeField] WeaponFloorPrefabPair[] floorPrefabs;
    [SerializeField] GameObject damageIndicatorPrefab;

    Transform playerCameraTransform;
    GameObject actionTextObject;

    void Awake()
    {
        CachePlayerReferences();
        ConfigureAllChildren();
    }

    void Start()
    {
        ConfigureAllChildren();
    }

    void CachePlayerReferences()
    {
        if (actionTextObject == null)
        {
            actionTextObject = GameObject.Find("ActionText");
        }

        if (playerCameraTransform != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Camera cam = player.GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            playerCameraTransform = cam.transform;
        }
    }

    public GameObject GetFloorPrefab(string weaponObjectName)
    {
        if (floorPrefabs == null) return null;

        foreach (WeaponFloorPrefabPair pair in floorPrefabs)
        {
            if (pair.weaponObjectName == weaponObjectName)
            {
                return pair.floorPickupPrefab;
            }
        }

        return null;
    }

    public void ConfigureAllChildren()
    {
        CachePlayerReferences();

        WeaponFire[] weapons = GetComponentsInChildren<WeaponFire>(true);
        foreach (WeaponFire weapon in weapons)
        {
            ConfigureWeapon(weapon.gameObject);
        }

        foreach (Transform child in transform)
        {
            if (child.GetComponent<WeaponFire>() != null) continue;
            if (!WeaponDefinitions.TryGetConfig(child.name, out _)) continue;
            ConfigureWeapon(child.gameObject);
        }
    }

    void ConfigureWeapon(GameObject weaponObject)
    {
        if (!WeaponDefinitions.TryGetConfig(weaponObject.name, out WeaponDefinitions.WeaponConfig config))
        {
            return;
        }

        WeaponFire fire = weaponObject.GetComponent<WeaponFire>();
        if (fire == null) fire = weaponObject.AddComponent<WeaponFire>();
        fire.ApplyConfig(config);
        fire.SetDamageIndicatorPrefab(damageIndicatorPrefab);

        WeaponDrop drop = weaponObject.GetComponent<WeaponDrop>();
        if (drop == null) drop = weaponObject.AddComponent<WeaponDrop>();

        GameObject floorPrefab = GetFloorPrefab(weaponObject.name);
        if (floorPrefab != null) drop.floorPrefab = floorPrefab;

        if (playerCameraTransform != null) drop.playerCamera = playerCameraTransform;
        if (actionTextObject != null) drop.actionText = actionTextObject;

        drop.EnsureDropReferences();

        if (weaponObject.GetComponent<Animator>() == null)
        {
            weaponObject.AddComponent<Animator>();
        }
    }
}
