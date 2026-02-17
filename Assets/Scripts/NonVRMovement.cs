using UnityEngine;
using UnityEngine.InputSystem; // NEW input system

/// <summary>
/// Simple non-VR movement controller for in-editor testing.
/// Keyboard: WASD
/// Mouse: Look
/// Space: Jump
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class NonVRMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = 9.81f;

    [Header("Look")]
    public float lookSpeed = 2f;
    public Transform cameraHolder;

    private CharacterController controller;
    private Vector3 velocity;
    private float verticalLookRotation;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Lock cursor for FPS-style look
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // =============================
        // Ground check
        // =============================
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        // =============================
        // Movement (WASD)
        // =============================
        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            moveInput.x =
                (Keyboard.current.dKey.isPressed ? 1 : 0) -
                (Keyboard.current.aKey.isPressed ? 1 : 0);

            moveInput.y =
                (Keyboard.current.wKey.isPressed ? 1 : 0) -
                (Keyboard.current.sKey.isPressed ? 1 : 0);
        }

        Vector3 move =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        controller.Move(move * speed * Time.deltaTime);

        // =============================
        // Mouse Look
        // =============================
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue() * lookSpeed * 0.1f;

            // Horizontal rotation (yaw)
            transform.Rotate(Vector3.up * mouseDelta.x);

            // Vertical rotation (pitch)
            verticalLookRotation -= mouseDelta.y;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

            cameraHolder.localRotation =
                Quaternion.Euler(verticalLookRotation, 0f, 0f);
        }

        // =============================
        // Jump
        // =============================
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
        }

        // =============================
        // Gravity
        // =============================
        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
