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
    public bool IsLocked { get; private set; }

    public void Reduce(int amount)
    {
        if (IsLocked) return;

        CorruptionLevel = Mathf.Max(0, CorruptionLevel - amount);
        Debug.Log("Corruption reduced to " + CorruptionLevel);
    }

    public void Increase(int amount)
    {
        if (IsLocked) return;

        CorruptionLevel += amount;
        Debug.Log("Corruption increased to " + CorruptionLevel);
    }

    public void LockCorruption()
    {
        IsLocked = true;
        Debug.Log("Corruption LOCKED");
    }

}
