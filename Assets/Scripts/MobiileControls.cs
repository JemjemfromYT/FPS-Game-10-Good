// ============================================================
// AI INSTRUCTIONS — READ BEFORE EDITING THIS FILE
// ============================================================
// 1. COMMENTS: Add a comment to every section, method, and
//    non-obvious line. NEVER remove or shorten existing
//    comments — only add or update them.
// 2. DELIVER AS FILE: Always save changes to a .txt file and
//    share that file. Never paste the full code in the chat.
// ============================================================

using UnityEngine;
using StarterAssets;

/// <summary>
/// Drives all mobile UI input for the FPS game.
///
/// Responsibilities:
///   - Joystick movement and sprint
///   - Right-half swipe camera look
///   - Fire button (hold = auto, tap = semi-auto)
///   - Reload, pickup, drop, store, wave buttons
///   - Show/hide UI buttons depending on game state
///
/// Setup required in the Inspector:
///   - Assign all public fields in each [Header] section.
///   - Drag the FireButton GameObject into "Fire Button Object" — the fire
///     system will not work without this assignment.
/// </summary>
public class MobileControls : MonoBehaviour
{
    // ─── Inspector fields ────────────────────────────────────────────────────

    [Header("Player")]
    public CharacterController playerController;
    public Transform playerCamera;
    public VirtualJoystick joystick;
    public StarterAssetsInputs starterInputs;

    [Header("Weapons")]
    [Tooltip("Parent GameObject that holds all WeaponFire children.")]
    public GameObject weaponContainer;

    [Header("Fire Button")]
    [Tooltip("Drag the FireButton GameObject here. Must be assigned for firing to work.")]
    public GameObject fireButtonObject;

    [Header("UI Panels")]
    public GameObject statsPanel;
    public GameObject storePanel;

    [Header("Wave UI")]
    public GameObject waveManager;
    public GameObject waveButton;
    public GameObject storeButton;
    public GameObject promptText;

    [Header("Context Buttons — shown/hidden automatically")]
    public GameObject pickupButton;
    public GameObject dropButton;

    [Header("Gun-only Buttons — hidden when no weapon is equipped")]
    public GameObject[] gunOnlyButtons;

    [Header("Store")]
    public GameObject storeExitButton;
    public GameObject[] hideWhenStoreOpen;

    [Header("Movement Feel")]
    public float moveSpeed = 4f;
    public float sprintSpeed = 8.5f;
    public float lookSensitivity = 0.3f;

    // ─── Private state ───────────────────────────────────────────────────────

    // Cached component references
    private PlayerStamina _playerStamina;

    // Fire button — RectTransform and Canvas are cached once in InitFireButton().
    // They are used by PollFireTouch() to check whether a physical touch lands
    // on the button rect each frame (reliable on device).
    private RectTransform _fireBtnRect;
    private Canvas _fireBtnCanvas;

    // Touch-rect fire state (primary path — physical touch tracking).
    // Updated every frame by PollFireTouch().
    private bool _fireTouchActive; // a finger is currently held on the button
    private bool _fireTouchBegan;  // a finger just touched the button this frame

    // Pointer-event fire state (secondary path — Unity EventSystem).
    // Set by OnFireButtonDown / OnFireButtonUp, which are called by FireButton.cs.
    // Covers the Unity Editor (no Input.touches) and acts as a safety net on device.
    private bool _isFiring;        // true while the finger is held down
    private bool _fireJustPressed; // true for exactly one frame on initial press

    // Tracks the last active weapon so fire state resets on weapon pickup.
    private WeaponFire _lastActiveWeapon;

    // Camera look tracking — stores which finger is doing the look gesture.
    private int _lookFingerId = -1;
    private Vector2 _lastLookPos;

