using UnityEngine;

/// <summary>
/// STABLE SECTION ROTATOR (Physics-Safe, Joint-Friendly)
/// ----------------------------------------------------
/// PURPOSE:
/// Drives a slow, controlled rotation around the LOCAL Y axis
/// of a station section using physics torque.
///
/// THIS SCRIPT:
/// ✔ Applies torque in FixedUpdate (physics-correct)
/// ✔ Respects Rigidbody constraints
/// ✔ Avoids solver fighting
/// ✔ Works with FixedJoint / hub systems
/// ✔ Does NOT override inertia tensors
///
/// THIS SCRIPT DOES NOT:
/// ✘ Move transforms directly
/// ✘ Touch center of mass
/// ✘ Modify joints
/// ✘ Apply forces off-axis
///
/// IMPORTANT ARCHITECTURE RULE:
/// ▶ This script MUST live on the SAME GameObject as the Rigidbody
/// ▶ That Rigidbody must be the ONLY Rigidbody for that section
/// ▶ Attach points must be Transform-only (no Rigidbody)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class StableSectionRotator : MonoBehaviour
{
    // -------------------- Rotation Direction --------------------

    [Header("Rotation Direction")]
    [Tooltip("If true, rotates clockwise when viewed from above (local +Y).")]
    public bool clockwise = true;

    // -------------------- Rotation Control --------------------

    [Header("Rotation Control")]
    [Tooltip("Torque strength applied per physics step (very small values!).")]
    [Range(0f, 0.1f)]
    public float torqueAmount = 0.02f;

    [Tooltip("Maximum allowed angular speed in radians per second.")]
    [Range(0.01f, 1f)]
    public float maxAngularSpeed = 0.2f;

    // -------------------- Debug --------------------

    [Header("Debug")]
    public bool drawAxisGizmo = true;

    // -------------------- Private --------------------

    private Rigidbody rb;

    // -------------------- Initialisation --------------------

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Safety: ensure predictable behaviour
        rb.useGravity = false;

        // IMPORTANT:
        // Let PhysX compute a correct inertia tensor for the actual mesh
        // DO NOT override inertiaTensor or centerOfMass
        rb.ResetInertiaTensor();

        // Limit angular velocity at the engine level
        rb.maxAngularVelocity = maxAngularSpeed;
    }

    // -------------------- Physics Update --------------------

    void FixedUpdate()
    {
        // Always rotate around the LOCAL up axis
        // This keeps rotation stable even if the section is tilted
        Vector3 rotationAxis = transform.up;

        // Choose direction
        if (!clockwise)
            rotationAxis = -rotationAxis;

        // Soft clamp angular velocity (solver-friendly)
        rb.angularVelocity = Vector3.ClampMagnitude(
            rb.angularVelocity,
            maxAngularSpeed
        );

        // Apply torque as ACCELERATION:
        // - Independent of mass
        // - Stable for space / zero-G
        // - Plays nicely with joints
        rb.AddTorque(
            rotationAxis * torqueAmount,
            ForceMode.Acceleration
        );
    }

#if UNITY_EDITOR
    // -------------------- Editor Gizmos --------------------

    void OnDrawGizmosSelected()
    {
        if (!drawAxisGizmo) return;

        Gizmos.color = Color.cyan;

        // Visualise the actual rotation axis being used
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.up * 2f
        );
    }
#endif
}
