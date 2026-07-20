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
    [Tooltip("Drag the StoreManager GameObject (B key)")]
    public GameObject storeObject;
    [Tooltip("Drag the WaveManager GameObject (P key)")]
    public GameObject waveManager;

    [Header("Feel")]
    public float moveSpeed = 4f;
    public float lookSensitivity = 0.15f;

    private float _cameraPitch = 0f;
    private int _lookFingerId = -1;
    private Vector2 _lastLookPos;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
    }

    void HandleMovement()
    {
        if (joystick == null || playerController == null) return;
        Vector3 move = transform.right * joystick.Horizontal
                     + transform.forward * joystick.Vertical;
        move.y = -9.8f;
        playerController.Move(move * moveSpeed * Time.deltaTime);
    }

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
            {
                _lookFingerId = -1;
            }
        }
    }

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

    public void OnStatsButton()
    {
        if (statsPanel != null)
            statsPanel.SetActive(!statsPanel.activeSelf);
    }

    public void OnStoreButton()
    {
        if (storeObject != null)
            storeObject.SendMessage("OpenStore", SendMessageOptions.DontRequireReceiver);
    }

    public void OnStartWaveButton()
    {
        if (waveManager != null)
            waveManager.SendMessage("StartNextWave", SendMessageOptions.DontRequireReceiver);
    }
}