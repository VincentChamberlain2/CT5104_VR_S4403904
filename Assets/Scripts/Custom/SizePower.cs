using UnityEngine;

public class SizePower : MonoBehaviour
{
    public GameObject HeightOffset;
    public void Grow()
    {
        HeightOffset.transform.position += new Vector3(0,0.5f,0);
        gameObject.GetComponent<AudioSource>().Play();
    }
    public void Shrink()
    {
        HeightOffset.transform.position -= new Vector3(0, 0.5f, 0);
        gameObject.GetComponent<AudioSource>().Play();
    }
}