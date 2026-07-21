using UnityEngine;

public class StoreManager : MonoBehaviour
{
    [Header("UI Overlays")]
    public GameObject storePanel;

    [Header("Shop Delivery Floor Items")]
    public GameObject m9FloorPrefab;
    public GameObject ak47FloorPrefab;
    public GameObject mac10FloorPrefab;
    public GameObject shotgunFloorPrefab;
    public GameObject awpFloorPrefab;

    private GameObject actionText;
    private bool isStoreOpen = false;

    void Start()
    {
        if (storePanel != null) storePanel.SetActive(false);
        actionText = GameObject.Find("ActionText");
    }

    void Update()
    {
        if (!WaveManager.isWaveActive)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                ToggleStore();
            }
        }
        else if (isStoreOpen)
        {
            CloseStore();
        }
    }

    public void ToggleStore()
    {
        isStoreOpen = !isStoreOpen;
        if (storePanel != null) storePanel.SetActive(isStoreOpen);

        Cursor.lockState = isStoreOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isStoreOpen;
    }

    public void CloseStore()
    {
        isStoreOpen = false;
        if (storePanel != null) storePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --- BUTTON CLICK ROUTINES ---

    public void BuyM9(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            SpawnItemAtPlayer(m9FloorPrefab);
            CloseStore();
        }
    }

    public void BuyAK47(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            SpawnItemAtPlayer(ak47FloorPrefab);
            CloseStore();
        }
    }

    public void BuySpeedUpgrade(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            GlobalStats.permanentSpeedUpgrade += 0.2f;
            CloseStore();
        }
    }

    public void BuyHealth(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            PlayerHealth health = Object.FindAnyObjectByType<PlayerHealth>();
            if (health != null && health.currentHealth < health.maxHealth)
            {
                GlobalStats.money -= cost;
                health.currentHealth = health.maxHealth;
                health.UpdateHealthUI();
                CloseStore();
            }
        }
    }

    public void BuyPistolAmmo(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            GlobalAmmo.AddReserve(AmmoType.Pistol, 20);
            CloseStore();
        }
    }

    public void BuyRifleAmmo(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            GlobalAmmo.AddReserve(AmmoType.Rifle, 60);
            CloseStore();
        }
    }

    public void BuySMGAmmo(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            GlobalAmmo.AddReserve(AmmoType.SMG, 64);
            CloseStore();
        }
    }

    public void BuyShotgunAmmo(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            GlobalAmmo.AddReserve(AmmoType.Shotgun, 16);
            CloseStore();
        }
    }

    public void BuySniperAmmo(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            GlobalAmmo.AddReserve(AmmoType.Sniper, 10);
            CloseStore();
        }
    }

    public void BuyMac10(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            SpawnItemAtPlayer(mac10FloorPrefab);
            CloseStore();
        }
    }

    public void BuyShotgun(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            SpawnItemAtPlayer(shotgunFloorPrefab);
            CloseStore();
        }
    }

    public void BuyAWP(int cost)
    {
        if (GlobalStats.money >= cost)
        {
            GlobalStats.money -= cost;
            SpawnItemAtPlayer(awpFloorPrefab);
            CloseStore();
        }
    }

    private void SpawnItemAtPlayer(GameObject targetPrefab)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || targetPrefab == null) return;

        Vector3 spawnPosition = player.transform.position
                              + (player.transform.forward * 1.5f)
                              + (Vector3.up * 0.4f);
        GameObject deliveredWeapon = Instantiate(targetPrefab, spawnPosition, player.transform.rotation);

        WeaponPickup pickup = deliveredWeapon.GetComponent<WeaponPickup>();
        if (pickup != null && actionText != null)
        {
            pickup.actionText = actionText;
        }
    }
}
