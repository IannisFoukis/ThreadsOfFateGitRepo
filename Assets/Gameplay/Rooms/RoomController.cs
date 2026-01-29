using UnityEngine;

public class RoomController : MonoBehaviour
{
    private bool completed = false;

    protected virtual void Start()
    {
        Debug.Log($"Room started: {gameObject.name}");
    }

    public void CompleteRoom()
    {
        if (completed)
            return; // 🔒 HARD GUARD

        completed = true;

        Debug.Log($"Room completed: {gameObject.name}");

        var director = FindFirstObjectByType<RoomDirector>();
        if (director == null)
        {
            Debug.LogWarning("[RoomController] RoomDirector not found (scene likely unloading)");
            return;
        }

        director.NotifyRoomCompleted();
    }
}
