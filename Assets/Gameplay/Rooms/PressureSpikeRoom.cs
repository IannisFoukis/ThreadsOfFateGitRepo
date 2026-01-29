using UnityEngine;

public class PressureSpikeRoom : RoomController
{
    protected override void Start()
    {
        base.Start();

        var contract = RoomAccess.Current;
        if (contract == null || !contract.pressureSpike)
            return;

        Debug.Log("[PressureSpikeRoom] Pressure spike active");

        Invoke(nameof(Release), 2.5f);
    }

    void Release()
    {
        Debug.Log("[PressureSpikeRoom] Pressure released – unlocking exit");

        // 1️⃣ Let the player leave (door / gate / collider)
        

        // 2️⃣ NOW complete the room
        CompleteRoom();
    }
}
