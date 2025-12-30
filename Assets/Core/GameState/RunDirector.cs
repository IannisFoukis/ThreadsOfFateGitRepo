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
    Combat1,
    PressureSpike,
    Combat2,
    Boss
}

public class RunDirector : MonoBehaviour
{
    private GameStateManager gsm;
    private List<RoomRole> demoRun;

    [Header("Data")]
    [SerializeField] BiomeConfig biomeConfig;

    // internal convenience list built from biomeConfig or fallback demoRun
    private List<BiomeConfig.RoomEntry> biomeEntries;
    private void Awake()
    {

        DontDestroyOnLoad(gameObject);
        gsm = FindAnyObjectByType<GameStateManager>();
        if (gsm == null)
            Debug.LogError("RunDirector: GameStateManager not found!");
        Debug.Log("RunDirector persistent.");
    }

    private void Start()
    {
        demoRun = new List<RoomRole>
        {
            RoomRole.Entry,
            RoomRole.Combat,
            RoomRole.Breather,
            RoomRole.Combat1,
            RoomRole.PressureSpike,
            RoomRole.Combat2,
            RoomRole.Boss
        };

        Debug.Log("RunDirector ready.");
    }
    public void BeginRun()
    {
        if (gsm == null) return;
        gsm.StartNewRun();
        // Notify listeners a run started
        GameEvents.RaiseRunStart();

        EnterNextRoom();
    }

    void EnterNextRoom()
    {
        if (gsm == null)
        {
            Debug.LogError("RunDirector: GameStateManager missing");
            return;
        }

        RunState run = gsm.RunState;

        // Determine next role (from biome entries if present, otherwise demoRun)
        RoomRole chosenRole;

        if (biomeEntries != null)
        {
            if (run.currentRoomIndex >= biomeEntries.Count)
            {
                gsm.EndRun();
                return;
            }

            var entry = biomeEntries[run.currentRoomIndex];
            chosenRole = entry.role;
            Debug.Log($"Loading biome room {run.currentRoomIndex}: {chosenRole}");
            ApplyTension(chosenRole);
            run.currentRoomIndex++;
            // Notify listeners a room is starting
            GameEvents.RaiseRoomStart();
            SceneManager.LoadScene(GetSceneName(chosenRole));
            return;
        }

        if (run.currentRoomIndex >= demoRun.Count)
        {
            gsm.EndRun();
            return;
        }

        chosenRole = demoRun[run.currentRoomIndex];
        Debug.Log($"Loading room {run.currentRoomIndex}: {chosenRole}");

        ApplyTension(chosenRole);
        run.currentRoomIndex++;
        // Notify listeners a room is starting
        GameEvents.RaiseRoomStart();
        SceneManager.LoadScene(GetSceneName(chosenRole));
    }

    string GetSceneName(RoomRole role) //Helper
    {
        return role switch
        {
            RoomRole.Entry => "Room_Entry",
            RoomRole.Combat => "Room_Combat",
            RoomRole.Combat1 => "Room_Combat1",
            RoomRole.Combat2 => "Room_Combat2",
            RoomRole.Breather => "Room_Breather",
            RoomRole.PressureSpike => "Room_PressureSpike",
            RoomRole.Boss => "Room_Boss",
            _ => "Room_Entry"
        };
    }


    void ApplyTension(RoomRole role)
    {
        if (gsm == null)
        {
            Debug.LogError("RunDirector: GameStateManager missing");
            return;
        }

        RunState run = gsm.RunState;

        if (role == RoomRole.Combat || role == RoomRole.Combat1)
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
