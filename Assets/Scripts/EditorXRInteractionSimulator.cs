using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// EDITOR XR INTERACTION SIMULATOR (UNITY 6 / XRIT 2.5+)
/// ===================================================
///
/// PURPOSE
/// -------
/// Allows XR Interactables to be tested in the Unity Editor
/// WITHOUT a VR headset, using keyboard + mouse.
///
/// This script simulates *XR selection events* rather than
/// full controller input. It is intended for:
///
/// • Desktop / Editor testing
/// • Teaching XR interaction concepts
/// • Rapid iteration when no headset is available
///
/// IMPORTANT
/// ---------
/// • Automatically disables itself if real XR is active
/// • Never intended to run in-headset
/// • Uses modern, interface-based XR Interaction Toolkit APIs
/// </summary>
public class EditorXRInteractionSimulator : MonoBehaviour
{
    // ======================================================
    // INSPECTOR CONTROLS (STUDENT-FRIENDLY)
    // ======================================================

    [Header("Editor XR Simulation")]

    [Tooltip("Master toggle for editor-based XR simulation")]
    public bool enableEditorSimulation = true;

    [Tooltip("Camera used for raycasting (typically a non-VR camera)")]
    public Camera raycastCamera;

    [Header("Input Keys (Editor Only)")]

    [Tooltip("Key to simulate a quick 'poke' (select + release)")]
    public KeyCode pokeKey = KeyCode.E;

    [Tooltip("Key to grab / release an interactable")]
    public KeyCode grabKey = KeyCode.G;

    [Header("Interaction Settings")]

    [Tooltip("Maximum distance for interaction raycasts")]
    public float maxDistance = 3f;

    // ======================================================
    // XR RUNTIME REFERENCES
    // ======================================================

    /// <summary>
    /// Central XR interaction manager.
    /// Required for *all* XR interaction events.
    /// </summary>
    private XRInteractionManager interactionManager;

    /// <summary>
    /// Hidden XRDirectInteractor used to fire select events.
    /// Implements IXRSelectInteractor.
    /// </summary>
    private XRDirectInteractor dummyInteractor;

    // ======================================================
    // GRAB STATE
    // ======================================================

    /// <summary>
    /// Currently grabbed interactable (interface-based).
    /// </summary>
    private IXRSelectInteractable grabbedInteractable;

    /// <summary>
    /// Rigidbody of the grabbed object (optional).
    /// Used for simple pull-to-camera behaviour.
    /// </summary>
    private Rigidbody grabbedRigidbody;

    // ======================================================
    // UNITY LIFECYCLE
    // ======================================================

    private void Start()
    {
        // --------------------------------------------------
        // 1. Disable if real XR is active
        // --------------------------------------------------
        // Prevents double-interaction and controller conflicts.
        if (IsXRActive())
        {
            enabled = false;
            return;
        }

        // --------------------------------------------------
        // 2. Respect inspector toggle
        // --------------------------------------------------
        if (!enableEditorSimulation)
        {
            enabled = false;
            return;
        }

        // --------------------------------------------------
        // 3. Find XRInteractionManager (Unity 6 safe)
        // --------------------------------------------------
        interactionManager = FindAnyObjectByType<XRInteractionManager>();

        if (interactionManager == null)
        {
            Debug.LogWarning(
                "[EditorXRInteractionSimulator] No XRInteractionManager found. " +
                "Editor XR simulation disabled."
            );
            enabled = false;
            return;
        }

        // --------------------------------------------------
        // 4. Create the dummy interactor
        // --------------------------------------------------
        CreateDummyInteractor();
    }

    private void Update()
    {
        if (!enableEditorSimulation)
            return;

        // --------------------------------------------------
        // Simulate a poke (select + release)
        // --------------------------------------------------
        if (Input.GetKeyDown(pokeKey))
        {
            SimulatePoke();
        }

        // --------------------------------------------------
        // Grab / release toggle
        // --------------------------------------------------
        if (Input.GetKeyDown(grabKey))
        {
            if (grabbedInteractable == null)
                SimulateGrab();
            else
                SimulateRelease();
        }

        // --------------------------------------------------
        // Pull grabbed object toward camera
        // --------------------------------------------------
        // NOTE:
        // Unity 6 prefers Rigidbody.linearVelocity over velocity.
        if (grabbedInteractable != null && grabbedRigidbody != null)
        {
            Vector3 targetPosition =
                raycastCamera.transform.position +
                raycastCamera.transform.forward * 0.6f;

            grabbedRigidbody.linearVelocity =
                (targetPosition - grabbedRigidbody.position) * 10f;
        }
    }

