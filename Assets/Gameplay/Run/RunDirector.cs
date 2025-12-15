using System.Collections.Generic;
using UnityEngine;

// ==================================================
// SCRIPT ROLE: CONTROLS FLOW
// SYSTEM: Run
// RESPONSIBILITY: Advances the run room by room
// ==================================================

public enum RoomRole
{
    Entry,
    Combat,
    Breather,
    Combat2,
    PressureSpike,
    Boss
}

public class RunDirector : MonoBehaviour
{
    private List<RoomRole> demoRun;

    private void Start()
    {
        demoRun = new List<RoomRole>
        {
            RoomRole.Entry,
            RoomRole.Combat,
            RoomRole.Breather,
            RoomRole.Combat2,
            RoomRole.PressureSpike,
            RoomRole.Boss
        };

        GameStateManager.Instance.StartNewRun();
        EnterNextRoom();
    }

    void EnterNextRoom()
    {
        RunState run = GameStateManager.Instance.RunState;

        if (run.currentRoomIndex >= demoRun.Count)
        {
            GameStateManager.Instance.EndRun();
            return;
        }

        RoomRole role = demoRun[run.currentRoomIndex];
        Debug.Log($"Entering room {run.currentRoomIndex}: {role}");

        ApplyTension(role);
        run.currentRoomIndex++;

        Invoke(nameof(EnterNextRoom), 1f);
    }

    void ApplyTension(RoomRole role)
    {
        RunState run = GameStateManager.Instance.RunState;

        if (role == RoomRole.Combat || role == RoomRole.Combat2)
            run.runTension += 2;

        if (role == RoomRole.PressureSpike)
            run.runTension += 3;

        Debug.Log("Run tension: " + run.runTension);
    }
}
