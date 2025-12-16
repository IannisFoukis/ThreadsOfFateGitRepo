using UnityEngine;

public class RunCorruptionState : MonoBehaviour
{
    public static RunCorruptionState Instance;

    public int corruptionLevel = 0;
    public bool corruptionAcceptedThisRun = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AcceptCorruption()
    {
        corruptionLevel++;
        corruptionAcceptedThisRun = true;

        Debug.Log($"CORRUPTION ACCEPTED — Level {corruptionLevel}");
    }

    public void RejectCorruption()
    {
        Debug.Log("CORRUPTION REJECTED — CONSEQUENCE TRIGGERED");
    }
}
