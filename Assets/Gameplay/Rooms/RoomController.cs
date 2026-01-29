using UnityEngine;

public class RoomController : MonoBehaviour
{
    private bool completed = false;
    protected bool roomCompleted;

    protected virtual void Start()
    {
        Debug.Log($"Room started: {gameObject.name}");
    }

    public virtual void CompleteRoom()
    {
        if (roomCompleted)
            return;

        roomCompleted = true;

        Debug.Log($"Room completed: {gameObject.name}");

        var director = Object.FindFirstObjectByType<RoomDirector>();

        if (director == null)
        {
            Debug.LogError("[RoomController] RoomDirector not found");
            return;
        }

        director.NotifyRoomCompleted();
    }

}
