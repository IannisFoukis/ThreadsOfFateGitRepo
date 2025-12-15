using UnityEngine;

// ==================================================
// SCRIPT ROLE: CONTROLS FLOW
// SYSTEM: Core
// RESPONSIBILITY: Owns MetaState and RunState
// ==================================================

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public MetaState MetaState { get; private set; }
    public RunState RunState { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        MetaState = new MetaState();
    }

    public void StartNewRun()
    {
        RunState = new RunState();
        Debug.Log("=== NEW RUN STARTED ===");
    }

    public void EndRun()
    {
        MetaState.runsCompleted++;
        Debug.Log("=== RUN ENDED ===");
        Debug.Log("Runs completed: " + MetaState.runsCompleted);

        RunState = null;
    }
}
