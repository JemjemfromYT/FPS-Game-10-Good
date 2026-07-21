// ============================================================
//  WeaponFire_FIXED.cs
//  Replace Assets/Scripts/Weapons/WeaponFire.cs with this file.
//
//  BUG FIXED: "No damage when shooting while walking backward"
//  ---------------------------------------------------------------
//  ROOT CAUSE:
//  Unity's Cinemachine camera updates in LateUpdate, but Shoot()
//  runs during Update. When you walk BACKWARD the CharacterController
//  already moves the player capsule backward (in Update → Move()),
//  but the camera hasn't caught up yet (it catches up in LateUpdate).
//  This puts the camera IN FRONT OF the player's own capsule for
//  one frame, so Physics.Raycast immediately hits the front face of
//  the player's own CharacterController collider instead of the enemy.
//
//  When walking FORWARD the capsule moves ahead of the camera, so the
//  ray starts inside/behind the capsule — Unity ignores colliders the
//  ray originates inside, so it passes through harmlessly.
//
//  THE FIX:
//  1. A new serialized LayerMask field "shootLayerMask" (defaults to
//     Everything) is passed to Physics.Raycast so the player's own
//     layer can be excluded in the Inspector.
//  2. A "Player" tag check skips self-hits even if the layer is not
//     configured yet — belt-and-suspenders safety net.
//
//  HOW TO SET UP IN UNITY (one-time, takes 30 seconds):
//  a) In Edit → Project Settings → Tags and Layers, add a new layer
//     called "Player" (e.g. Layer 6).
//  b) Select your Player GameObject in the Hierarchy and set its Layer
//     to "Player" (apply to children too if asked).
//  c) Select your weapon GameObject, find the WeaponFire component in
//     the Inspector, and in the "Shoot Settings" section uncheck the
//     "Player" layer from the Shoot Layer Mask field.
//  Done. The raycast will now skip the player capsule entirely.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

public class WeaponFire : MonoBehaviour
{
    const int ShotgunPelletCount = 8;

    [Header("Weapon Configurations")]
    [FormerlySerializedAs("weaponType")]
    [SerializeField] AmmoType ammoType = AmmoType.Pistol;
    public string weaponName = "M9";
    [SerializeField] int maxClipSize = 7;
    [SerializeField] float fireRate = 0.2f;
    [SerializeField] float reloadDuration = 3.2f;
    [SerializeField] bool isAutomatic = false;
    [SerializeField] float weaponSpread = 0f;
    [SerializeField] float weaponDamage = 10f;

    [Header("Animation States (optional until you add clips)")]
    [SerializeField] string fireAnimationName = "";
    [SerializeField] string reloadAnimationName = "";

    [Header("References")]
    [SerializeField] AudioSource gunFireSound;
    [SerializeField] GameObject weaponMeshModel;

    [FormerlySerializedAs("crosshairDot")]
    [SerializeField] GameObject crosshair;

    [SerializeField] AudioSource emptyGunSound;
    [SerializeField] GameObject damageIndicatorPrefab;

    [Header("Bullet Tracer")]
    [SerializeField] bool showBulletTracer = true;
    [SerializeField] BulletTracer bulletTracerPrefab;
    [SerializeField] float bulletTracerLifetime = 0.05f;
    [SerializeField] float bulletTracerMaxDistance = 120f;
    [SerializeField] Transform bulletTracerOrigin;

    // ── BUG FIX: backward-walking no-damage ──────────────────────────────────
    // Set this in the Inspector: uncheck the "Player" layer so the raycast
    // never hits the player's own CharacterController capsule.
    // Defaults to ~0 (Everything) so the weapon works out-of-the-box even
    // before you configure the layer — the tag check below acts as a fallback.
    [Header("Shoot Settings")]
    [Tooltip("Layers the shoot raycast can hit. UNCHECK the Player layer to fix " +
             "no-damage when walking backward (camera leads the capsule by one frame).")]
    [SerializeField] LayerMask shootLayerMask = ~0;
    // ─────────────────────────────────────────────────────────────────────────

