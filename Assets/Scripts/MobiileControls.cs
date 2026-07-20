using UnityEngine;

public class MobileControls : MonoBehaviour
{
    [Header("References")]
    public CharacterController playerController;
    public Transform playerCamera;
    [Tooltip("Drag in Weapon_Container (the parent of all your weapons)")]
    public GameObject weaponContainer;
    public VirtualJoystick joystick;

    [Header("UI / Game Buttons")]
    [Tooltip("Drag the StatsPanel GameObject (Tab key)")]
    public GameObject statsPanel;
    [Tooltip("Drag the StorePanel GameObject (B key)")]
    public GameObject storePanel;
    [Tooltip("Drag the WaveManager GameObject (P key)")]
    public GameObject waveManager;

    [Header("Button GameObjects (to auto-hide/show)")]
    [Tooltip("Drag the START WAVE button GameObject here")]
    public GameObject waveButton;
    [Tooltip("Drag the STORE button GameObject here")]
    public GameObject storeButton;
    [Tooltip("Drag PromptText (the cyan text CHILD inside WaveTextFolder)")]
    public GameObject promptText;

    [Header("Feel")]
    public float moveSpeed = 4f;
    public float lookSensitivity = 0.15f;

    private float _cameraPitch = 0f;
    private int _lookFingerId = -1;
    private Vector2 _lastLookPos;
    private bool _statsOpen = false;

    void Update()
    {
        // Force cursor visible every frame — overrides FPS controller re-locking
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        HandleMovement();
        HandleLook();
        SyncWaveButtons();
    }

    // ── Auto-sync wave/store buttons with WaveManager's prompt text ──

    void SyncWaveButtons()
    {
        if (promptText == null) return;
        bool show = promptText.activeSelf;
        if (waveButton != null) waveButton.SetActive(show);
        if (storeButton != null) storeButton.SetActive(show);
    }

    // ── Movement ────────────────────────────────────────────────────

    void HandleMovement()
    {
        if (joystick == null || playerController == null) return;
        Vector3 move = transform.right * joystick.Horizontal
                     + transform.forward * joystick.Vertical;
        move.y = -9.8f;
        playerController.Move(move * moveSpeed * Time.deltaTime);
    }

    // ── Look ────────────────────────────────────────────────────────

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
                playerController.transform.Rotate(Vector3.up * delta.x * lookSensitivity);
                _cameraPitch -= delta.y * lookSensitivity;
                _cameraPitch = Mathf.Clamp(_cameraPitch, -80f, 80f);
                if (playerCamera != null)
                    playerCamera.localEulerAngles = new Vector3(_cameraPitch, 0f, 0f);
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                     && touch.fingerId == _lookFingerId)
                _lookFingerId = -1;
        }
    }

    // ── Weapon buttons ───────────────────────────────────────────────

    public void OnFireButtonDown()
    {
        if (weaponContainer != null)
            weaponContainer.BroadcastMessage("Shoot", SendMessageOptions.DontRequireReceiver);
    }

    public void OnFireButtonUp() { }

    public void OnReloadButton()
    {
        if (weaponContainer != null)
            weaponContainer.BroadcastMessage("Reload", SendMessageOptions.DontRequireReceiver);
    }

    // E — pickup weapon (sent to player so its pickup raycast triggers)
    public void OnPickupButton()
    {
        if (playerController != null)
            playerController.gameObject.BroadcastMessage("PickUp",
                SendMessageOptions.DontRequireReceiver);
    }

    // Q — drop current weapon
    public void OnDropButton()
    {
        if (weaponContainer != null)
            weaponContainer.BroadcastMessage("Drop", SendMessageOptions.DontRequireReceiver);
    }

    // ── Stats (Tab) — toggle open/close, refresh on open ────────────

    public void OnStatsButton()
    {
        if (statsPanel == null) return;
        _statsOpen = !_statsOpen;
        statsPanel.SetActive(_statsOpen);
        if (_statsOpen)
            statsPanel.SendMessage("UpdateStatsDisplay",
                SendMessageOptions.DontRequireReceiver);
    }

    // ── Store (B) ────────────────────────────────────────────────────

    public void OnStoreButton()
    {
        if (storePanel != null)
            storePanel.SetActive(!storePanel.activeSelf);
    }

    // ── Start wave (P) ───────────────────────────────────────────────

    public void OnStartWaveButton()
    {
        if (waveManager != null)
            waveManager.SendMessage("StartNextWave", SendMessageOptions.DontRequireReceiver);
    }
}