using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyHurtVisual))]
public class EnemyAi : MonoBehaviour
{
    public float health = 50f;
    private Transform playerTarget;
    private NavMeshAgent agent;

    [Header("Combat Settings")]
    public float damageAmount = 20f;
    public float attackCooldown = 1.0f;
    public float attackDistance = 1.5f;
    private float nextAttackTime = 0f;

    private PlayerHealth pHealth;
    private EnemyHurtVisual hurtVisual;
    bool isDead;

    void Awake()
    {
        hurtVisual = GetComponent<EnemyHurtVisual>();
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            pHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (playerTarget != null)
        {
            agent.SetDestination(playerTarget.position);

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            if (distanceToPlayer <= attackDistance && Time.time >= nextAttackTime)
            {
                if (pHealth != null)
                {
                    pHealth.TakeDamage(damageAmount);
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;
        bool isKillingBlow = health <= 0;

        // --- EXPLODING ENEMY SUPPORT ---
        // If this enemy has an ExplodingEnemy component, let it handle death
        // instead of just destroying the object normally.
        System.Action deathCallback;
        ExplodingEnemy exploder = GetComponent<ExplodingEnemy>();
        if (exploder != null)
            deathCallback = exploder.TriggerExplosion;
        else
            deathCallback = () => Destroy(gameObject);
        // --------------------------------

        hurtVisual.PlayHitFlash(isKillingBlow, isKillingBlow ? deathCallback : null);

        if (isKillingBlow)
        {
            isDead = true;
            GlobalStats.money += 25;
        }
    }
}
