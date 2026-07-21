// ============================================================
//  RangedEnemy.cs  —  NEW FILE
//  Place in: Assets/Scripts/RangedEnemy.cs
//
//  This is a brand-new enemy type that SHOOTS projectiles at the
//  player instead of running up and punching. It uses the same
//  NavMesh, EnemyHurtVisual, and tag system as your other enemies
//  so WaveManager.FindGameObjectsWithTag("Enemy") counts it correctly.
//
//  HOW TO SET UP (takes about 3 minutes):
//  ─────────────────────────────────────────────────────────────
//  1. In Unity, duplicate one of your existing enemy prefabs
//     (right-click Prefab → Duplicate). Rename it "ZombieRanged".
//
//  2. On ZombieRanged, REMOVE the EnemyAi component (it has its
//     own movement/attack logic). Keep:
//       ✓ NavMeshAgent
//       ✓ EnemyHurtVisual
//       ✓ Collider (Capsule Collider)
//       ✓ Rigidbody (if any)
//
//  3. Add THIS script (RangedEnemy) to ZombieRanged.
//
//  4. In the Inspector fields:
//       • Projectile Prefab → drag in EnemyProjectile prefab
//         (see EnemyProjectile_NEW.txt for how to make it)
//       • Shoot Sound      → any gunshot / throw AudioClip (optional)
//       • Everything else  → defaults are fine to start
//
//  5. Tag ZombieRanged as "Enemy" (same as your other enemies).
//
//  6. Add ZombieRanged to WaveManager's extraEnemyPrefabs array
//     OR to the zombiePrefab slot to always spawn it.
//
//  7. Bake or re-bake NavMesh if the new prefab uses a different
//     agent radius (Window → AI → Navigation → Bake).
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHurtVisual))]
public class RangedEnemy : MonoBehaviour
{
    [Header("Health")]
    public float health = 60f;

    [Header("Movement")]
    [Tooltip("How close it gets before it STOPS and starts shooting")]
    public float preferredRange = 8f;
    [Tooltip("If player gets closer than this, it backs up a little")]
    public float retreatRange = 4f;
    [Tooltip("Walk speed (NavMesh override)")]
    public float moveSpeed = 2.5f;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    [Tooltip("Seconds between each shot")]
    public float fireRate = 2.5f;
    [Tooltip("How much damage each projectile deals")]
    public float projectileDamage = 15f;
    [Tooltip("How fast the projectile flies (passed to EnemyProjectile)")]
    public float projectileSpeed = 12f;
    [Tooltip("Where the projectile spawns — leave empty to use this object's position")]
    public Transform firePoint;

    [Header("Audio")]
    public AudioClip shootSound;
    [Range(0f, 1f)] public float shootVolume = 0.8f;

    // ── private ──────────────────────────────────────────────────────────────
    private NavMeshAgent _agent;
    private EnemyHurtVisual _hurtVisual;
    private AudioSource _audio;
    private Transform _player;
    private PlayerHealth _playerHealth;

    private float _nextFireTime = 0f;
    private bool _isDead = false;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _hurtVisual = GetComponent<EnemyHurtVisual>();
        _audio = GetComponent<AudioSource>();

        _agent.speed = moveSpeed;

        // Locate the player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (_isDead || _player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        HandleMovement(dist);
        HandleShooting(dist);
    }

    // ── Movement: keep preferred range, retreat if too close ─────────────────
    void HandleMovement(float dist)
    {
        if (!_agent.isOnNavMesh) return;

        if (dist > preferredRange)
        {
            // Too far — close in
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
        }
        else if (dist < retreatRange)
        {
            // Too close — back away
            _agent.isStopped = false;
            Vector3 awayDir = (transform.position - _player.position).normalized;
            Vector3 retreatTarget = transform.position + awayDir * (retreatRange - dist + 1f);

            // Only retreat to valid NavMesh positions
            if (NavMesh.SamplePosition(retreatTarget, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }
        else
        {
            // In the sweet spot — stop and face the player
            _agent.isStopped = true;
            Vector3 lookDir = (_player.position - transform.position);
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 5f);
        }
    }

    // ── Shooting ──────────────────────────────────────────────────────────────
    void HandleShooting(float dist)
    {
        // Only shoot when within range and cooldown has passed
        if (dist > preferredRange * 1.2f) return;
        if (Time.time < _nextFireTime) return;

        _nextFireTime = Time.time + fireRate;
        Shoot();
    }

    void Shoot()
    {
        // Determine spawn position — use firePoint if assigned, else own position
        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : transform.position + Vector3.up * 1f;

        // Aim slightly ahead of the player's chest for believability
        Vector3 aimTarget = _player.position + Vector3.up * 0.8f;
        Vector3 aimDir = (aimTarget - spawnPos).normalized;

        if (projectilePrefab != null)
        {
            // Instantiate the prefab projectile
            GameObject proj = Instantiate(
                projectilePrefab,
                spawnPos,
                Quaternion.LookRotation(aimDir));

            EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
            if (ep != null)
            {
                ep.damage = projectileDamage;
                ep.speed = projectileSpeed;
                ep.Launch(aimDir);
            }
        }
        else
        {
            // No prefab assigned — spawn a simple primitive projectile
            SpawnPrimitiveProjectile(spawnPos, aimDir);
        }

        // Play shoot sound
        if (_audio != null && shootSound != null)
            _audio.PlayOneShot(shootSound, shootVolume);
    }

    // ── Fallback: create a visible projectile without a prefab ───────────────
    void SpawnPrimitiveProjectile(Vector3 spawnPos, Vector3 aimDir)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "EnemyShot_Auto";
        go.transform.position = spawnPos;
        go.transform.localScale = Vector3.one * 0.25f;

        // Give it a red material so the player can see it
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            r.material.color = Color.red;
        }

        // Replace the StaticCollider with a Rigidbody-friendly collider
        Destroy(go.GetComponent<SphereCollider>());
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.radius = 0.5f;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;

        EnemyProjectile ep = go.AddComponent<EnemyProjectile>();
        ep.damage = projectileDamage;
        ep.speed = projectileSpeed;
        ep.Launch(aimDir);
    }

    // ── Called by WeaponFire (same as EnemyAi.TakeDamage) ────────────────────
    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        health -= damage;
        bool isKillingBlow = health <= 0;

        _hurtVisual.PlayHitFlash(isKillingBlow, isKillingBlow ? OnDeath : (System.Action)null);

        if (isKillingBlow)
        {
            _isDead = true;
            GlobalStats.money += 25;

            if (_agent != null && _agent.isOnNavMesh)
                _agent.isStopped = true;
        }
    }

    void OnDeath()
    {
        Destroy(gameObject);
    }

    // ── Gizmos — see ranges in Scene view ────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        // Green = preferred shooting range
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, preferredRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, preferredRange);

        // Yellow = retreat trigger
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, retreatRange);
    }
}
