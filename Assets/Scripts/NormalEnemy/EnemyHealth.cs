using UnityEngine;

[RequireComponent(typeof(EnemyHurtVisual))]
public class EnemyHealth : MonoBehaviour
{
    public float health = 50f;

    private EnemyHurtVisual hurtVisual;
    bool isDead;

    void Awake()
    {
        hurtVisual = GetComponent<EnemyHurtVisual>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        bool isKillingBlow = health <= 0;

        hurtVisual.PlayHitFlash(isKillingBlow, isKillingBlow ? OnDeathFlashComplete : null);

        if (isKillingBlow)
        {
            isDead = true;
        }
    }

    void OnDeathFlashComplete()
    {
        Debug.Log($"{gameObject.name} has died!");
        Destroy(gameObject);
    }
}
