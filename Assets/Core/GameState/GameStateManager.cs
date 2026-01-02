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
        if (!endRunPending) return;
        if (scene.name == "Room_Entry")
        {
            endRunPending = false;
            // Restart run loop automatically
            FindAnyObjectByType<RunDirector>()?.BeginRun();
            // Ensure player is active, reset health and move to spawn point when returning to entry
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                // Try to instantiate a player prefab from Resources as a fallback
                var prefab = Resources.Load<GameObject>("Player");
                if (prefab != null)
                {
                    player = Instantiate(prefab);
                    player.tag = "Player";
                }
            }

            if (player != null)
            {
                // Reactivate persistent player if it was deactivated on death
                if (!player.activeInHierarchy)
                    player.SetActive(true);

                var h = player.GetComponent<Health>();
                if (h != null)
                {
                    h.currentHealth = h.maxHealth;
                }

                var ps = player.GetComponent<PlayerStats>() ?? PlayerStats.Instance;
                if (ps != null)
                {
                    ps.currentHealth = ps.maxHealth;
                }

                // Ensure player controller is enabled and game isn't locked/paused
                var pc = player.GetComponent<PlayerController>();
                if (pc != null)
                    pc.enabled = true;

                // Clear any global locks or time scale caused by previous run
                GameLock.IsLocked = false;
                Time.timeScale = 1f;

                // Clear pending choice state if any
                ChoiceManager.Instance?.FinishChoice();

                // If scene contains a spawn point, place the player there
                if (PlayerSpawnPoint.Active != null)
                {
                    player.transform.position = PlayerSpawnPoint.Active.transform.position;
                }
                else
                {
                    // Fallback: try to find a spawn point in scene
                    var sp = GameObject.FindObjectOfType<PlayerSpawnPoint>();
                    if (sp != null)
                        player.transform.position = sp.transform.position;
                }
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    public void EndRun()
    {
        Debug.Log("=== RUN ENDED ===");

        // Later:
        // - Save run results
        // - Return to hub
        // - Apply meta progression
    }

    public void EndRun(RunEndReason reason)
    {
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
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene("Room_Entry");
    }

}
