using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// SIMULATE POKE (EDITOR / DESKTOP ONLY)
/// ===================================
///
/// PURPOSE
/// -------
/// Allows XR Interactables to be "poked" (selected once)
/// in the Unity Editor without a VR headset.
///
/// A poke is defined here as:
/// • SelectEnter
/// • immediately followed by SelectExit
///
/// INTENDED USE
/// ------------
/// • Editor-only testing
/// • Teaching XR interaction fundamentals
/// • Desktop fallback when no headset is available
///
/// NOT INTENDED FOR
/// ----------------
/// • Real VR gameplay
/// • Continuous interaction / holding
/// </summary>
public class SimulatePoke3D : MonoBehaviour
{
    // ======================================================
    // INSPECTOR CONTROLS
    // ======================================================

    [Header("Input")]

    [Tooltip("Key used to simulate a poke interaction")]
    public KeyCode pokeKey = KeyCode.E;

    [Header("Raycast Settings")]

    [Tooltip("Maximum distance for poke raycast")]
    public float maxDistance = 3f;

    [Header("XR References (Optional)")]

    [Tooltip("XR Interaction Manager (auto-found if null)")]
    public XRInteractionManager interactionManager;

    [Tooltip("Dummy XRDirectInteractor (auto-created if null)")]
    public XRDirectInteractor dummyInteractor;

    // ======================================================
    // UNITY LIFECYCLE
    // ======================================================

    private void Start()
    {
        // --------------------------------------------------
        // 1. Find XRInteractionManager (Unity 6 safe)
        // --------------------------------------------------
        if (interactionManager == null)
        {
            interactionManager = FindAnyObjectByType<XRInteractionManager>();
        }

        if (interactionManager == null)
        {
            Debug.LogWarning(
                "[SimulatePoke3D] No XRInteractionManager found. Script disabled."
            );
            enabled = false;
            return;
        }

        // --------------------------------------------------
        // 2. Create dummy interactor if required
        // --------------------------------------------------
        if (dummyInteractor == null)
        {
            CreateDummyInteractor();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(pokeKey))
        {
            SimulatePoke();
        }
    }

    // ======================================================
    // DUMMY INTERACTOR SETUP
    // ======================================================

    /// <summary>
    /// Creates a hidden XRDirectInteractor that satisfies
    /// XR validation rules.
    ///
    /// IMPORTANT:
    /// Collider + Rigidbody MUST exist BEFORE
    /// XRDirectInteractor is added.
    /// </summary>
    private void CreateDummyInteractor()
    {
        GameObject interactorGO = new GameObject("Editor_Dummy_XRDirectInteractor");

        // Trigger collider (required)
        SphereCollider trigger = interactorGO.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.08f;

        // Rigidbody (required for trigger events)
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
    // POKE LOGIC
    // ======================================================

    /// <summary>
    /// Performs a raycast forward and simulates
    /// a single XR "poke" interaction.
    /// </summary>
    private void SimulatePoke()
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

        interactionManager.SelectExit(
            (IXRSelectInteractor)dummyInteractor,
            (IXRSelectInteractable)interactable
        );

        Debug.Log($"[SimulatePoke3D] Poked: {interactable.name}");
    }
}
