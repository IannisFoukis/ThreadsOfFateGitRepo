using UnityEngine;

public class EntryGate : MonoBehaviour
{
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

        var results = new Collider2D[4];
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
        if (triggered)
            return;

        Transform root = other.transform.root;
        if (!root.CompareTag("Player"))
            return;

        triggered = true;

        Debug.Log("[EntryGate] Player crossed entry gate");

        if (gateCollider != null)
            gateCollider.enabled = false;

        EntryRoom.CommitEntry();
    }
}
