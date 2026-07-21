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

    // ── fire button — found by name, retried every frame until found ─────────
    // We do NOT use FireButton.cs pointer events. Instead we poll Input.touches
    // each frame and check whether any touch falls inside the button's rect.
    // This is 100% reliable: no EventSystem, no component injection, no flags
    // that can get stuck.
    private RectTransform _fireBtnRect;
    private Canvas _fireBtnCanvas;

    // Set true for the exact frame a new touch BEGINS on the fire button.
    // Used to fire one initial shot (correct for both semi-auto and full-auto).
    private bool _fireTouchBegan;

    // True every frame at least one finger is touching the fire button area.
    // Used by HandleAutoFire() to keep firing automatic weapons.
    private bool _fireTouchActive;

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

        // First attempt — may fail if canvas is inactive at Start time.
        // Update() retries every frame until the button is found.
        FindFireButton();

        if (pickupButton != null) pickupButton.SetActive(false);
        if (dropButton != null) dropButton.SetActive(false);
        if (storeExitButton != null) storeExitButton.SetActive(false);
    }

    // Finds the "FireButton" GameObject, caches its RectTransform and parent
    // Canvas, then kills the Inspector onClick so it can't call
    // OnFireButtonDown() on release. Safe to call every frame — bails early
    // once the rect is cached.
    void FindFireButton()
    {
        if (_fireBtnRect != null) return;   // already found

        GameObject obj = GameObject.Find("FireButton");
        if (obj == null) return;

        _fireBtnRect = obj.GetComponent<RectTransform>();
        _fireBtnCanvas = obj.GetComponentInParent<Canvas>();

        // Silence the Inspector On Click () entry by replacing the entire
        // event object with a fresh empty one. This runs once — any subsequent
        // Inspector-wired calls to OnFireButtonDown() via onClick are gone.
        var btn = obj.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
            btn.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        // Also ensure the FireButton.cs stub is present (it does the same
        // onClick clear in Awake as an extra safety net).
        if (obj.GetComponent<FireButton>() == null)
            obj.AddComponent<FireButton>();
    }

    void OnDestroy()
    {
        WeaponFire.MobileMode = false;
        PlayerStamina.MobileJoystickInput = Vector2.zero;
        PlayerStamina.MobileSprinting = false;
    }

    void Update()
    {
        // Retry finding the fire button until found (handles inactive canvas at Start).
        if (_fireBtnRect == null) FindFireButton();

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

    // Polls every active touch and checks whether it sits inside the FireButton's
    // screen-space rect. Updates _fireTouchActive and _fireTouchBegan.
    //
    // Why direct touch polling instead of EventSystem pointer events?
    //   • EventSystem pointer events require the FireButton.cs component to be
    //     successfully injected at runtime — which fails silently when the Canvas
    //     is inactive at Start time.
    //   • IPointerExitHandler fires spuriously on mobile when the thumb moves
    //     even slightly, resetting the fire state mid-hold.
    //   • Input.touches + RectTransformUtility never has either problem.
    void PollFireTouch()
    {
        _fireTouchActive = false;
        _fireTouchBegan = false;

        if (_fireBtnRect == null) return;

        // For ScreenSpaceOverlay canvases (the standard mobile setup) the camera
        // argument must be null. For ScreenSpaceCamera / WorldSpace, use the
        // canvas's assigned world camera.
        Camera cam = (_fireBtnCanvas != null
                      && _fireBtnCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _fireBtnCanvas.worldCamera
            : null;

        foreach (Touch t in Input.touches)
        {
            if (t.phase == TouchPhase.Canceled) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                    _fireBtnRect, t.position, cam))
            {
                _fireTouchActive = true;
                if (t.phase == TouchPhase.Began)
                    _fireTouchBegan = true;
                break;  // one finger on the button is enough
            }
        }
    }

    void HandleAutoFire()
    {
        PollFireTouch();

        if (!_fireTouchActive) return;      // no finger on the button

        WeaponFire active = GetActiveWeapon();
        if (active == null) return;

        if (active.IsAutomatic)
        {
            // Call Shoot() every frame. WeaponFire.Shoot() enforces fireRate
            // internally via nextFireTime, so this is safe to call every frame.
            active.Shoot();
        }
        else
        {
            // Semi-auto: only fire on the very first frame the touch begins.
            // Holding the button does nothing extra — one press = one shot.
            if (_fireTouchBegan)
                active.Shoot();
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

    // These remain public because the Inspector On Click () entry still
    // references them. The onClick event is replaced with an empty one at
    // runtime (in FindFireButton), so these are never actually invoked.
    // They are kept here so Unity doesn't log a missing-method warning.
    public void OnFireButtonDown() { }
    public void OnFireButtonUp() { }

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
