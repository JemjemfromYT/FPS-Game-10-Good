using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// ================================================================
//  ExplodingEnemy.cs
//
//  HOW TO SET UP (only 3 steps):
//
//  STEP 1 — Replace Assets/Scripts/EnemyAi.cs with the new one
//           (only 4 lines changed inside TakeDamage)
//
//  STEP 2 — Add THIS script to your ZombieExploder prefab
//           (the same prefab that already has EnemyAi on it)
//
//  STEP 3 — In the Inspector, fill in the fields below:
//           • Body Renderer  → drag the enemy Capsule's Mesh Renderer here
//           • Fuse Sound     → a ticking AudioClip (can leave empty)
//           • Explosion Sound→ a boom AudioClip (can leave empty)
//           • Explosion Effect Prefab → a particle prefab (can leave empty)
//
//  That's it. No other scripts need to be changed.
// ================================================================

public class ExplodingEnemy : MonoBehaviour
{
    [Header("Explosion")]
    [Tooltip("How far the blast reaches (Unity units)")]
    public float explosionRadius = 4f;

    [Tooltip("Max damage at the centre — drops off toward the edge")]
    public float explosionDamage = 80f;

    [Header("Proximity Trigger")]
    [Tooltip("How close the enemy gets before it starts the countdown")]
    public float triggerDistance = 2.5f;

    [Tooltip("Seconds from trigger to boom")]
    public float fuseTime = 1.5f;

    [Header("Appearance")]
    [Tooltip("Make it slightly bigger than a normal zombie so it stands out")]
    public float sizeMultiplier = 1.3f;

    [Header("Warning Flash")]
    [Tooltip("Drag the enemy Capsule's Mesh Renderer here")]
    public Renderer bodyRenderer;

    [Tooltip("How fast it flashes red (flashes per second)")]
    public float flashSpeed = 5f;

    [Header("Audio")]
    [Tooltip("Ticking/beeping sound during the countdown (optional)")]
    public AudioClip fuseSound;

    [Tooltip("Explosion boom sound (optional)")]
    public AudioClip explosionSound;

    [Header("VFX")]
    [Tooltip("Particle prefab spawned at the explosion point (optional)")]
    public GameObject explosionEffectPrefab;

    [Header("Camera Shake")]
    public float shakeMagnitude = 0.3f;
    public float shakeDuration = 0.25f;

    // ── private ──────────────────────────────────────────────────
    private EnemyAi _enemyAi;
    private NavMeshAgent _agent;
    private AudioSource _audio;
    private Transform _player;

    private bool _fuseStarted = false;
    private bool _exploded = false;

    private Material _bodyMat;
    private Color _originalColor;

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        _enemyAi = GetComponent<EnemyAi>();
        _agent = GetComponent<NavMeshAgent>();
        _audio = GetComponent<AudioSource>();

        // Find the player the same way EnemyAi does
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        // Auto-find the renderer if you didn't assign one in the Inspector
        if (bodyRenderer == null)
            bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<Renderer>();

        // Cache a per-instance copy of the body material for flashing
        if (bodyRenderer != null)
        {
            _bodyMat = bodyRenderer.material;   // creates instance copy — safe
            _originalColor = _bodyMat.color;          // remember the actual material colour
        }

        // Make it slightly bigger so it's easy to spot
        transform.localScale *= sizeMultiplier;
    }

    void Update()
    {
        if (_exploded || _player == null) return;

        // Start proximity countdown if not already started
        if (!_fuseStarted)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= triggerDistance)
                StartCoroutine(ProximityFuse());
        }
    }

    // ── Called by EnemyAi.TakeDamage when the enemy is shot to death ──
    // (This runs after the death flash animation finishes)
    public void TriggerExplosion()
    {
        Explode();
    }

    // ── Proximity fuse — runs when player walks too close ─────────
    private IEnumerator ProximityFuse()
    {
        _fuseStarted = true;

        // Freeze movement so enemy stops and locks in
        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = true;

        // Disable EnemyAi so it stops trying to attack during countdown
        if (_enemyAi != null)
            _enemyAi.enabled = false;

        // Start ticking sound
        if (_audio != null && fuseSound != null)
        {
            _audio.clip = fuseSound;
            _audio.loop = true;
            _audio.Play();
        }

        // Flash body red for fuseTime seconds
        float elapsed = 0f;
        while (elapsed < fuseTime)
        {
            if (_bodyMat != null)
            {
                float t = Mathf.PingPong(elapsed * flashSpeed, 1f);
                _bodyMat.color = Color.Lerp(_originalColor, Color.red, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        Explode();
    }

    // ── Core explosion logic ───────────────────────────────────────
    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        // Stop ticking
        if (_audio != null) _audio.Stop();

        // Boom sound — played at world position so it survives Destroy
        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);

        // Spawn the explosion visual effect (no prefab needed — built in code)
        ExplosionEffect.Spawn(transform.position, explosionRadius);

        // Also spawn optional prefab if you assigned one in the Inspector
        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        // Camera shake — uses the CameraShake script on your Main Camera
        CameraShake shake = Camera.main?.GetComponent<CameraShake>();
        if (shake != null)
            shake.TriggerShake(shakeMagnitude, shakeDuration);

        // Damage everything inside the explosion radius
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            float factor = 1f - Mathf.Clamp01(dist / explosionRadius); // 1 at centre → 0 at edge
            float dmg = explosionDamage * factor;

            // Damage the player
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(dmg);
                continue;
            }

            // Damage other enemies with EnemyHealth
            EnemyHealth eh = hit.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                eh.TakeDamage(dmg);
                continue;
            }

            // Damage other enemies with EnemyAi (but not ourselves)
            EnemyAi ea = hit.GetComponent<EnemyAi>();
            if (ea != null && ea != _enemyAi)
                ea.TakeDamage(dmg);
        }

        Destroy(gameObject);
    }

    // ── Draw radius in Scene view (Gizmos must be ON) ─────────────
    void OnDrawGizmosSelected()
    {
        // Orange = explosion radius
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Red = proximity trigger distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}
