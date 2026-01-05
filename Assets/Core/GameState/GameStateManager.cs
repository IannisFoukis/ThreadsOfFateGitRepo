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

        // 🚫 DO NOT auto-start run here
        // FindAnyObjectByType<RunDirector>()?.BeginRun();

        // Ensure player exists
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found in Entry scene. Ensure the Entry scene provides a Player GameObject tagged 'Player'.");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            return;
        }

        // Reactivate persistent player if it was deactivated on death
        if (!player.activeInHierarchy)
            player.SetActive(true);

        // Reset health
        var h = player.GetComponent<Health>();
        if (h != null)
            h.currentHealth = h.maxHealth;

        var ps = player.GetComponent<PlayerStats>() ?? PlayerStats.Instance;
        if (ps != null)
            ps.currentHealth = ps.maxHealth;

        // Ensure player controller is enabled
        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
            pc.enabled = true;

        // Clear global locks / pause
        GameLock.IsLocked = false;
        Time.timeScale = 1f;

        // Clear any pending choice state
        ChoiceManager.Instance?.FinishChoice();

        // Move player to spawn point
        if (PlayerSpawnPoint.Active != null)
        {
            player.transform.position = PlayerSpawnPoint.Active.transform.position;
        }
        else
        {
            var sp = Object.FindAnyObjectByType<PlayerSpawnPoint>();
            if (sp != null)
                player.transform.position = sp.transform.position;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
   

    public void EndRun(RunEndReason reason)
    {
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