    // Store open/close state.
    private bool _storeOpen;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null)
            _playerStamina = playerController.GetComponent<PlayerStamina>();

        // Tell WeaponFire to skip keyboard/mouse polling — on mobile, any
        // screen touch maps to mouse button 0 and would fire unintentionally.
        WeaponFire.MobileMode = true;

        InitFireButton();

        if (pickupButton != null) pickupButton.SetActive(false);
        if (dropButton != null) dropButton.SetActive(false);
        if (storeExitButton != null) storeExitButton.SetActive(false);
    }

    void OnDestroy()
    {
        // Reset static mobile flags so they don't bleed into other scenes.
        WeaponFire.MobileMode = false;
        PlayerStamina.MobileJoystickInput = Vector2.zero;
        PlayerStamina.MobileSprinting = false;
    }

    void Update()
    {
        // If the fire button wasn't ready at Start() (e.g. its Canvas was
        // inactive), keep retrying each frame until it becomes active.
        if (_fireBtnRect == null) InitFireButton();

        // Auto-close store if the store panel was closed from outside this script.
        if (_storeOpen && storePanel != null && !storePanel.activeSelf)
            CloseStore();

        HandleMovement();
        HandleLook();
        HandleFire();
        SyncWaveButtons();
        SyncPickupDropButtons();
        SyncGunButtons();
    }

    // ─── Fire button setup ───────────────────────────────────────────────────

    /// <summary>
    /// Caches the fire button's RectTransform and Canvas, clears its onClick
    /// list, and attaches FireButton.cs so pointer-down/up events are forwarded
    /// to this script.
    ///
    /// Uses the Inspector-assigned <see cref="fireButtonObject"/> field directly.
    /// No name search — assign the field in the Inspector to avoid fragility.
    /// </summary>
    void InitFireButton()
    {
        if (_fireBtnRect != null) return; // already initialised

        GameObject obj = fireButtonObject;
        if (obj == null || !obj.activeInHierarchy) return;

        _fireBtnRect = obj.GetComponent<RectTransform>();
        _fireBtnCanvas = obj.GetComponentInParent<Canvas>();

        // Clear the Inspector "On Click ()" list.
        // Unity's Button.onClick fires on pointer-UP, not pointer-DOWN.
        // If left wired to OnFireButtonDown it would set _isFiring on release
        // and never clear it, causing the weapon to fire non-stop.
        var btn = obj.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
            btn.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        // Add FireButton.cs (if not already present). It implements
        // IPointerDownHandler and IPointerUpHandler and forwards the events
        // to OnFireButtonDown / OnFireButtonUp on this script.
        FireButton fb = obj.GetComponent<FireButton>() ?? obj.AddComponent<FireButton>();
        fb.mobileControls = this;
    }

    // ─── Movement ────────────────────────────────────────────────────────────

    void HandleMovement()
    {
        if (joystick == null || playerController == null) return;

        Vector2 joyInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        PlayerStamina.MobileJoystickInput = joyInput;

        Transform t = playerController.transform;
        Vector3 move = t.right * joyInput.x + t.forward * joyInput.y;
        move.y = -9.8f; // constant downward force; no jump on mobile

        float speed = (_playerStamina != null && _playerStamina.IsSprinting)
            ? sprintSpeed : moveSpeed;

        playerController.Move(move * speed * Time.deltaTime);
    }

    // ─── Camera look ─────────────────────────────────────────────────────────

    void HandleLook()
    {
        // Only the right half of the screen drives the camera.
        // We track a single finger ID so other fingers (joystick, buttons)
        // don't interfere with the look gesture.
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

    // ─── Fire ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans Input.touches each frame and sets _fireTouchActive / _fireTouchBegan
    /// if any touch lands inside the fire button's screen-space rect.
    ///
    /// Ended and Canceled phases are intentionally skipped: a touch in Ended
    /// still has its last position inside the button, so including it would
    /// leave _fireTouchActive true for one extra frame and fire an unwanted
    /// extra shot when the player releases an automatic weapon.
    /// </summary>
    void PollFireTouch()
    {
        _fireTouchActive = false;
        _fireTouchBegan = false;

        if (_fireBtnRect == null) return;

        // RectangleContainsScreenPoint needs the canvas camera for
        // ScreenSpaceCamera / WorldSpace modes; null is correct for Overlay.
        Camera cam = null;
        if (_fireBtnCanvas != null &&
            _fireBtnCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = _fireBtnCanvas.worldCamera != null
                ? _fireBtnCanvas.worldCamera
                : Camera.main;
        }

        foreach (Touch t in Input.touches)
        {
            if (t.phase == TouchPhase.Canceled || t.phase == TouchPhase.Ended) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(_fireBtnRect, t.position, cam))
            {
                _fireTouchActive = true;
                if (t.phase == TouchPhase.Began) _fireTouchBegan = true;
                break; // one finger on the button is enough
            }
        }
    }

    /// <summary>
    /// Decides whether to fire each frame by combining two input paths:
    ///
    ///   Touch-rect path  — PollFireTouch() reads Input.touches directly.
    ///                      Reliable on a physical device.
    ///
    ///   Pointer-event path — OnFireButtonDown / OnFireButtonUp set _isFiring
    ///                        and _fireJustPressed via Unity's EventSystem.
    ///                        Works in the Unity Editor and as a backup on device.
    ///
    /// Both paths feed into the same holdFiring / justPressed booleans so either
    /// one alone is sufficient to trigger a shot.
    /// </summary>
    void HandleFire()
    {
        PollFireTouch();

        // Reset fire state whenever the player switches weapons, so the new
        // gun never inherits a pressed state from the previous weapon.
        WeaponFire active = GetActiveWeapon();
        if (active != _lastActiveWeapon)
        {
            _isFiring = false;
            _fireJustPressed = false;
            _fireTouchActive = false;
            _fireTouchBegan = false;
            _lastActiveWeapon = active;
        }

        bool holdFiring = _fireTouchActive || _isFiring;
        bool justPressed = _fireTouchBegan || _fireJustPressed;

        // _fireJustPressed must be consumed here so it is only true for a
        // single HandleFire() call, giving semi-auto exactly one shot per tap.
        _fireJustPressed = false;

        if (active == null) return;

        if (active.IsAutomatic)
        {
            // WeaponFire.Shoot() enforces the fire rate internally, so calling
            // it every frame while held is safe — it self-throttles.
            if (holdFiring) active.Shoot();
        }
        else
        {
            // Semi-auto: fire once on the initial press, ignore held state.
            if (justPressed) active.Shoot();
        }
    }

    // Called by FireButton.cs (IPointerDownHandler) — finger just touched the button.
    public void OnFireButtonDown()
    {
        _isFiring = true;
        _fireJustPressed = true;
    }

    // Called by FireButton.cs (IPointerUpHandler) — finger lifted off the button.
    public void OnFireButtonUp()
    {
        _isFiring = false;
        _fireJustPressed = false;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Returns the first enabled, active WeaponFire under weaponContainer.</summary>
    WeaponFire GetActiveWeapon()
    {
        if (weaponContainer == null) return null;
        foreach (WeaponFire w in weaponContainer.GetComponentsInChildren<WeaponFire>(true))
        {
            if (w.gameObject.activeInHierarchy && w.enabled) return w;
        }
        return null;
    }

    bool PlayerHasEquippedWeapon() => GetActiveWeapon() != null;

    // ─── UI sync ─────────────────────────────────────────────────────────────

    void SyncWaveButtons()
    {
        if (promptText == null) return;
        bool show = promptText.activeSelf && !_storeOpen;
        if (waveButton != null) waveButton.SetActive(show);
        if (storeButton != null) storeButton.SetActive(show);
    }

    void SyncPickupDropButtons()
    {
        if (pickupButton != null) pickupButton.SetActive(WeaponPickup.CurrentPickupTarget != null);
        if (dropButton != null) dropButton.SetActive(PlayerHasEquippedWeapon());
    }

    void SyncGunButtons()
    {
        bool show = PlayerHasEquippedWeapon() && !_storeOpen;
        foreach (GameObject btn in gunOnlyButtons)
            if (btn != null) btn.SetActive(show);
    }

    // ─── Store ───────────────────────────────────────────────────────────────

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

    // ─── Button event handlers (wired in Inspector) ──────────────────────────

    public void OnReloadButton()
    {
        // TriggerReload() guards against reloading while already reloading,
        // a full clip, or empty reserve — prevents animation stacking on spam.
        GetActiveWeapon()?.TriggerReload();
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
        WeaponPickup.CurrentPickupTarget?.TryPickup();
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

    public void OnSprintButtonDown() => PlayerStamina.MobileSprinting = true;
    public void OnSprintButtonUp() => PlayerStamina.MobileSprinting = false;
}
