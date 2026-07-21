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
    public float lookSensitivity = 0.3f;

    // ── cached refs ──────────────────────────────────────────────────────────
    private PlayerStamina _playerStamina;

    // ── fire button ──────────────────────────────────────────────────────────
    // PRIMARY: direct touch-rect detection (reliable, no stuck-flag issues).
    //   _fireTouchActive — a finger is currently on the fire button rect
    //   _fireTouchBegan  — the touch started THIS frame (initial press)
    //
    // FALLBACK: used when _fireBtnRect is null (button not yet found).
    //   _isFiring        — set by OnFireButtonDown, cleared by OnFireButtonUp
    //   _fireJustPressed — true for ONE frame after OnFireButtonDown fires;
    //                      lets semi-auto guns fire exactly one shot per tap
    //                      on the fallback path

    private RectTransform _fireBtnRect;
    private Canvas _fireBtnCanvas;

    private bool _fireTouchActive;
    private bool _fireTouchBegan;

    private bool _isFiring;
    private bool _fireJustPressed; // FIX: one-frame flag for semi-auto fallback

    // FIX: track which weapon is active so we can reset fire state on pickup
    private WeaponFire _lastActiveWeapon;

    // ── look ─────────────────────────────────────────────────────────────────
    private int _lookFingerId = -1;
    private Vector2 _lastLookPos;

    // ── misc ─────────────────────────────────────────────────────────────────
    private bool _storeOpen = false;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null)
            _playerStamina = playerController.GetComponent<PlayerStamina>();

        WeaponFire.MobileMode = true;

        FindFireButton(); // first attempt; Update() retries if canvas was inactive

        if (pickupButton != null) pickupButton.SetActive(false);
        if (dropButton != null) dropButton.SetActive(false);
        if (storeExitButton != null) storeExitButton.SetActive(false);
    }

    void FindFireButton()
    {
        if (_fireBtnRect != null) return; // already found

        // GameObject.Find only sees active objects. If the canvas is inactive
        // at Start() time this returns null — Update() retries every frame.
        GameObject obj = GameObject.Find("FireButton");
        if (obj == null) return;

        _fireBtnRect = obj.GetComponent<RectTransform>();
        _fireBtnCanvas = obj.GetComponentInParent<Canvas>();

        // Kill the Inspector On Click () so it can't call OnFireButtonDown()
        // on RELEASE and leave _isFiring stuck true.
        var btn = obj.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
            btn.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        // FireButton.cs provides IPointerDownHandler + IPointerUpHandler so
        // _isFiring / _fireJustPressed are set/cleared on the exact touch frame.
        FireButton fb = obj.GetComponent<FireButton>() ?? obj.AddComponent<FireButton>();
        fb.mobileControls = this;
    }

    void OnDestroy()
    {
        WeaponFire.MobileMode = false;
        PlayerStamina.MobileJoystickInput = Vector2.zero;
        PlayerStamina.MobileSprinting = false;
    }

    void Update()
    {
        if (_fireBtnRect == null) FindFireButton(); // retry until canvas is active

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

    // ─────────────────────────── FIRE ────────────────────────────────────────

    // Polls Input.touches and checks whether any active touch falls inside the
    // FireButton's screen-space RectTransform. Updates _fireTouchActive and
    // _fireTouchBegan for this frame.
    void PollFireTouch()
    {
        _fireTouchActive = false;
        _fireTouchBegan = false;

        if (_fireBtnRect == null) return;

        // ScreenSpaceOverlay canvases need camera = null.
        // ScreenSpaceCamera / WorldSpace canvases need their assigned camera.
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
            // FIX: skip Canceled AND Ended phases — a touch in Ended phase still
            // has its last position, so without this check a tap would leave
            // _fireTouchActive = true for one extra frame, firing an unwanted
            // extra shot on automatic weapons when the player releases.
            if (t.phase == TouchPhase.Canceled || t.phase == TouchPhase.Ended) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                    _fireBtnRect, t.position, cam))
            {
                _fireTouchActive = true;
                if (t.phase == TouchPhase.Began)
                    _fireTouchBegan = true;
                break; // one finger on the button is enough
            }
        }
    }

    void HandleAutoFire()
    {
        PollFireTouch();

        // FIX: weapon-change guard — reset all fire flags when the player
        // picks up a new weapon so the new gun doesn't inherit a stuck-firing
        // state from a previous fire-button press on another weapon.
        WeaponFire active = GetActiveWeapon();
        if (active != _lastActiveWeapon)
        {
            _isFiring = false;
            _fireJustPressed = false;
            _fireTouchActive = false;
            _fireTouchBegan = false;
            _lastActiveWeapon = active;
        }

        // Once the rect is found we have reliable per-frame touch data.
        // Clear _isFiring so the fallback path never conflicts with the
        // primary touch-rect path.
        if (_fireBtnRect != null)
            _isFiring = false;

        // Combine both detection paths:
        //   Primary  — _fireTouchActive / _fireTouchBegan (touch-rect check)
        //   Fallback — _isFiring / _fireJustPressed (pointer events when rect unavailable)
        bool holdFiring = _fireTouchActive || _isFiring;
        bool justPressed = _fireTouchBegan || _fireJustPressed;

        // Consume _fireJustPressed — it must only be true for one HandleAutoFire() call.
        _fireJustPressed = false;

        if (active == null) return;

        if (active.IsAutomatic)
        {
            // Fires every frame the button is held.
            // WeaponFire.Shoot() enforces fireRate internally — safe to call every frame.
            if (holdFiring) active.Shoot();
        }
        else
        {
            // Semi-auto: one shot per press, regardless of hold duration.
            if (justPressed) active.Shoot();
        }
    }

    WeaponFire GetActiveWeapon()
    {
        if (weaponContainer == null) return null;
        foreach (WeaponFire w in
                 weaponContainer.GetComponentsInChildren<WeaponFire>(true))
        {
            if (w.gameObject.activeInHierarchy && w.enabled) return w;
        }
        return null;
    }

    // Called by FireButton.cs IPointerDownHandler — finger just touched the button.
    // FIX: now fires on actual PRESS (pointer-down) instead of on RELEASE (onClick),
    // which was the root cause of the original stuck-firing bug.
    public void OnFireButtonDown()
    {
        _isFiring = true;
        _fireJustPressed = true;

        // FIX: only fire the direct shot when PollFireTouch can't help (rect not
        // found yet). Once the rect is found, HandleAutoFire() handles firing via
        // _fireTouchActive / _fireTouchBegan. Calling Shoot() here AND letting
        // HandleAutoFire() fire from _fireTouchBegan would double-fire.
        if (_fireBtnRect == null)
            GetActiveWeapon()?.Shoot();
    }

    // Called by FireButton.cs IPointerUpHandler — finger lifted off the button.
    public void OnFireButtonUp()
    {
        _isFiring = false;
        _fireJustPressed = false;
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
        foreach (WeaponFire w in
                 weaponContainer.GetComponentsInChildren<WeaponFire>(true))
        {
            if (w.gameObject.activeInHierarchy && w.enabled) return true;
        }
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
            weaponContainer.BroadcastMessage("Reload",
                                             SendMessageOptions.DontRequireReceiver);
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
            waveManager.SendMessage("StartNextWave",
                                    SendMessageOptions.DontRequireReceiver);
    }

    public void OnPickupButton()
    {
        WeaponPickup target = WeaponPickup.CurrentPickupTarget;
        if (target != null) target.TryPickup();
    }

    public void OnDropButton()
    {
        if (weaponContainer == null) return;
        foreach (WeaponDrop drop in
                 weaponContainer.GetComponentsInChildren<WeaponDrop>(true))
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
