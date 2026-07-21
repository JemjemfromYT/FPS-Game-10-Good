using UnityEngine;
using TMPro;

public class PlayerStatsDisplay : MonoBehaviour
{
    [Header("UI Layout Elements")]
    public GameObject statsPanel;
    public TMP_Text statsText;

    // Set to true by MobileControls.OnStatsButtonDown,
    // false by MobileControls.OnStatsButtonUp.
    public static bool MobileHeld = false;

    private PlayerHealth playerHealthScript;
    private PlayerStamina playerStaminaScript;

    void Start()
    {
        if (statsPanel != null) statsPanel.SetActive(false);
        FindPlayerScripts();
    }

    void FindPlayerScripts()
    {
        playerHealthScript = Object.FindAnyObjectByType<PlayerHealth>();
        playerStaminaScript = Object.FindAnyObjectByType<PlayerStamina>();
    }

    void Update()
    {
        // Show while Tab is held (keyboard) OR while mobile Stats button is held
        bool shouldShow = Input.GetKey(KeyCode.Tab) || MobileHeld;

        if (statsPanel != null) statsPanel.SetActive(shouldShow);

        if (shouldShow)
            UpdateStatsDisplay();
    }

    void UpdateStatsDisplay()
    {
        if (statsText == null) return;

        if (playerHealthScript == null || playerStaminaScript == null)
            FindPlayerScripts();

        float currentHP = (playerHealthScript != null) ? playerHealthScript.currentHealth : 0f;
        float currentStam = (playerStaminaScript != null) ? playerStaminaScript.currentStamina : 0f;
        float maxStam = (playerStaminaScript != null) ? playerStaminaScript.maxStamina : 100f;

        float baseSprintSpeed = 8.5f;
        float upgradedSprintSpeed = baseSprintSpeed * GlobalStats.permanentSpeedUpgrade;

        string currentWeaponName = "UNARMED";
        string currentWeaponAmmo = "0 / 0";

        WeaponFire activeWeapon = WeaponFireUtility.FindEquippedWeapon();
        if (activeWeapon != null)
        {
            currentWeaponName = activeWeapon.weaponName;
            currentWeaponAmmo = activeWeapon.GetAmmoDisplayString();
        }

        string layoutReport = "--- CHARACTER STATS ---\n\n";
        layoutReport += "HEALTH: " + Mathf.RoundToInt(currentHP) + "\n";
        layoutReport += "STAMINA: " + Mathf.RoundToInt(currentStam) + " / " + maxStam + "\n";
        layoutReport += "MOVEMENT SPEED: " + upgradedSprintSpeed.ToString("F1") + " m/s\n";
        layoutReport += "SPEED MULTIPLIER: " + GlobalStats.permanentSpeedUpgrade.ToString("F2") + "x\n\n";
        layoutReport += "--- CURRENT WEAPON ---\n";
        layoutReport += "EQUIPPED: " + currentWeaponName + "\n";
        layoutReport += "AMMO POOL: " + currentWeaponAmmo + "\n";

        statsText.text = layoutReport;
    }
}
