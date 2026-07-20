using UnityEngine;
using TMPro;

public class GlobalStats : MonoBehaviour
{
    public static int money = 500;

    // Shop Upgrades Data
    public static float permanentSpeedUpgrade = 1.0f; // Multiplier for speed
    public static bool ownsM9 = false;                // Weapon unlock check

    [SerializeField] TMP_Text moneyText;

    void Update()
    {
        if (moneyText != null)
        {
            moneyText.text = "$" + money;
        }
    }
}