    private float nextFireTime = 0f;
    private bool isReloading = false;

    Animator gunAnimator;
    AudioClip cachedFireClip;
    CrosshairSpreadDisplay _cachedSpreadDisplay;

    // MobileControls.Start() sets this true so WeaponFire skips all
    // Input.GetButton polling — on mobile any screen touch maps to mouse
    // button 0 and would fire the weapon unintentionally.
    public static bool MobileMode = false;

    public AmmoType AmmoType => ammoType;
    public int MaxClipSize => maxClipSize;
    public bool IsReloading => isReloading;
    public bool CanShoot => Time.time >= nextFireTime && !isReloading;
    public bool IsWeaponBusy => isReloading || Time.time < nextFireTime;
    public float WeaponSpread => weaponSpread;
    public float SpreadDegrees => WeaponSpreadUtility.OffsetToDegrees(weaponSpread);
    public bool IsAutomatic => isAutomatic;

    void Start()
    {
        CacheAnimator();
        CacheFireSound();
    }

    void CacheFireSound()
    {
        if (gunFireSound == null) return;
        cachedFireClip = gunFireSound.clip;
        if (cachedFireClip == null && gunFireSound.resource != null)
            cachedFireClip = gunFireSound.resource as AudioClip;
    }

    void OnEnable()
    {
        CacheFireSound();
        ResetAnimatorPose();
        if (crosshair != null) crosshair.SetActive(true);
    }

    void OnDisable()
    {
        CancelWeaponActions();
        if (crosshair != null) crosshair.SetActive(false);
    }

    /// <summary>
    /// Called by WeaponContainerSetup to initialise ammo for this weapon.
    /// FIX (wrong ammo): uses maxClipSize (the Inspector field) as the starting
    /// clip so the value the user sets in the Inspector is always respected.
    /// The config's startReserve is still used for the reserve pool.
    /// </summary>
    public void ApplyConfig(WeaponDefinitions.WeaponConfig config)
    {
        if (GlobalAmmo.GetClip(ammoType) == 0 && GlobalAmmo.GetReserve(ammoType) == 0)
        {
            // maxClipSize is the Inspector value — use it, not config.startClip.
            SetClip(maxClipSize);
            SetReserve(config.startReserve);
        }
    }

    public void SetDamageIndicatorPrefab(GameObject prefab)
    {
        if (prefab != null) damageIndicatorPrefab = prefab;
    }

    void CacheAnimator()
    {
        if (gunAnimator != null) return;
        if (weaponMeshModel != null) gunAnimator = weaponMeshModel.GetComponent<Animator>();
        if (gunAnimator == null) gunAnimator = GetComponent<Animator>();
    }

    public void CancelWeaponActions()
    {
        StopAllCoroutines();
        isReloading = false;
        ResetAnimatorPose();
    }

    void ResetAnimatorPose()
    {
        CacheAnimator();
        if (gunAnimator == null) return;
        if (!gunAnimator.gameObject.activeInHierarchy) return;
        gunAnimator.Rebind();
        gunAnimator.Update(0f);
    }

    void Update()
    {
        UpdateCrosshairSpread();
        if (isReloading) return;

        // Skip all mouse / keyboard input on mobile — the Fire button calls
        // Shoot() directly via FireButton.cs (IPointerDownHandler).
        if (!MobileMode)
        {
            bool isFiring = isAutomatic
                ? Input.GetButton("Fire1")
                : Input.GetButtonDown("Fire1");

            if (isFiring && CanShoot)
            {
                if (GetCurrentClip() > 0)
                    Shoot();
                else if (Input.GetButtonDown("Fire1") && emptyGunSound != null)
                    emptyGunSound.Play();
            }

            if (Input.GetKeyDown(KeyCode.R) && CanShoot)
            {
                if (GetCurrentClip() < maxClipSize && GetCurrentReserve() > 0)
                    StartCoroutine(Reload());
            }
        }
    }

