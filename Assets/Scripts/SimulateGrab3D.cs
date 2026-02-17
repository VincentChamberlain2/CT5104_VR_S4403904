using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// SIMULATE GRAB (EDITOR / DESKTOP ONLY)
/// ====================================
///
/// PURPOSE
/// -------
/// Allows XR Grab Interactables to be tested in the Unity Editor
/// without a VR headset, using a keyboard key and a forward raycast.
///
/// DESIGN NOTES
/// ------------
/// • This simulates XR *selection events*, not controller tracking
/// • Uses a hidden XRDirectInteractor
/// • Uses interface-based XRIT APIs (Unity 6 / XRIT 2.5+ safe)
/// • Intended for teaching, prototyping, and fallback testing
///
/// NOT INTENDED FOR
/// ----------------
/// • Real VR gameplay
/// • Shipping builds
/// </summary>
public class SimulateGrab3D : MonoBehaviour
{
    // ======================================================
    // INSPECTOR CONTROLS
    // ======================================================

    [Header("Input")]

    [Tooltip("Key to toggle grab / release")]
    public KeyCode grabKey = KeyCode.G;

    [Header("Raycast Settings")]

    [Tooltip("Maximum grab distance")]
    public float maxDistance = 3f;

    [Header("XR References (Optional)")]

    [Tooltip("XR Interaction Manager (auto-found if null)")]
    public XRInteractionManager interactionManager;

    [Tooltip("Dummy XRDirectInteractor (auto-created if null)")]
    public XRDirectInteractor dummyInteractor;

    // ======================================================
    // GRAB STATE
    // ======================================================

    /// <summary>
    /// Currently grabbed XR interactable (interface-based).
    /// </summary>
    private IXRSelectInteractable grabbedInteractable;

    /// <summary>
    /// Rigidbody of the grabbed object (if present).
    /// </summary>
    private Rigidbody grabbedRigidbody;

    /// <summary>
    /// Original parent transform (restored on release).
    /// </summary>
    private Transform originalParent;

    // ======================================================
    // UNITY LIFECYCLE
    // ======================================================

    private void Start()
    {
        // --------------------------------------------------
        // 1. Locate XRInteractionManager (Unity 6 safe)
        // --------------------------------------------------
        if (interactionManager == null)
        {
            interactionManager = FindAnyObjectByType<XRInteractionManager>();
        }

        if (interactionManager == null)
        {
            Debug.LogWarning(
                "[SimulateGrab3D] No XRInteractionManager found. Script disabled."
            );
            enabled = false;
            return;
        }

        // --------------------------------------------------
        // 2. Create dummy interactor if needed
        // --------------------------------------------------
        if (dummyInteractor == null)
        {
            CreateDummyInteractor();
        }
    }

    private void Update()
    {
        // --------------------------------------------------
        // Toggle grab / release
        // --------------------------------------------------
        if (Input.GetKeyDown(grabKey))
        {
            if (grabbedInteractable == null)
                SimulateGrab();
            else
                SimulateRelease();
        }

        // --------------------------------------------------
        // Pull grabbed object toward interactor
        // --------------------------------------------------
        // Uses Rigidbody.linearVelocity (Unity 6 preferred API)
        if (grabbedInteractable != null && grabbedRigidbody != null)
        {
            Vector3 targetPosition = dummyInteractor.transform.position;

            grabbedRigidbody.linearVelocity =
                (targetPosition - grabbedRigidbody.position) * 10f;
        }
    }

    // ======================================================
    // DUMMY INTERACTOR SETUP
    // ======================================================

    /// <summary>
    /// Creates a hidden XRDirectInteractor that satisfies
    /// XR validation rules (collider + rigidbody first).
    /// </summary>
    private void CreateDummyInteractor()
    {
        GameObject interactorGO = new GameObject("Editor_Dummy_XRDirectInteractor");

        // Trigger collider (required)
        SphereCollider trigger = interactorGO.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.08f;

        // Rigidbody (required for trigger detection)
        Rigidbody rb = interactorGO.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // XRDirectInteractor (added last)
        dummyInteractor = interactorGO.AddComponent<XRDirectInteractor>();
        dummyInteractor.interactionManager = interactionManager;

        // Hide helper object from hierarchy
        interactorGO.hideFlags = HideFlags.HideInHierarchy;
    }

    // ======================================================
    // GRAB / RELEASE LOGIC
    // ======================================================

    private void SimulateGrab()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            return;

        XRBaseInteractable interactable =
            hit.collider.GetComponent<XRBaseInteractable>();

        if (interactable == null)
            return;

        // --------------------------------------------------
        // XRIT 2.5+ INTERFACE-BASED SELECTION
        // --------------------------------------------------
        interactionManager.SelectEnter(
            (IXRSelectInteractor)dummyInteractor,
            (IXRSelectInteractable)interactable
        );

        grabbedInteractable = interactable;
        grabbedRigidbody = hit.collider.GetComponent<Rigidbody>();

        if (grabbedRigidbody != null)
        {
            // Ensure physics is active
            grabbedRigidbody.isKinematic = false;
            grabbedRigidbody.useGravity = true;
        }

        // Detach from any parent to avoid snapping
        originalParent = interactable.transform.parent;
        interactable.transform.SetParent(null);

        Debug.Log($"[SimulateGrab3D] Grabbed: {interactable.name}");
    }

    private void SimulateRelease()
    {
        if (grabbedInteractable == null)
            return;

        interactionManager.SelectExit(
            (IXRSelectInteractor)dummyInteractor,
            grabbedInteractable
        );

        if (grabbedRigidbody != null)
        {
            grabbedRigidbody.useGravity = true;
            grabbedRigidbody.isKinematic = false;
        }

        // Restore original parent (if any)
        if (originalParent != null)
            grabbedInteractable.transform.SetParent(originalParent);
        else
            grabbedInteractable.transform.SetParent(null);

        Debug.Log($"[SimulateGrab3D] Released");

        grabbedInteractable = null;
        grabbedRigidbody = null;
        originalParent = null;
    }
}
