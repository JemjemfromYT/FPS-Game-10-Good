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
    // FALLBACK: _isFiring bool set by OnFireButtonDown / cleared by OnFireButtonUp
    //           (used during the brief startup window before the rect is found,
    //           or if FindFireButton can't locate the button at all).

    private RectTransform _fireBtnRect;
    private Canvas _fireBtnCanvas;

    private bool _fireTouchActive; // a finger is currently on the fire button rect
    private bool _fireTouchBegan;  // the touch started THIS frame (initial press)

    // Fallback flag — set by OnFireButtonDown, cleared by OnFireButtonUp.
    // Also force-cleared every frame once _fireBtnRect is found so it can
    // never get permanently stuck after the rect becomes available.
    private bool _isFiring;

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

        // Add FireButton.cs which handles IPointerUpHandler so _isFiring is
        // always cleared when the finger lifts (important for the fallback path).
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
        // Fall back to Camera.main if worldCamera is not assigned.
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
            if (t.phase == TouchPhase.Canceled) continue;

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

        // Once the rect is found we have reliable per-frame touch data, so
        // forcibly clear _isFiring — it can never get stuck after this point.
        if (_fireBtnRect != null)
            _isFiring = false;

        // Combine both detection paths:
        //   _fireTouchActive — direct touch-rect check (primary, most reliable)
        //   _isFiring        — fallback flag from OnFireButtonDown / OnFireButtonUp
        bool holdFiring = _fireTouchActive || _isFiring;
        bool justPressed = _fireTouchBegan;

        WeaponFire active = GetActiveWeapon();
        if (active == null) return;

        if (active.IsAutomatic)
        {
            // Call Shoot() every frame while held.
            // WeaponFire.Shoot() enforces fireRate internally — safe to call every frame.
            if (holdFiring) active.Shoot();
        }
        else
        {
            // Semi-auto: one shot per press.
            // justPressed is true only on the FIRST frame of a new touch.
            // OnFireButtonDown() also calls Shoot() once for the onClick fallback path.
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

    // Called by the Inspector On Click () entry on the FireButton.
    // Acts as a FALLBACK for the startup window before _fireBtnRect is found.
    // Once FindFireButton() succeeds it clears onClick, so this is no longer called.
    public void OnFireButtonDown()
    {
        _isFiring = true;
        // Fire one shot immediately on press (covers both semi-auto and auto).
        GetActiveWeapon()?.Shoot();
    }

    // Called by FireButton.cs IPointerUpHandler — clears the fallback flag.
    public void OnFireButtonUp()
    {
        _isFiring = false;
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
