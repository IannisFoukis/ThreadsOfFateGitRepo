using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AreaSpawnTrigger : MonoBehaviour
{
    public SquadAnchor squad;
    private bool fired;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (fired) return;
        if (!other.CompareTag("Player")) return;

        fired = true;
        Debug.Log("[AreaSpawnTrigger] Player entered trigger");

        if (squad == null)
        {
            Debug.LogError("[AreaSpawnTrigger] SquadAnchor is NULL. Aborting activation.");
            return;
        }

        // 🔒 Phase G1 rule: ALWAYS initialize before activation
        squad.InitializeAfterSpawn();

        var coordinator = FindFirstObjectByType<EncounterCoordinator>();
        squad.Activate(coordinator);
    }
}
