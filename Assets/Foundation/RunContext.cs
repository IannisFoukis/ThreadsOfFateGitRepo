using UnityEngine;

public class RunContext : MonoBehaviour
{
    public static RunContext Instance;

    public RunState progress;
    public RunMemory memory;
    public RunRules rules;

    private void Awake()
    {
        Debug.Log("[RunContext] Initialized");

        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        progress = new RunState();
        memory = new RunMemory();
        rules = new RunRules();
    }
}
