using UnityEngine;
using StarterAssets;

public class MobileControls : MonoBehaviour
{
    [Header("Player References")]
    public CharacterController playerController;
    public Transform playerCamera;
    public GameObject weaponContainer;
    public VirtualJoystick joystick;
    public StarterAssetsInputs starterInputs;

    [Header("UI Panels")]
    public GameObject statsPanel;
    public GameObject storePanel;

    [Header("Wave UI")]
    public GameObject waveManager;
    public GameObject waveButton;
    public GameObject storeButton;
    public GameObject promptText;

    [Header("Context Buttons (auto-show/hide)")]
    public GameObject pickupButton;
    public GameObject dropButton;

    [Header("Gun Buttons — hidden when no weapon")]
    public GameObject[] gunOnlyButtons;

    [Header("Store")]
    public GameObject storeExitButton;

    [Header("Hide when Store opens")]
    public GameObject[] hideWhenStoreOpen;

    [Header("Feel")]
    public float moveSpeed = 4f;
    public float sprintSpeed = 8.5f;

    // Look sensitivity — no /dt division; tune in the Inspector.
    public float lookSensitivity = 0.3f;

    // ── cached refs ──
    private PlayerStamina _playerStamina;
    private FireButton _fireButton;          // cached after injection

    private int _lookFingerId = -1;
    private Vector2 _lastLookPos;
    private bool _storeOpen = false;

