using UnityEngine;

public class ConstantRotation : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second.")]
    public Vector3 rotationSpeed = new Vector3(0f, 10f, 0f);

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }
}
