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

    private float nextFireTime = 0f;
    private bool isReloading = false;

    Animator gunAnimator;
    AudioClip cachedFireClip;
    CrosshairSpreadDisplay _cachedSpreadDisplay;

    // FIX 2 (screen-tap fires weapon): MobileControls.Start() sets this true.
    // When true, WeaponFire skips all Input.GetButton polling — on mobile any
    // screen touch maps to mouse button 0 and was triggering shots unintentionally.
    // The mobile Fire UI button calls Shoot() directly instead.
    public static bool MobileMode = false;

    public AmmoType AmmoType => ammoType;
    public int MaxClipSize => maxClipSize;
    public bool IsReloading => isReloading;
    public bool CanShoot => Time.time >= nextFireTime && !isReloading;
    public bool IsWeaponBusy => isReloading || Time.time < nextFireTime;
    public float WeaponSpread => weaponSpread;
    public float SpreadDegrees => WeaponSpreadUtility.OffsetToDegrees(weaponSpread);

    // Lets MobileControls know whether to hold-fire or single-fire.
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

    public void ApplyConfig(WeaponDefinitions.WeaponConfig config)
    {
        if (GlobalAmmo.GetClip(ammoType) == 0 && GlobalAmmo.GetReserve(ammoType) == 0)
        {
            SetClip(config.startClip);
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

        // FIX 2: Skip all mouse/keyboard input when in mobile mode.
        // The mobile Fire button calls Shoot() directly via its UI OnClick event.
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

    // Public so the mobile Fire button's OnClick event can call it directly.
    public void Shoot()
    {
        if (isReloading || Time.time < nextFireTime) return;

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

        // FIX 3 (tracer lag): pass the barrel Transform so BulletTracer
        // can update the start point every frame while the player moves.
        Transform originTransform = bulletTracerOrigin;
        Vector3 tracerStart = originTransform != null
            ? originTransform.position
            : cameraTransform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Vector3 direction = GetSpreadDirection(cameraTransform);
            bool hasHit = Physics.Raycast(cameraTransform.position, direction,
                                          out RaycastHit hit, Mathf.Infinity);
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
    {
        return WeaponSpreadUtility.GetSpreadDirection(cameraTransform, SpreadDegrees);
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

    public void ModifyClip(int amount) => SetClip(GetCurrentClip() + amount);
    public void ModifyReserve(int amount) => SetReserve(GetCurrentReserve() + amount);

    public void SetClip(int value) => GlobalAmmo.SetClip(ammoType, Mathf.Clamp(value, 0, maxClipSize));
    public void SetReserve(int value) => GlobalAmmo.SetReserve(ammoType, Mathf.Max(0, value));
}
