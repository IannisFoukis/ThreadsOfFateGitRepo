using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("RunDirector persistent.");
    }

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

        Debug.Log("RunDirector ready.");
    }
    public void BeginRun()
    {
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
        Debug.Log($"Loading room {run.currentRoomIndex}: {role}");

        ApplyTension(role);
        run.currentRoomIndex++;

        SceneManager.LoadScene(GetSceneName(role));
    }
    string GetSceneName(RoomRole role) //Helper
    {
        return role switch
        {
            RoomRole.Entry => "Room_Entry",
            RoomRole.Combat => "Room_Combat",
            RoomRole.Breather => "Room_Breather",
            RoomRole.PressureSpike => "Room_PressureSpike",
            RoomRole.Boss => "Room_Boss",
            _ => "Room_Entry"
        };
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
    public void OnRoomCompleted()
    {
        EnterNextRoom();
    }

}
