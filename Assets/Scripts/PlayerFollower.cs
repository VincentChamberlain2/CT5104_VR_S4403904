using UnityEngine;

/// <summary>
/// PLAYER FOLLOWER — ROTATING STATION (PHYSICS-INERT)
/// ------------------------------------------------
/// Moves the player in a rotating reference frame
/// WITHOUT applying forces or collisions to the station.
///
/// This script:
/// ✔ Moves the player kinematically
/// ✔ Preserves local movement (WASD / XR)
/// ✔ Never interferes with station physics
///
/// Attach to:
///     - Player root (XR rig or non-VR controller)
/// </summary>
[DisallowMultipleComponent]
public class PlayerFollower : MonoBehaviour
{
    [Header("Station Reference")]
    public Rigidbody stationRb;
    public Transform rotationCenter;

    [Header("Options")]
    public bool followRotation = true;
    public bool followTangentialMotion = true;

    void LateUpdate()
    {
        if (!stationRb || !rotationCenter)
            return;

        Vector3 r = transform.position - rotationCenter.position;
        Vector3 omega = stationRb.angularVelocity;

        // Tangential displacement
        if (followTangentialMotion)
        {
            Vector3 tangentialVelocity = Vector3.Cross(omega, r);
            transform.position += tangentialVelocity * Time.deltaTime;
        }

        // Rotate player with station
        if (followRotation && omega.sqrMagnitude > 0.0001f)
        {
            Quaternion deltaRotation =
                Quaternion.AngleAxis(
                    omega.magnitude * Mathf.Rad2Deg * Time.deltaTime,
                    omega.normalized
                );

            transform.rotation = deltaRotation * transform.rotation;
        }
    }
}
