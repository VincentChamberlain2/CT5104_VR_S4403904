using UnityEngine;
using UnityEngine.Events;
using System.Reflection;

/// <summary>
/// EventZone
/// 
/// This script creates a simple trigger volume that:
/// • Fires UnityEvents when the player enters/exits
/// • Optionally disables gravity
/// • Optionally enables FreeFly mode (zero-G controlled movement)
/// 
/// It works with:
/// • XR locomotion systems
/// • Rigidbody-based players
/// • The NonVRMovement controller
/// 
/// It is designed for prototype clarity, not production optimisation.
/// </summary>
public class EventZone : MonoBehaviour
{
    // ================================
    // SECTION 1 — BASIC TRIGGER SETTINGS
    // ================================

    [Header("Trigger Filtering")]
    [Tooltip("Only objects with this tag will activate this zone.")]
    public string requiredTag = "Player";

    // These allow designers to hook up animations, lights, etc.
    public UnityEvent onTriggerEnterEvent;
    public UnityEvent onTriggerExitEvent;

    // ================================
    // SECTION 2 — OPTIONAL MOVEMENT STATE CHANGES
    // ================================

    [Header("Optional Movement Overrides")]
    [Tooltip("Disable gravity while inside this zone.")]
    public bool toggleGravity = false;

    [Tooltip("Enable FreeFly (zero-G controlled movement).")]
    public bool enableFreeFly = false;

    // ================================
    // SECTION 3 — TRIGGER ENTRY
    // ================================

    private void OnTriggerEnter(Collider other)
    {
        // Ignore anything that isn't tagged as Player
        if (!other.CompareTag(requiredTag))
            return;

        // Apply movement state changes if requested
        ApplyMovementState(other.transform, enteringZone: true);

        // Fire designer-configured events
        onTriggerEnterEvent?.Invoke();
    }

    // ================================
    // SECTION 4 — TRIGGER EXIT
    // ================================

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(requiredTag))
            return;

        ApplyMovementState(other.transform, enteringZone: false);

        onTriggerExitEvent?.Invoke();
    }

    // ================================
    // SECTION 5 — APPLY MOVEMENT STATE
    // ================================

    /// <summary>
    /// This method updates gravity and FreeFly state depending on
    /// whether the player is entering or exiting the zone.
    /// </summary>
    private void ApplyMovementState(Transform target, bool enteringZone)
    {
        bool gravityState = !enteringZone; // Gravity OFF when entering, ON when exiting

        // --------------------------------
        // 1️⃣ Rigidbody support (if used)
        // --------------------------------
        Rigidbody rb = target.GetComponentInParent<Rigidbody>();
        if (rb != null && toggleGravity)
        {
            rb.useGravity = gravityState;
        }

        // --------------------------------
        // 2️⃣ Non-VR Movement Controller
        // --------------------------------
        NonVRMovement nonVR = target.GetComponentInParent<NonVRMovement>();
        if (nonVR != null)
        {
            if (toggleGravity)
                nonVR.useGravity = gravityState;

            if (enableFreeFly)
                nonVR.freeFlyMode = enteringZone;
        }

        // --------------------------------
        // 3️⃣ XR Toolkit (version-agnostic)
        // --------------------------------
        // Uses reflection so we don’t depend on XR version.
        MonoBehaviour[] components = target.GetComponentsInParent<MonoBehaviour>();

        foreach (var comp in components)
        {
            PropertyInfo gravityProperty =
                comp.GetType().GetProperty("useGravity");

            if (gravityProperty != null &&
                gravityProperty.PropertyType == typeof(bool) &&
                toggleGravity)
            {
                gravityProperty.SetValue(comp, gravityState);
            }
        }
    }
}