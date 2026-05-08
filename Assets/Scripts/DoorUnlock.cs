using System.Collections.Generic;
using UnityEngine;

public class DoorUnlock : MonoBehaviour
{
    public List<GameObject> locks = new List<GameObject>();
    public GameObject door;

    //On button press
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            foreach (GameObject obj in locks)
            {
                obj.SetActive(false);
            }
            door.GetComponent<Animator>().Play("Doors_Open");
        }
    }
}