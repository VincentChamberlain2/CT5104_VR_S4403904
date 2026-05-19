using System.Collections.Generic;
using UnityEngine;

public class DoorUnlock : MonoBehaviour
{
    public int remainingLocks;

    public bool isElevator;
    public void UpdateDoor()
    {
        remainingLocks--;
        if (remainingLocks <= 0)
        {
            OpenDoors();
        }
    }
    private void OpenDoors()
    {
        if (!isElevator)
        {
            gameObject.GetComponent<Animator>().SetBool("isOpened", true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}