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

    private int _lookFingerId = -1;
    private Vector2 _lastLookPos;
    private bool _storeOpen = false;
    private bool _isFiring = false;   // true while fire button is physically held

    void Start()
    {
        // Lock to landscape immediately
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Cache stamina for sprint speed
        if (playerController != null)
            _playerStamina = playerController.GetComponent<PlayerStamina>();

        // Disable WeaponFire's mouse/keyboard polling — mobile fire button
        // calls Shoot() directly via FireButton.cs (IPointerDownHandler).
        WeaponFire.MobileMode = true;

        // Auto-inject FireButton onto the Fire button so OnPointerDown /
        // OnPointerUp events work correctly. This means no Inspector setup
        // is needed as long as your button is named "FireButton".
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

        // Tell PlayerStamina the joystick is active (keyboard axes are 0 on mobile).
        PlayerStamina.MobileJoystickInput = joyInput;

        // Use the player's own transform so direction follows their look angle.
        Transform t = playerController.transform;
        Vector3 move = t.right * joyInput.x + t.forward * joyInput.y;
        move.y = -9.8f;

        float speed = (_playerStamina != null && _playerStamina.IsSprinting)
            ? sprintSpeed
            : moveSpeed;

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

    // Runs every frame while fire button is held.
    // Automatic weapons (Mac10, M9-when-set-to-auto): keep shooting.
    // Semi-auto (AK47, AWP, Shotgun): only the initial press fires; this exits early.
    void HandleAutoFire()
    {
        if (!_isFiring || weaponContainer == null) return;

        WeaponFire active = GetActiveWeapon();
        if (active == null || !active.IsAutomatic) return;

        // Shoot() guards against empty ammo and enforces fire rate internally.
        active.Shoot();
    }

    WeaponFire GetActiveWeapon()
    {
        if (weaponContainer == null) return null;
        foreach (WeaponFire w in weaponContainer.GetComponentsInChildren<WeaponFire>(true))
            if (w.gameObject.activeInHierarchy && w.enabled) return w;
        return null;
    }

    // Called by FireButton.cs on PointerDown.
    public void OnFireButtonDown()
    {
        _isFiring = true;
        // Fire once immediately on press regardless of mode (semi or auto).
        GetActiveWeapon()?.Shoot();
    }

    // Called by FireButton.cs on PointerUp and PointerExit.
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
