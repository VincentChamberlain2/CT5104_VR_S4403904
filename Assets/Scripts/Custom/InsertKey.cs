using UnityEngine;

public class InsertKey : MonoBehaviour
{
    public string keyName;

    public GameObject finalObject;
    public GameObject elevatorDoor;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == keyName)
        {
            elevatorDoor.GetComponent<DoorUnlock>().UpdateDoor();
            finalObject.SetActive(true);
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }
}