    void UpdateCrosshairSpread()
    {
        if (crosshair == null) return;
        Camera camera = Camera.main;
        if (camera == null) return;

        if (_cachedSpreadDisplay == null)
        {
            _cachedSpreadDisplay = crosshair.GetComponent<CrosshairSpreadDisplay>();
            if (_cachedSpreadDisplay == null)
                _cachedSpreadDisplay = crosshair.AddComponent<CrosshairSpreadDisplay>();
        }

        _cachedSpreadDisplay.ApplySpread(SpreadDegrees, camera);
    }

    /// <summary>
    /// Public so the mobile FireButton.cs can call it directly on PointerDown.
    /// FIX (fires with no ammo): guards against empty clip and plays the empty-
    /// gun click (throttled so it does not spam every frame on hold).
    /// </summary>
    public void Shoot()
    {
        if (isReloading || Time.time < nextFireTime) return;

        // FIX: check ammo BEFORE consuming it or playing effects.
        if (GetCurrentClip() <= 0)
        {
            // Throttle the empty-gun click using the fire rate so it doesn't
            // spam the sound every frame when the player holds the button.
            nextFireTime = Time.time + Mathf.Max(fireRate, 0.3f);
            if (emptyGunSound != null) emptyGunSound.Play();
            return;
        }

        nextFireTime = Time.time + fireRate;
        ModifyClip(-1);
        PlayFireSound();
        PlayAnimation(fireAnimationName);

        Camera camera = Camera.main;
        if (camera == null) return;

        Transform cameraTransform = camera.transform;
        bool isShotgun = IsShotgunWeapon();
        int hitCount = isShotgun ? ShotgunPelletCount : 1;
        float damagePerHit = isShotgun ? weaponDamage / ShotgunPelletCount : weaponDamage;

        // Pass the barrel Transform so BulletTracer tracks it each frame
        // (prevents the tracer lagging behind at high movement speed).
        Transform originTransform = bulletTracerOrigin;
        Vector3 tracerStart = originTransform != null
            ? originTransform.position
            : cameraTransform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Vector3 direction = GetSpreadDirection(cameraTransform);

            // ── BUG FIX: backward-walking no-damage ──────────────────────────
            // Pass shootLayerMask so the player's own CharacterController
            // capsule is excluded from the raycast. Without this mask,
            // Cinemachine's one-frame camera lag (LateUpdate vs Update) causes
            // the camera to sit just in front of the capsule when walking
            // backward, and the ray hits the player's own collider first.
            bool hasHit = Physics.Raycast(cameraTransform.position, direction,
                                          out RaycastHit hit, Mathf.Infinity,
                                          shootLayerMask);

            // Secondary safety net: skip if we somehow still hit the player
            // (works even when the "Player" layer is not configured yet).
            if (hasHit && hit.collider.CompareTag("Player"))
                hasHit = false;
            // ─────────────────────────────────────────────────────────────────

            Vector3 tracerEnd = hasHit
                ? hit.point
                : cameraTransform.position + direction * bulletTracerMaxDistance;

            if (showBulletTracer)
                BulletTracerPool.Spawn(tracerStart, originTransform, tracerEnd,
                                       bulletTracerPrefab, bulletTracerLifetime);

            if (!hasHit) continue;

            if (HandleEnemyDamage(hit, damagePerHit))
                ApplyDamageIndicator(hit.point, damagePerHit);
        }
    }

    bool IsShotgunWeapon()
    {
        return ammoType == AmmoType.Shotgun
            || weaponName.Equals("ChromeShotgun", System.StringComparison.OrdinalIgnoreCase)
            || weaponName.Equals("Shotgun", System.StringComparison.OrdinalIgnoreCase)
            || gameObject.name.Equals("Weapon_ChromeShotgun", System.StringComparison.OrdinalIgnoreCase)
            || gameObject.name.Contains("Shotgun");
    }

    bool HandleEnemyDamage(RaycastHit hit, float damage)
    {
        if (TryGetEnemyHealth(hit, out EnemyHealth enemyHealth))
        {
            enemyHealth.TakeDamage(damage);
            return true;
        }
        if (TryGetEnemyAi(hit, out EnemyAi enemyAi))
        {
            enemyAi.TakeDamage(damage);
            return true;
        }
        return false;
    }

    static bool TryGetEnemyHealth(RaycastHit hit, out EnemyHealth enemyHealth)
    {
        enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) enemyHealth = hit.collider.GetComponent<EnemyHealth>();
        if (enemyHealth == null) enemyHealth = hit.collider.GetComponentInChildren<EnemyHealth>();
        return enemyHealth != null;
    }

    static bool TryGetEnemyAi(RaycastHit hit, out EnemyAi enemyAi)
    {
        enemyAi = hit.collider.GetComponentInParent<EnemyAi>();
        if (enemyAi == null) enemyAi = hit.collider.GetComponent<EnemyAi>();
        if (enemyAi == null) enemyAi = hit.collider.GetComponentInChildren<EnemyAi>();
        return enemyAi != null;
    }

    void ApplyDamageIndicator(Vector3 hitPoint, float damage)
    {
        DamageIndicatorSpawner.Spawn(hitPoint, damage, damageIndicatorPrefab);
    }

    void PlayFireSound()
    {
        if (gunFireSound == null) return;
        if (cachedFireClip == null) CacheFireSound();
        if (cachedFireClip != null) { gunFireSound.PlayOneShot(cachedFireClip); return; }
        gunFireSound.Play();
    }

    void PlayAnimation(string stateName)
    {
        if (gunAnimator == null || string.IsNullOrEmpty(stateName)) return;
        gunAnimator.Play(stateName, 0, 0f);
    }

    Vector3 GetSpreadDirection(Transform cameraTransform)
        => WeaponSpreadUtility.GetSpreadDirection(cameraTransform, SpreadDegrees);

    /// <summary>
    /// Called by the mobile Reload button. Ignores the request if a reload
    /// is already in progress, the clip is already full, or there is no
    /// reserve ammo — prevents animation spam from repeated taps.
    /// </summary>
    public void TriggerReload()
    {
        if (isReloading) return;
        if (GetCurrentClip() >= maxClipSize) return;
        if (GetCurrentReserve() <= 0) return;
        StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        isReloading = true;
        PlayAnimation(reloadAnimationName);
        yield return new WaitForSeconds(reloadDuration);

        int currentClip = GetCurrentClip();
        int currentReserve = GetCurrentReserve();
        int ammoNeeded = maxClipSize - currentClip;

        if (currentReserve >= ammoNeeded)
        {
            SetClip(currentClip + ammoNeeded);
            SetReserve(currentReserve - ammoNeeded);
        }
        else
        {
            SetClip(currentClip + currentReserve);
            SetReserve(0);
        }

        isReloading = false;
        nextFireTime = Time.time;
    }

    public string GetAmmoDisplayString() =>
        GetCurrentClip() + " / " + GetCurrentReserve();

    public void WriteAmmoToText(TMP_Text text)
    {
        if (text == null) return;
        text.SetText("{0} / {1}", GetCurrentClip(), GetCurrentReserve());
    }

    public int GetCurrentClip() => GlobalAmmo.GetClip(ammoType);
    public int GetCurrentReserve() => GlobalAmmo.GetReserve(ammoType);
    public void ModifyClip(int amt) => SetClip(GetCurrentClip() + amt);
    public void ModifyReserve(int a) => SetReserve(GetCurrentReserve() + a);

    public void SetClip(int value)
        => GlobalAmmo.SetClip(ammoType, Mathf.Clamp(value, 0, maxClipSize));

    public void SetReserve(int value)
        => GlobalAmmo.SetReserve(ammoType, Mathf.Max(0, value));
}
