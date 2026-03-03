using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// NonVRMovement
///
/// A simple first-person controller for in-editor testing.
/// 
/// Supports:
/// • Standard walking + jumping
/// • Gravity toggling
/// • FreeFly (zero-G controlled movement)
/// • Optional inertia drift (space-like float behaviour)
///
/// Designed for clarity and teaching.
/// </summary>

[RequireComponent(typeof(CharacterController))]
public class NonVRMovement : MonoBehaviour
{
    // ============================================================
    // SECTION 1 — MOVEMENT SETTINGS (NORMAL MODE)
    // ============================================================

    [Header("Grounded Movement")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravityStrength = 9.81f;

    // ============================================================
    // SECTION 2 — FREEFLY SETTINGS
    // ============================================================

    [Header("FreeFly Settings")]
    public float freeFlySpeed = 6f;
    public float verticalSpeed = 4f;

    [Tooltip("If enabled, movement continues drifting when no input is pressed.")]
    public bool useInertiaDrift = false;

    [Tooltip("How quickly drift slows down (only used if inertia is enabled).")]
    public float driftDamping = 0.98f;

    // ============================================================
    // SECTION 3 — LOOK SETTINGS
    // ============================================================

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public Transform cameraHolder;

    // ============================================================
    // SECTION 4 — RUNTIME STATE (MODIFIED BY EVENT ZONES)
    // ============================================================

    [Header("Runtime State")]
    public bool useGravity = true;
    public bool freeFlyMode = false;

    // ============================================================
    // INTERNAL VARIABLES
    // ============================================================

    private CharacterController controller;
    private Vector3 velocity;          // Used for gravity & jumping
    private Vector3 freeFlyVelocity;   // Used for inertia drift
    private float verticalLookRotation;
    private bool isGrounded;

    // ============================================================
    // INITIALISATION
    // ============================================================

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ============================================================
    // MAIN UPDATE LOOP
    // ============================================================

    void Update()
    {
        if (freeFlyMode)
        {
            HandleFreeFlyMovement();
        }
        else
        {
            HandleGroundCheck();
            HandleGroundedMovement();
            HandleJump();
            HandleGravity();
        }

        HandleMouseLook();
    }

    // ============================================================
    // GROUND CHECK (ONLY USED IN NORMAL MODE)
    // ============================================================

    void HandleGroundCheck()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // Keeps player grounded
        }
    }

    // ============================================================
    // NORMAL GROUNDED MOVEMENT (WASD ON FLOOR)
    // ============================================================

    void HandleGroundedMovement()
    {
        Vector2 input = GetMovementInput();

        Vector3 move =
            transform.right * input.x +
            transform.forward * input.y;

        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    // ============================================================
    // FREEFLY MOVEMENT (FULL 3D CAMERA-RELATIVE)
    // ============================================================

    void HandleFreeFlyMovement()
    {
        Vector2 input = GetMovementInput();

        // Movement is relative to camera direction
        Vector3 directionalMove =
            cameraHolder.forward * input.y +
            cameraHolder.right * input.x;

        // Vertical thrust (Q = down, E = up)
        float verticalInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.isPressed) verticalInput = 1f;
            if (Keyboard.current.qKey.isPressed) verticalInput = -1f;
        }

        Vector3 verticalMove = Vector3.up * verticalInput;

        Vector3 totalMove = directionalMove + verticalMove;

        if (useInertiaDrift)
        {
            // Apply acceleration instead of direct movement
            freeFlyVelocity += totalMove * freeFlySpeed * Time.deltaTime;

            // Apply damping (simulates space drag)
            freeFlyVelocity *= driftDamping;

            controller.Move(freeFlyVelocity * Time.deltaTime);
        }
        else
        {
            // Immediate, direct movement (no drift)
            controller.Move(totalMove * freeFlySpeed * Time.deltaTime);
        }
    }

    // ============================================================
    // JUMP (NORMAL MODE ONLY)
    // ============================================================

    void HandleJump()
    {
        if (!useGravity) return;

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravityStrength);
        }
    }

    // ============================================================
    // GRAVITY (NORMAL MODE ONLY)
    // ============================================================

    void HandleGravity()
    {
        if (useGravity)
        {
            velocity.y -= gravityStrength * Time.deltaTime;
        }
        else
        {
            velocity.y = 0f;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    // ============================================================
    // MOUSE LOOK (SHARED BY BOTH MODES)
    // ============================================================

    void HandleMouseLook()
    {
        if (Mouse.current == null || cameraHolder == null)
            return;

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue() *
            mouseSensitivity * 0.1f;

        transform.Rotate(Vector3.up * mouseDelta.x);

        verticalLookRotation -= mouseDelta.y;
        verticalLookRotation =
            Mathf.Clamp(verticalLookRotation, -90f, 90f);

        cameraHolder.localRotation =
            Quaternion.Euler(verticalLookRotation, 0f, 0f);
    }

    // ============================================================
    // INPUT HELPER
    // ============================================================

    Vector2 GetMovementInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            input.x =
                (Keyboard.current.dKey.isPressed ? 1 : 0) -
                (Keyboard.current.aKey.isPressed ? 1 : 0);

            input.y =
                (Keyboard.current.wKey.isPressed ? 1 : 0) -
                (Keyboard.current.sKey.isPressed ? 1 : 0);
        }

        return input;
    }
}