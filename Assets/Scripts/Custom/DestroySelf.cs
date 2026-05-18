using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    public void SelfDestruct()
    {
        Destroy(gameObject);
    }
}
