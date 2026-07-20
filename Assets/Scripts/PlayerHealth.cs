using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Elements")]
    public TextMeshProUGUI healthText;
    public Image damageOverlay;
    public float flashSpeed = 5f;
    public Color flashColor = new Color(1f, 0f, 0f, 0.4f);

    private bool wasDamaged = false;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    void Update()
    {
        if (wasDamaged)
        {
            damageOverlay.color = flashColor;
        }
        else
        {
            damageOverlay.color = Color.Lerp(damageOverlay.color, Color.clear, flashSpeed * Time.deltaTime);
        }
        wasDamaged = false;
    }

    public void TakeDamage(float amount)
    {
        wasDamaged = true;
        currentHealth -= amount;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Helper method to update standard UI text layout cleanly
    public void UpdateHealthUI()
    {
        if (healthText != null)
        {
            // Rounds it to a whole number layout (e.g., HP: 100)
            healthText.text = "HP: " + Mathf.RoundToInt(currentHealth);
        }
    }

    void Die()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}