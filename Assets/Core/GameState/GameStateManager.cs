using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public RunData RunData { get; private set; }
    public RunState RunState { get; private set; }
    [SerializeField] private GameStateManager gsm;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        RunData = new RunData();
        RunState = new RunState();
    }

    public void StartNewRun()
    {
        // Reset persistent run data
        RunData.roomsCleared = 0;
        RunData.tension = 0;
        RunData.corruption = 0;

        // Reset runtime state
        RunState.currentRoomIndex = 0;
        RunState.runTension = 0;

        Debug.Log("=== NEW RUN STARTED ===");
    }

    public void OnRoomCleared()
    {
        RunData.roomsCleared++;
    }
    public void EndRun()
    {
        Debug.Log("=== RUN ENDED ===");

        // Later:
        // - Save run results
        // - Return to hub
        // - Apply meta progression
    }

}
