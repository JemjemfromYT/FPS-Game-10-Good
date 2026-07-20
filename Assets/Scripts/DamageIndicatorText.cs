using UnityEngine;
using TMPro;

public class DamageIndicatorText : MonoBehaviour
{
    private TextMeshProUGUI damageText;

    void Awake()
    {
        damageText = GetComponent<TextMeshProUGUI>();
    }

    public void SetDamageText(float damage)
    {
        damageText.text = Mathf.RoundToInt(damage).ToString();
    }
}