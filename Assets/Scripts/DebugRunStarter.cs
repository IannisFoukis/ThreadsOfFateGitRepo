using UnityEngine;

public class DebugRunStarter : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.Log("DebugRunStarter ENABLED");
        InvokeRepeating(nameof(TryStartRun), 0.1f, 0.1f);
    }


    void TryStartRun()
    {
        GameStateManager gsm = FindFirstObjectByType<GameStateManager>();
        RunDirector director = FindFirstObjectByType<RunDirector>();

        if (gsm == null || director == null)
            return;

        if (GameStateManager.Instance == null)
            return;

        CancelInvoke(nameof(TryStartRun));

        Debug.Log("DebugRunStarter: starting run");
        director.BeginRun();
    }
}
