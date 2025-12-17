using UnityEngine;

public class RunCorruptionState : MonoBehaviour
{
    public static RunCorruptionState Instance;

    public int corruptionLevel = 0;
    public bool corruptionAcceptedThisRun = false;

    public int CorruptionLevel { get; private set; }

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
        CorruptionLevel++;
        Debug.Log("CORRUPTION INCREASED → Level " + CorruptionLevel);
    }

    public void RejectCorruption()
    {
        Debug.Log("CORRUPTION RESISTED");
    }

}
