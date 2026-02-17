using UnityEngine;

/// <summary>
/// SECTION SPIN DRIVE — FINAL (TORQUE + PER-AXIS INERTIA)
/// =====================================================
///
/// Rotates a large space-station section using REAL physics torque,
/// not joint motors (which are unreliable for continuous rotation).
///
/// Key features:
/// • Spins around LOCAL Y axis
/// • Uses torque toward a target RPM
/// • Supports large-scale scenes via per-axis inertia tensor
/// • Fully deterministic and XR-safe
///
/// Attach to:
///     - SpaceStationUpper
///     - SpaceStationLower
///
/// Requires:
///     - Rigidbody (non-kinematic)
///     - HubJointSetup controlling allowed axes
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class SectionSpinDrive : MonoBehaviour
{
    // =====================================================
    // Spin Control
    // =====================================================

    [Header("Spin Control")]
    [Tooltip("Target rotation speed in RPM.")]
    public float targetRPM = 5f;

    [Tooltip("Direction of spin when viewed from above.")]
    public bool clockwise = true;

    // =====================================================
    // Torque Behaviour
    // =====================================================

    [Header("Torque Behaviour")]
    [Tooltip("How aggressively torque is applied toward target RPM.")]
    public float torqueStrength = 3000f;

    [Tooltip("Hard safety clamp on angular velocity (rad/sec).")]
    public float maxAngularSpeed = 50f;

    // =====================================================
    // Inertia Control (CRITICAL FOR LARGE SCENES)
    // =====================================================

    [Header("Inertia Override (Large-Scale Scenes)")]
    [Tooltip("If enabled, overrides Unity's automatic inertia tensor.")]
    public bool overrideInertia = true;

    [Tooltip(
        "Per-axis inertia tensor (LOCAL).\n" +
        "For large stations:\n" +
        "X/Z = small (1–50)\n" +
        "Y (spin axis) = large (500–10,000)"
    )]
    public Vector3 inertiaTensor = new Vector3(10f, 1000f, 10f);

    // =====================================================
    // Internal
    // =====================================================

    private Rigidbody rb;

    // =====================================================
    // Unity Lifecycle
    // =====================================================

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // ------------------------------
        // Basic Rigidbody sanity
        // ------------------------------
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // HubJointSetup owns translation & axis locking
        // We explicitly ensure rotation is NOT frozen
        rb.constraints &= ~RigidbodyConstraints.FreezeRotation;

        // ------------------------------
        // Inertia override (IMPORTANT)
        // ------------------------------
        if (overrideInertia)
        {
            rb.inertiaTensor = inertiaTensor;
            rb.inertiaTensorRotation = Quaternion.identity;
        }
    }

    void FixedUpdate()
    {
        // ------------------------------
        // Convert RPM → radians/sec
        // ------------------------------
        float targetRadPerSec = targetRPM * Mathf.PI * 2f / 60f;
        float direction = clockwise ? 1f : -1f;

        // ------------------------------
        // Current angular velocity (LOCAL Y)
        // ------------------------------
        float currentAngularSpeed = rb.angularVelocity.y;

        // ------------------------------
        // Simple proportional controller
        // ------------------------------
        float error = (direction * targetRadPerSec) - currentAngularSpeed;

        // ------------------------------
        // Apply torque along LOCAL Y
        // ------------------------------
        Vector3 torque = Vector3.up * error * torqueStrength;
        rb.AddRelativeTorque(torque, ForceMode.Force);

        // ------------------------------
        // Safety clamp (prevents runaway)
        // ------------------------------
        if (rb.angularVelocity.magnitude > maxAngularSpeed)
        {
            rb.angularVelocity =
                rb.angularVelocity.normalized * maxAngularSpeed;
        }
    }

#if UNITY_EDITOR
    // =====================================================
    // Debug Gizmo — visualise spin axis
    // =====================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.up * 3f
        );
    }
#endif
}
