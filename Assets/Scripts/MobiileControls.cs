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
    // _isFiring    — true while finger is DOWN on the fire button.
    //                Set by OnFireButtonDown(), cleared by OnFireButtonUp().
    //                NEVER force-cleared by polling logic — only pointer events.
    //
    // _fireJustPressed — true for exactly one HandleAutoFire() frame after the
    //                    finger first touches the button.  Used for semi-auto
    //                    weapons so one tap = one bullet.
    private bool _isFiring;
    private bool _fireJustPressed;

    // ── weapon-change tracking ───────────────────────────────────────────────
    // When the player picks up a new weapon we reset the fire flags so the
    // new gun doesn't inherit a "stuck firing" state from the previous one.
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

    // ── find & wire the FireButton UI element ─────────────────────────────────
    // Tries every frame until successful so it works even if the Canvas starts
    // inactive. Once found, FireButton.cs handles pointer events — no rect-polling.
    private bool _fireButtonWired = false;

    void FindFireButton()
    {
        if (_fireButtonWired) return;

        GameObject obj = GameObject.Find("FireButton");
        if (obj == null) return;

        // Clear the Inspector On Click () so it never fires on pointer-up
        // and leaves _isFiring stuck true.
        var btn = obj.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
            btn.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        // FireButton.cs provides IPointerDownHandler + IPointerUpHandler.
        FireButton fb = obj.GetComponent<FireButton>() ?? obj.AddComponent<FireButton>();
        fb.mobileControls = this;

        _fireButtonWired = true;
    }

    void OnDestroy()
    {
        WeaponFire.MobileMode = false;
        PlayerStamina.MobileJoystickInput = Vector2.zero;
        PlayerStamina.MobileSprinting = false;
    }

    void Update()
    {
        if (!_fireButtonWired) FindFireButton(); // retry until canvas is active

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

    void HandleAutoFire()
    {
        WeaponFire active = GetActiveWeapon();

        // ── Weapon-change guard ───────────────────────────────────────────────
        // If the player just picked up a new weapon, reset fire flags so the
        // new gun doesn't inherit a stuck-firing state from the old one.
        if (active != _lastActiveWeapon)
        {
            _isFiring = false;
            _fireJustPressed = false;
            _lastActiveWeapon = active;
        }

        if (active == null) return;

        if (active.IsAutomatic)
        {
            // ── Automatic ────────────────────────────────────────────────────
            // Fires every frame while the finger is held on the button.
            // WeaponFire.Shoot() enforces fireRate internally — safe to call every frame.
            if (_isFiring)
                active.Shoot();
        }
        else
        {
            // ── Semi-auto ────────────────────────────────────────────────────
            // One shot per tap — _fireJustPressed is only true for a single
            // HandleAutoFire() call after the finger first touched the button.
            if (_fireJustPressed)
                active.Shoot();
        }

        // Always consume _fireJustPressed after one frame so it can never
        // linger and cause multiple shots from a single tap.
        _fireJustPressed = false;
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

    // Called by FireButton.cs IPointerDownHandler — finger touched the button.
    public void OnFireButtonDown()
    {
        _isFiring = true;
        _fireJustPressed = true;
        // Note: do NOT call Shoot() here.
        // HandleAutoFire() reads _isFiring / _fireJustPressed each frame and
        // decides whether to fire based on the active weapon type.
        // Calling Shoot() here AND in HandleAutoFire() would double-fire.
    }

    // Called by FireButton.cs IPointerUpHandler — finger lifted off the button.
    public void OnFireButtonUp()
    {
        _isFiring = false;
        // _fireJustPressed is already false by the time OnFireButtonUp fires
        // (it's consumed inside HandleAutoFire the same frame it's set), but
        // clear it here too for safety.
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
