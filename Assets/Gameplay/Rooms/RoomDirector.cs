using UnityEngine;

public class RoomDirector : MonoBehaviour
{
    public void NotifyRoomCompleted()
    {
        Debug.Log("[RoomDirector] Room completed");

        RunDirector director = FindFirstObjectByType<RunDirector>();
        if (director == null)
        {
            Debug.LogError("[RoomDirector] RunDirector not found");
            return;
        }

        director.OnRoomCompleted();
    }
}
