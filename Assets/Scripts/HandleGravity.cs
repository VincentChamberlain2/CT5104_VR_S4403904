using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ZeroGravityTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Ensure XR Rig has "Player" tag
        {
            Rigidbody playerRB = other.GetComponent<Rigidbody>();
            if (playerRB != null)
            {
                playerRB.useGravity = false;  // Disable gravity
                playerRB.linearDamping = 0;            // Reduce drag for zero-G effect
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) // When exiting, restore gravity at new position
        {
            Rigidbody playerRB = other.GetComponent<Rigidbody>();
            if (playerRB != null)
            {
                playerRB.useGravity = true;   // Re-enable gravity
                playerRB.linearDamping = 1;            // Restore normal movement resistance
            }
        }
    }
}
