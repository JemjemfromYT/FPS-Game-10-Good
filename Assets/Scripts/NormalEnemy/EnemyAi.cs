// ============================================================
//  EnemyAi.cs  —  UPDATED (replaces existing EnemyAi.cs)
//  Place in: Assets/Scripts/EnemyAi.cs
//
//  WHAT CHANGED vs the original:
//  One extra check in TakeDamage — if this enemy has a
//  SplitterEnemy component, its TriggerSplit() is called on
//  death instead of (or in addition to) the normal destroy.
//  Everything else is identical to the original.
// ============================================================

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

        // --- DEATH CALLBACK: pick the right handler ---
        // Priority: ExplodingEnemy > SplitterEnemy > default destroy
        System.Action deathCallback;

        ExplodingEnemy exploder = GetComponent<ExplodingEnemy>();
        SplitterEnemy splitter = GetComponent<SplitterEnemy>();

        if (exploder != null)
        {
            // Exploding enemy handles its own destroy inside TriggerExplosion
            deathCallback = exploder.TriggerExplosion;
        }
        else if (splitter != null)
        {
            // Splitter spawns minis then destroys itself
            deathCallback = () =>
            {
                splitter.TriggerSplit();
                Destroy(gameObject);
            };
        }
        else
        {
            deathCallback = () => Destroy(gameObject);
        }
        // -----------------------------------------------

        hurtVisual.PlayHitFlash(isKillingBlow, isKillingBlow ? deathCallback : null);

        if (isKillingBlow)
        {
            isDead = true;
            GlobalStats.money += 25;

            if (agent != null && agent.isOnNavMesh)
                agent.isStopped = true;
        }
    }
}
