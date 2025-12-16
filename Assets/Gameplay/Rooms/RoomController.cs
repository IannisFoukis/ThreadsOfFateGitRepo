using UnityEngine;

// ==================================================
// SCRIPT ROLE: READS ONLY
// SYSTEM: Rooms
// RESPONSIBILITY: Base room lifecycle
// ==================================================

public abstract class RoomController : MonoBehaviour
{
    protected bool isCompleted = false;

    protected virtual void Start()
    {
        Debug.Log($"Room started: {GetType().Name}");
    }

    protected void CompleteRoom()
    {
        if (isCompleted) return;

        isCompleted = true;
        Debug.Log($"Room completed: {GetType().Name}");

        RunDirector director = FindFirstObjectByType<RunDirector>();

        director.OnRoomCompleted();

    }
}
