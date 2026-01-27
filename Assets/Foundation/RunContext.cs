using UnityEngine;

public class RunContext : MonoBehaviour
{
    public static RunContext Instance { get; private set; }

    public RunMemory memory;
    public RunRules rules;

    // 👇 ADD THIS
    public WorldModifiers worldModifiers;

    public RunEndReason lastRunEndReason;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 🔑 GUARANTEE NON-NULL
        if (memory == null)
            memory = new RunMemory();

        if (rules == null)
            rules = new RunRules();

        // 🔑 GUARANTEE NON-NULL (NEW)
        if (worldModifiers == null)
            worldModifiers = new WorldModifiers();

        Debug.Log("[RunContext] Initialized");
    }
}