    // ── fire button debounce ──────────────────────────────────────────────────
    // Guards OnFireButtonDown() against the Unity Button.OnClick double-fire.
    // OnClick fires AFTER OnPointerUp in the same frame, so Time.time matches.
    private float _fireButtonUpTime = -999f;
    private const float kFireBtnDebounce = 0.15f;
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null)
            _playerStamina = playerController.GetComponent<PlayerStamina>();

        // Disable WeaponFire's mouse/keyboard polling on mobile.
        WeaponFire.MobileMode = true;

        InjectFireButton();

        if (pickupButton != null) pickupButton.SetActive(false);
        if (dropButton != null) dropButton.SetActive(false);
        if (storeExitButton != null) storeExitButton.SetActive(false);
    }

    void InjectFireButton()
    {
        GameObject fireBtn = GameObject.Find("FireButton");
        if (fireBtn == null) return;

        FireButton fb = fireBtn.GetComponent<FireButton>();
        if (fb == null) fb = fireBtn.AddComponent<FireButton>();
        fb.mobileControls = this;
        _fireButton = fb;   // cache so HandleAutoFire can read IsPointerDown
    }

    void OnDestroy()
    {
        WeaponFire.MobileMode = false;
        PlayerStamina.MobileJoystickInput = Vector2.zero;
        PlayerStamina.MobileSprinting = false;
    }

    void Update()
    {
        if (_storeOpen && storePanel != null && !storePanel.activeSelf)
            CloseStore();

        HandleMovement();
        HandleLook();
        HandleAutoFire();
        SyncWaveButtons();
        SyncPickupDropButtons();
        SyncGunButtons();
    }

    // ─────────────────────────── MOVEMENT ────────────────────────────────────

    void HandleMovement()
    {
        if (joystick == null || playerController == null) return;

        Vector2 joyInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        PlayerStamina.MobileJoystickInput = joyInput;

        Transform t = playerController.transform;
        Vector3 move = t.right * joyInput.x + t.forward * joyInput.y;
        move.y = -9.8f;

        float speed = (_playerStamina != null && _playerStamina.IsSprinting)
            ? sprintSpeed : moveSpeed;

        playerController.Move(move * speed * Time.deltaTime);
    }

    // ─────────────────────────── LOOK ────────────────────────────────────────

    void HandleLook()
    {
        foreach (Touch touch in Input.touches)
        {
            if (touch.position.x < Screen.width * 0.5f) continue;

            if (touch.phase == TouchPhase.Began && _lookFingerId == -1)
            {
                _lookFingerId = touch.fingerId;
                _lastLookPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved && touch.fingerId == _lookFingerId)
            {
                Vector2 delta = touch.position - _lastLookPos;
                _lastLookPos = touch.position;

                if (starterInputs != null)
                {
                    starterInputs.LookInput(new Vector2(delta.x, -delta.y) * lookSensitivity);
                }
                else
                {
                    playerController.transform.Rotate(Vector3.up * delta.x * lookSensitivity);
                    if (playerCamera != null)
                    {
                        Vector3 e = playerCamera.localEulerAngles;
                        float p = e.x > 180f ? e.x - 360f : e.x;
                        p = Mathf.Clamp(p - delta.y * lookSensitivity, -80f, 80f);
                        playerCamera.localEulerAngles = new Vector3(p, 0f, 0f);
                    }
                }
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                     && touch.fingerId == _lookFingerId)
            {
                _lookFingerId = -1;
                if (starterInputs != null) starterInputs.LookInput(Vector2.zero);
            }
        }
    }

    // ─────────────────────────── FIRE LOGIC ──────────────────────────────────

    void HandleAutoFire()
    {
        // ── KEY FIX ──────────────────────────────────────────────────────────
        // Read IsPointerDown directly from FireButton instead of using _isFiring.
        //
        // _isFiring was a bool that could get STUCK true because Unity's
        // Button.OnClick fired OnFireButtonDown() again AFTER OnPointerUp already
        // reset it — leaving the Mac10 (and any other auto weapon) firing forever.
        //
        // IsPointerDown is set true only in OnPointerDown and cleared in
        // OnPointerUp/OnPointerExit. It ALWAYS reflects whether a finger is
        // physically touching the button right now — it cannot get stuck.
        //
        // This also fixes "pick up Mac10 and it fires by itself": when the player
        // was NOT touching the fire button at pickup time, IsPointerDown is false,
        // so auto-fire never starts regardless of which weapon is active.
        // ─────────────────────────────────────────────────────────────────────
        bool fingerIsDown = _fireButton != null && _fireButton.IsPointerDown;
        if (!fingerIsDown || weaponContainer == null) return;

        WeaponFire active = GetActiveWeapon();
        if (active == null || !active.IsAutomatic) return;

        // Shoot() enforces fire rate and ammo internally.
        active.Shoot();
    }

    WeaponFire GetActiveWeapon()
    {
        if (weaponContainer == null) return null;
        foreach (WeaponFire w in weaponContainer.GetComponentsInChildren<WeaponFire>(true))
            if (w.gameObject.activeInHierarchy && w.enabled) return w;
        return null;
    }

    // Called by FireButton.cs on PointerDown — fires the initial shot.
    public void OnFireButtonDown()
    {
        // Debounce: reject calls that arrive within kFireBtnDebounce seconds of
        // the last OnFireButtonUp().  Unity's Button.OnClick fires on RELEASE
        // (after OnPointerUp) in the same frame, so Time.time - _fireButtonUpTime
        // will be ~0 — safely blocked here even if onClick wasn't cleared.
        if (Time.time - _fireButtonUpTime < kFireBtnDebounce) return;

        // Fire one shot immediately on press (works for both semi and auto).
        GetActiveWeapon()?.Shoot();
    }

    // Called by FireButton.cs on PointerUp / PointerExit.
    public void OnFireButtonUp()
    {
        _fireButtonUpTime = Time.time;  // timestamp for debounce only
        // Auto-fire is now governed by FireButton.IsPointerDown, so no _isFiring
        // flag to clear here — HandleAutoFire() will stop on the very next frame.
    }

    // ─────────────────────────── UI SYNC ─────────────────────────────────────

    void SyncWaveButtons()
    {
        if (promptText == null) return;
        bool show = promptText.activeSelf && !_storeOpen;
        if (waveButton != null) waveButton.SetActive(show);
        if (storeButton != null) storeButton.SetActive(show);
    }

    void SyncPickupDropButtons()
    {
        if (pickupButton != null)
            pickupButton.SetActive(WeaponPickup.CurrentPickupTarget != null);
        if (dropButton != null)
            dropButton.SetActive(PlayerHasEquippedWeapon());
    }

    void SyncGunButtons()
    {
        bool hasWeapon = PlayerHasEquippedWeapon();
        foreach (GameObject btn in gunOnlyButtons)
            if (btn != null) btn.SetActive(hasWeapon && !_storeOpen);
    }

    bool PlayerHasEquippedWeapon()
    {
        if (weaponContainer == null) return false;
        foreach (WeaponFire w in weaponContainer.GetComponentsInChildren<WeaponFire>(true))
            if (w.gameObject.activeInHierarchy && w.enabled) return true;
        return false;
    }

    // ─────────────────────────── STORE ───────────────────────────────────────

    void OpenStore()
    {
        _storeOpen = true;
        if (storePanel != null) storePanel.SetActive(true);
        if (storeExitButton != null) storeExitButton.SetActive(true);
        if (pickupButton != null) pickupButton.SetActive(false);
        if (dropButton != null) dropButton.SetActive(false);
        foreach (GameObject obj in hideWhenStoreOpen)
            if (obj != null) obj.SetActive(false);
    }

    void CloseStore()
    {
        _storeOpen = false;
        if (storePanel != null) storePanel.SetActive(false);
        if (storeExitButton != null) storeExitButton.SetActive(false);
        foreach (GameObject obj in hideWhenStoreOpen)
            if (obj != null) obj.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ─────────────────────────── BUTTON EVENTS ───────────────────────────────

    public void OnReloadButton()
    {
        if (weaponContainer != null)
            weaponContainer.BroadcastMessage("Reload", SendMessageOptions.DontRequireReceiver);
    }

    public void OnStatsButtonDown()
    {
        PlayerStatsDisplay.MobileHeld = true;
        if (joystick != null) joystick.gameObject.SetActive(false);
    }

    public void OnStatsButtonUp()
    {
        PlayerStatsDisplay.MobileHeld = false;
        if (joystick != null && !_storeOpen) joystick.gameObject.SetActive(true);
    }

    public void OnStoreButton()
    {
        if (_storeOpen) CloseStore();
        else OpenStore();
    }

    public void OnExitStoreButton() => CloseStore();

    public void OnStartWaveButton()
    {
        CloseStore();
        if (waveManager != null)
            waveManager.SendMessage("StartNextWave", SendMessageOptions.DontRequireReceiver);
    }

    public void OnPickupButton()
    {
        WeaponPickup target = WeaponPickup.CurrentPickupTarget;
        if (target != null) target.TryPickup();
    }

    public void OnDropButton()
    {
        if (weaponContainer == null) return;
        foreach (WeaponDrop drop in weaponContainer.GetComponentsInChildren<WeaponDrop>(true))
        {
            if (drop != null && drop.gameObject.activeInHierarchy)
            {
                drop.Drop();
                return;
            }
        }
    }

    public void OnSprintButtonDown() { PlayerStamina.MobileSprinting = true; }
    public void OnSprintButtonUp() { PlayerStamina.MobileSprinting = false; }
}