    // ======================================================
    // DUMMY INTERACTOR CREATION
    // ======================================================

    /// <summary>
    /// Creates a hidden XRDirectInteractor that satisfies
    /// all XR validation requirements.
    ///
    /// IMPORTANT:
    /// Collider + Rigidbody MUST exist BEFORE the interactor
    /// is added, or XR validation will fail.
    /// </summary>
    private void CreateDummyInteractor()
    {
        GameObject interactorGO =
            new GameObject("Editor_Dummy_XRDirectInteractor");

        // 1️⃣ Trigger collider (required by XRDirectInteractor)
        SphereCollider trigger = interactorGO.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.08f;

        // 2️⃣ Rigidbody (required for trigger detection)
        Rigidbody rb = interactorGO.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 3️⃣ XRDirectInteractor (added last)
        dummyInteractor = interactorGO.AddComponent<XRDirectInteractor>();
        dummyInteractor.interactionManager = interactionManager;

        // Hide helper object from hierarchy
        interactorGO.hideFlags = HideFlags.HideInHierarchy;
    }

    // ======================================================
    // INTERACTION SIMULATION
    // ======================================================

    private void SimulatePoke()
    {
        if (!RaycastForInteractable(out XRBaseInteractable interactable))
            return;

        interactionManager.SelectEnter(
            (IXRSelectInteractor)dummyInteractor,
            (IXRSelectInteractable)interactable
        );

        interactionManager.SelectExit(
            (IXRSelectInteractor)dummyInteractor,
            (IXRSelectInteractable)interactable
        );

        Debug.Log($"[Editor XR Poke] {interactable.name}");
    }

    private void SimulateGrab()
    {
        if (!RaycastForInteractable(out XRBaseInteractable interactable))
            return;

        interactionManager.SelectEnter(
            (IXRSelectInteractor)dummyInteractor,
            (IXRSelectInteractable)interactable
        );

        grabbedInteractable = interactable;
        grabbedRigidbody = interactable.GetComponent<Rigidbody>();

        if (grabbedRigidbody != null)
        {
            grabbedRigidbody.isKinematic = false;
            grabbedRigidbody.useGravity = true;
        }

        Debug.Log($"[Editor XR Grab] {interactable.name}");
    }

    private void SimulateRelease()
    {
        if (grabbedInteractable == null)
            return;

        interactionManager.SelectExit(
            (IXRSelectInteractor)dummyInteractor,
            grabbedInteractable
        );

        grabbedInteractable = null;
        grabbedRigidbody = null;

        Debug.Log("[Editor XR Release]");
    }

    // ======================================================
    // RAYCAST HELPER
    // ======================================================

    /// <summary>
    /// Raycasts forward from the camera and attempts to find
    /// an XRBaseInteractable on the hit collider.
    /// </summary>
    private bool RaycastForInteractable(out XRBaseInteractable interactable)
    {
        interactable = null;

        if (raycastCamera == null)
            return false;

        Ray ray = new Ray(
            raycastCamera.transform.position,
            raycastCamera.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            interactable = hit.collider.GetComponent<XRBaseInteractable>();
            return interactable != null;
        }

        return false;
    }

    // ======================================================
    // XR MODE DETECTION (UNITY 6 SAFE)
    // ======================================================

    /// <summary>
    /// Returns true if XR has successfully initialised.
    /// Used to auto-disable editor simulation.
    /// </summary>
    private bool IsXRActive()
    {
#if UNITY_XR_MANAGEMENT
        return UnityEngine.XR.Management.XRGeneralSettings.Instance != null &&
               UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager != null &&
               UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.isInitializationComplete;
#else
        return false;
#endif
    }
}
