using UnityEngine;

public class EntryGate : MonoBehaviour
{
    [Header("Behavior")]
    [Tooltip("If true, the gate will call GameStateManager.StartNewRun() (preferred). If not found, it falls back to RunDirector.BeginRun().")]
    [SerializeField] bool startRunOnCross = true;

    bool triggered = false;
    Collider2D gateCollider;

    void Awake()
    {
        gateCollider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        triggered = false;

        if (gateCollider != null)
            gateCollider.enabled = true;

        Debug.Log("[EntryGate] Reset");

        // Player may already be inside the gate
        CheckForPlayerOverlap();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger(other);
    }

    void CheckForPlayerOverlap()
    {
        if (gateCollider == null)
            return;

        var results = new Collider2D[8];
        var filter = new ContactFilter2D();
        filter.NoFilter();

        int count = gateCollider.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            if (results[i] == null) continue;
            TryTrigger(results[i]);
        }
    }

    void TryTrigger(Collider2D other)
    {
        if (triggered || !startRunOnCross)
            return;

        Transform root = other.transform.root;
        if (!root.CompareTag("Player"))
            return;

        triggered = true;

        Debug.Log("[EntryGate] Player crossed entry gate");

        if (gateCollider != null)
            gateCollider.enabled = false;

        // IMPORTANT:
        // We do NOT depend on EntryRoom being part of the BiomeConfig flow.
        // The EntryGate is the run-start trigger.
        StartRun();
    }

    void StartRun()
    {
        var runDirector = FindAnyObjectByType<RunDirector>();
        if (runDirector == null)
        {
            Debug.LogError("[EntryGate] RunDirector not found");
            return;
        }

        Debug.Log("[EntryGate] Starting run via gate");

        // This is SAFE — BeginRun is idempotent
        runDirector.BeginRun();

        // ⚠️ THIS WAS MISSING
        runDirector.EnterNextRoom();
    }

}
