using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    public void SelfDestruct()
    {
        gameObject.SetActive(false);
    }
    public void SelfConstruct()
    {
        gameObject.SetActive(true);
    }
}