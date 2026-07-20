using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Exploding Enemy - attach this to your zombie prefab ALONGSIDE EnemyAI and EnemyHealth.
/// This script handles the explode-on-death / explode-on-proximity behaviour.
///
/// SETUP STEPS (in Unity):
///   1. Duplicate your existing Zombie prefab  ->  rename it "ZombieExploder"
///   2. Add THIS script to it
///   3. Assign the fields in the Inspector (see comments below)
///   4. Optionally create an "ExplosionEffect" prefab (particle + light) and assign it
///   5. Add ZombieExploder to your SpawnManager's enemy list for later waves
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]      // keeps working with your existing health system
public class ExplodingEnemy : MonoBehaviour
{
    [Header("Explosion Settings")]
    [Tooltip("How far the blast reaches")]
    public float explosionRadius = 4f;

    [Tooltip("Max damage at the centre of the blast (falls off with distance)")]
    public float explosionDamage = 80f;

    [Tooltip("How close the enemy gets before it auto-triggers the countdown")]
    public float triggerDistance = 2.5f;

    [Tooltip("Seconds between reaching trigger distance and the actual boom")]
    public float fuseTime = 1.5f;

    [Header("Flash / Warning")]
    [Tooltip("The Renderer whose material colour we flash red (assign the capsule mesh)")]
    public Renderer bodyRenderer;

    [Tooltip("How fast the warning flash pulses (flashes per second)")]
    public float flashSpeed = 4f;

    [Header("Audio")]
    [Tooltip("Beeping / ticking sound played during the fuse countdown")]
    public AudioClip fuseSound;

    [Tooltip("Explosion boom sound")]
    public AudioClip explosionSound;

    [Header("VFX")]
    [Tooltip("Optional particle prefab spawned at explosion point")]
    public GameObject explosionEffectPrefab;

    [Tooltip("Camera shake magnitude (uses CameraShake script if present on Main Camera)")]
    public float shakeMagnitude = 0.3f;

    [Tooltip("Camera shake duration in seconds")]
    public float shakeDuration = 0.2f;

    // ── internals ──────────────────────────────────────────────────────────────
    private Transform _player;
    private NavMeshAgent _agent;
    private EnemyHealth _health;
    private AudioSource _audio;
    private bool _fuseActive = false;
    private bool _hasExploded = false;
    private Color _originalColor;
    private Material _mat;

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<EnemyHealth>();
        _audio = GetComponent<AudioSource>();

        // Try to find the player the same way EnemyAI does
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        // Cache original colour so we can restore it between flashes
        if (bodyRenderer != null)
        {
            _mat = bodyRenderer.material;   // creates instance copy - safe
            _originalColor = _mat.color;
        }

        // Listen for the existing health system's death event so we explode on death too
        if (_health != null)
            _health.OnDeath += HandleDeath;
    }

    void Update()
    {
        if (_hasExploded || _player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        // Start the countdown when close enough
        if (!_fuseActive && dist <= triggerDistance)
            StartCoroutine(FuseCountdown());
    }

    void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        if (_health != null)
            _health.OnDeath -= HandleDeath;
    }

    // ── fuse & explosion ───────────────────────────────────────────────────────

    /// <summary>Called when the EnemyHealth script fires the OnDeath event.</summary>
    private void HandleDeath()
    {
        if (!_hasExploded)
            Explode();
    }

    private IEnumerator FuseCountdown()
    {
        _fuseActive = true;

        // Stop the NavMesh agent so the enemy "freezes" while counting down
        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = true;

        // Play fuse sound
        if (_audio != null && fuseSound != null)
        {
            _audio.clip = fuseSound;
            _audio.loop = true;
            _audio.Play();
        }

        float elapsed = 0f;
        while (elapsed < fuseTime)
        {
            // Flash body colour between red and original
            if (_mat != null)
            {
                float t = Mathf.PingPong(elapsed * flashSpeed, 1f);
                _mat.color = Color.Lerp(_originalColor, Color.red, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Explode();
    }

    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        // ── sound ──────────────────────────────────────────────────────────────
        if (explosionSound != null)
            // Detach and play so it survives the object being destroyed
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);

        // ── VFX ───────────────────────────────────────────────────────────────
        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        // ── camera shake ──────────────────────────────────────────────────────
        CameraShake shake = Camera.main?.GetComponent<CameraShake>();
        if (shake != null)
            shake.TriggerShake(shakeMagnitude, shakeDuration);

        // ── deal damage to everything in radius ───────────────────────────────
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            float factor = 1f - Mathf.Clamp01(dist / explosionRadius);   // 1 at centre, 0 at edge
            float dmg = explosionDamage * factor;

            // Player damage
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(dmg);
                continue;
            }

            // Other enemies (friendly fire from explosion, optional)
            EnemyHealth eh = hit.GetComponent<EnemyHealth>();
            if (eh != null && eh != _health)
                eh.TakeDamage(dmg);
        }

        // Draw a debug sphere in the editor so you can see the radius
        Debug.DrawRay(transform.position, Vector3.up * explosionRadius, Color.red, 2f);

        // ── destroy this enemy ────────────────────────────────────────────────
        Destroy(gameObject);
    }

    // Draw radius in editor (visible in Scene view with Gizmos on)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, explosionRadius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}
