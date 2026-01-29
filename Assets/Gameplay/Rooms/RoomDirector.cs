using UnityEngine;
using TOF.Rooms.Contracts;

public class RoomDirector : MonoBehaviour
{
    [Header("Room Contract")]
    public RoomContract contract;

    private bool completionSent = false;

    private void Start()
    {
        if (contract == null)
        {
            Debug.LogError($"[RoomDirector] RoomContract missing in scene {gameObject.scene.name}");
            return;
        }

        Debug.Log($"[RoomDirector] Room started | Role={contract.roomRole}");

        // 🔔 Notify observers only
        GameEvents.RaiseRoomStart();
    }

    public void NotifyRoomCompleted()
    {
        if (completionSent)
            return;

        completionSent = true;

        Debug.Log("[RoomDirector] Room completed");

        // 🔔 Notify observers only
        GameEvents.RaiseRoomCompleted();

        var runDirector = FindFirstObjectByType<RunDirector>();
        if (runDirector == null)
        {
            Debug.LogError("[RoomDirector] RunDirector not found");
            return;
        }

        runDirector.OnRoomCompleted();
    }
}
