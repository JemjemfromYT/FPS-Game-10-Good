// ============================================================
//  EnemyProjectile.cs  —  NEW FILE
//  Place in: Assets/Scripts/EnemyProjectile.cs
//
//  This is the bullet/orb that RangedEnemy fires at the player.
//
//  HOW TO MAKE THE PROJECTILE PREFAB (2 minutes):
//  ─────────────────────────────────────────────────────────────
//  1. In the Hierarchy, right-click → 3D Object → Sphere.
//     Rename it "EnemyProjectile".
//
//  2. Scale it to (0.25, 0.25, 0.25) so it's small.
//
//  3. Give it a bright Material (e.g. red/orange) so it's visible.
//
//  4. Add a Rigidbody component:
//       ✓ Use Gravity → OFF (uncheck)
//       ✓ Is Kinematic → OFF
//
//  5. Make sure it has a Sphere Collider (it does by default).
//     Set "Is Trigger" → ON so it passes through walls
//     correctly (or OFF if you want it to bounce off walls).
//
//  6. Add THIS script (EnemyProjectile) to it.
//
//  7. Drag it from the Hierarchy into Assets/Prefab to make it
//     a prefab, then DELETE the Hierarchy copy.
//
//  8. In RangedEnemy Inspector → "Projectile Prefab" → drag the
//     new prefab in.
//
//  That's it. The ranged enemy will now fire real projectiles.
// ============================================================

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Set by RangedEnemy at launch — no need to set manually")]
    public float damage = 15f;

    [Tooltip("Units per second")]
    public float speed = 12f;

    [Tooltip("Destroy after this many seconds if nothing is hit")]
    public float lifetime = 5f;

    [Tooltip("Optional particle effect spawned on impact")]
    public GameObject impactEffectPrefab;

    [Tooltip("Optional sound played on impact")]
    public AudioClip impactSound;

    // ── private ──────────────────────────────────────────────────────────────
    private Rigidbody _rb;
    private Vector3 _direction;
    private bool _launched = false;
    private bool _hasHit = false;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;

        // Destroy automatically after lifetime so stray projectiles don't linger
        Destroy(gameObject, lifetime);
    }

    // Called by RangedEnemy.Shoot() right after Instantiate
    public void Launch(Vector3 direction)
    {
        _direction = direction.normalized;
        _launched = true;
        _rb.linearVelocity = _direction * speed;
    }

    void FixedUpdate()
    {
        if (!_launched || _hasHit) return;

        // Keep constant velocity (in case something slows it down)
        _rb.linearVelocity = _direction * speed;
    }

    // ── Trigger mode (Is Trigger = ON on the collider) ───────────────────────
    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    // ── Collision mode (Is Trigger = OFF) ────────────────────────────────────
    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    void HandleHit(GameObject other)
    {
        if (_hasHit) return;

        // Don't hit other enemies
        if (other.CompareTag("Enemy")) return;

        // Check if it hit the player
        PlayerHealth playerHealth =
            other.GetComponent<PlayerHealth>()
            ?? other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        // Also check if it hit another damageable thing (e.g. EnemyHealth)
        // so friendly-fire between enemy types is possible if you want it.
        // Comment this block out if you don't want projectiles to hurt other enemies.
        //EnemyHealth eh = other.GetComponent<EnemyHealth>() ?? other.GetComponentInParent<EnemyHealth>();
        //if (eh != null) eh.TakeDamage(damage);

        Impact();
    }

    void Impact()
    {
        _hasHit = true;

        // Spawn impact effect if assigned
        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

        // Play impact sound at world position (survives Destroy)
        if (impactSound != null)
            AudioSource.PlayClipAtPoint(impactSound, transform.position);

        Destroy(gameObject);
    }
}
