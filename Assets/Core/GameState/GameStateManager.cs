using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public RunData RunData { get; private set; }
    public RunState RunState { get; private set; }
    [SerializeField] private GameStateManager gsm;
    bool endRunPending = false;
    RunEndReason pendingReason;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        RunData = new RunData();
        RunState = new RunState();
    }

    public void StartNewRun()
    {
        // Reset persistent run data
        RunData.Reset();

        // Reset runtime state
        RunState.Reset();

        // Reset per-run context data (rules + memory) safely
        if (RunContext.Instance != null)
        {
            RunContext.Instance.rules = new RunRules();    // resets all bools to false
            RunContext.Instance.memory = new RunMemory();  // resets per-run memory
        }


        Debug.Log("=== NEW RUN STARTED ===");
    }

    public void OnRoomCleared()
    {
        RunData.roomsCleared++;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!endRunPending)
            return;

        if (scene.name != "Room_Entry")
            return;

        endRunPending = false;

        // NOTHING player-related here anymore
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }



    public void EndRun(RunEndReason reason)
    {
        

        Debug.Log($"[RUN END] Reason = {reason}");

        if (endRunPending)
        {
            Debug.LogWarning($"EndRun already pending (reason={pendingReason}), ignoring duplicate call for {reason}");
            return;
        }

        Debug.Log($"[RUN END] Reason = {reason}");

        // 1️⃣ Escalate world (meta)
        switch (reason)
        {
            case RunEndReason.PlayerDied:
                RunData.deaths++;
                RunData.corruption += 1;
                break;

            case RunEndReason.BiomeCompleted:
                RunData.biomesCompleted++;
                RunData.corruption += 2; // feels harsher, tune later
                RunData.currentBiomeIndex++;
                break;
        }

        // 2️⃣ Reset runtime-only state
        RunState.Reset();

        // 3️⃣ Restart loop after scene load
        endRunPending = true;
        pendingReason = reason;
        var runDirector = FindAnyObjectByType<RunDirector>();
        if (runDirector != null)
        {
            runDirector.ResetRun();
        }
        else
        {
            Debug.LogError("[GameStateManager] RunDirector not found when ending run");
        }


        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene("Room_Entry");
    }

}
