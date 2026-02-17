using UnityEngine;

/// <summary>
/// HUB JOINT SETUP — FINAL (DUAL SECTION, TORQUE-SAFE)
/// --------------------------------------------------
/// Creates structural ConfigurableJoints between a kinematic hub
/// and two rotating station sections (upper + lower).
///
/// Responsibilities:
/// ✔ Create joints
/// ✔ Lock translation
/// ✔ Allow rotation on ONE axis only
///
/// Non-responsibilities:
/// ✘ Apply torque
/// ✘ Control rotation speed
/// ✘ Modify inertia tensors
/// </summary>
[DisallowMultipleComponent]
public class HubJointSetup : MonoBehaviour
{
    // =====================================================
    // References
    // =====================================================

    [Header("Station Sections")]
    public Rigidbody upperSectionRb;
    public Rigidbody lowerSectionRb;

    [Header("Attach Points (Transform ONLY)")]
    public Transform upperAttachPoint;
    public Transform lowerAttachPoint;

    [Header("Hub Rigidbody")]
    [Tooltip("If null, a kinematic Rigidbody will be created.")]
    public Rigidbody hubRb;

    // =====================================================
    // Spin Axis
    // =====================================================

    [Header("Spin Axis (LOCAL to section)")]
    [Tooltip("Axis that is allowed to rotate (usually X for your station).")]
    public Vector3 spinAxis = Vector3.right;

    // =====================================================
    // Lifecycle
    // =====================================================

    void Awake()
    {
        if (!Validate())
            return;

        ConfigureHub();
        ConfigureSection(upperSectionRb, upperAttachPoint);
        ConfigureSection(lowerSectionRb, lowerAttachPoint);
    }

    // =====================================================
    // Validation
    // =====================================================

    bool Validate()
    {
        if (!upperSectionRb || !lowerSectionRb)
        {
            Debug.LogError("HubJointSetup: Both station rigidbodies must be assigned.");
            return false;
        }

        if (!upperAttachPoint || !lowerAttachPoint)
        {
            Debug.LogError("HubJointSetup: Both attach points must be assigned.");
            return false;
        }

        return true;
    }

    // =====================================================
    // Hub Configuration
    // =====================================================

    void ConfigureHub()
    {
        if (!hubRb)
        {
            hubRb = GetComponent<Rigidbody>();
            if (!hubRb)
                hubRb = gameObject.AddComponent<Rigidbody>();
        }

        hubRb.isKinematic = true;
        hubRb.useGravity = false;
        hubRb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // =====================================================
    // Section Setup
    // =====================================================

    void ConfigureSection(Rigidbody sectionRb, Transform attachPoint)
    {
        sectionRb.useGravity = false;
        sectionRb.isKinematic = false;
        sectionRb.interpolation = RigidbodyInterpolation.Interpolate;

        // HARD RULE: position is owned by the joint, not physics
        sectionRb.constraints = RigidbodyConstraints.FreezePosition;

        BuildJoint(sectionRb, attachPoint);
    }

    // =====================================================
    // Joint Creation
    // =====================================================

    void BuildJoint(Rigidbody sectionRb, Transform attachPoint)
    {
        var joint = sectionRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = hubRb;

        // Anchor alignment (CRITICAL)
        Vector3 worldAnchor = attachPoint.position;
        joint.anchor = sectionRb.transform.InverseTransformPoint(worldAnchor);
        joint.connectedAnchor = hubRb.transform.InverseTransformPoint(worldAnchor);

        // Lock ALL linear motion
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        // Lock ALL angular motion first
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        // Unlock ONLY the spin axis
        if (spinAxis == Vector3.right)
            joint.angularXMotion = ConfigurableJointMotion.Free;
        else if (spinAxis == Vector3.up)
            joint.angularYMotion = ConfigurableJointMotion.Free;
        else
            joint.angularZMotion = ConfigurableJointMotion.Free;

        // Projection keeps things numerically stable
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = 0.01f;
        joint.projectionAngle = 1f;
    }
}
