using UnityEngine;

public class RunCorruptionState : MonoBehaviour
{
    public static RunCorruptionState Instance;

    [SerializeField] int corruptionLevel = 0;
    public bool corruptionAcceptedThisRun = false;
    public bool IsCorrupted => corruptionLevel > 0;

    public int CorruptionLevel => corruptionLevel;
    public int Level => corruptionLevel; // 👈 alias for convenience

    public bool IsLocked { get; private set; }

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
        if (IsLocked) return;

        corruptionLevel++;
        Debug.Log("CORRUPTION INCREASED → Level " + corruptionLevel);
    }

    public void RejectCorruption()
    {
        Debug.Log("CORRUPTION RESISTED");
    }

    public void Reduce(int amount)
    {
        if (IsLocked) return;

        corruptionLevel = Mathf.Max(0, corruptionLevel - amount);
        Debug.Log("Corruption reduced to " + corruptionLevel);
    }

    public void Increase(int amount)
    {
        if (IsLocked) return;

        corruptionLevel += amount;
        Debug.Log("Corruption increased to " + corruptionLevel);
    }

    public void LockCorruption()
    {
        IsLocked = true;
        Debug.Log("Corruption LOCKED");
    }
}
