using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Configurations")]
    public float maxStamina = 100f;
    public float currentStamina;
    [SerializeField] float drainRate = 25f;
    [SerializeField] float regenRate = 15f;

    [Header("Speed Values")]
    [SerializeField] float baseWalkSpeed = 5f;
    [SerializeField] float baseSprintSpeed = 8.5f;

    [Header("Jumping & Gravity Settings")]
    [SerializeField] float jumpHeight = 2.0f;
    [SerializeField] float gravity = -19.62f;
    private Vector3 velocity;
    private bool isGrounded;

    [Header("UI Elements")]
    [SerializeField] Slider staminaSlider;

    private CharacterController characterController;
    private bool canSprint = true;

    // Set to true by MobileControls.OnSprintButtonDown,
    // false by MobileControls.OnSprintButtonUp.
    public static bool MobileSprinting = false;

    // Footsteps script reads this to know if actually sprinting
    public bool IsSprinting { get; private set; }

    void Start()
    {
        currentStamina = maxStamina;
        characterController = GetComponent<CharacterController>();

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = maxStamina;
        }
    }

    void Update()
    {
        // 1. GRAVITY & GROUND CHECK
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // 2. READ INPUTS — Shift key (keyboard) OR mobile Sprint button
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        bool isMoving = (moveX != 0 || moveZ != 0);

        bool isTryingToSprint = (Input.GetKey(KeyCode.LeftShift) || MobileSprinting)
                                && isMoving && canSprint && currentStamina > 0;

        IsSprinting = isTryingToSprint;

        // 3. STAMINA MATH & RESTRICTIONS
        if (isTryingToSprint)
        {
            currentStamina -= drainRate * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                canSprint = false;
            }
        }
        else
        {
            currentStamina += regenRate * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;

            if (!canSprint && currentStamina >= maxStamina * 0.2f)
                canSprint = true;
        }

        if (staminaSlider != null) staminaSlider.value = currentStamina;

        // 4. APPLY SHOP UPGRADES & MOVE PLAYER
        float activeSpeed = isTryingToSprint ? baseSprintSpeed : baseWalkSpeed;
        float upgradedSpeed = activeSpeed * GlobalStats.permanentSpeedUpgrade;

        Vector3 moveDirection = transform.right * moveX + transform.forward * moveZ;
        characterController.Move(moveDirection * upgradedSpeed * Time.deltaTime);

        // 5. JUMP LOGIC
        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}
