using UnityEngine;

[DisallowMultipleComponent]
public class EnemyDebugGizmos : MonoBehaviour
{
    [Header("Toggle")]
    public bool draw = true;

    [Header("Radii")]
    public float offenderEngage = 1.8f;
    public float rangerEngage = 6.5f;
    public float formationToleranceMult = 3f;

    [Header("Colors")]
    public Color slotColor = new Color(0.2f, 1f, 1f, 0.9f);
    public Color engageColor = new Color(1f, 0.6f, 0.1f, 0.7f);
    public Color tolColor = new Color(0.3f, 1f, 0.3f, 0.5f);
    public Color lockColor = new Color(1f, 0.1f, 0.1f, 1f);

    EnemyAgent agent;
    EncounterCoordinator coord;
    Transform player;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();
        coord = agent != null ? agent.coordinator : null;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void OnDrawGizmos()
    {
        if (!draw) return;

        if (agent == null) agent = GetComponent<EnemyAgent>();
        if (agent != null && coord == null) coord = agent.coordinator;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // 1) Draw slot target + formation tolerance
        if (agent != null && coord != null)
        {
            Vector3 slot = agent.GetFormationTarget();
            Gizmos.color = slotColor;
            Gizmos.DrawSphere(slot, 0.08f);
            Gizmos.DrawLine(transform.position, slot);

            float tol = agent.SlotArrivalThreshold * formationToleranceMult;
            Gizmos.color = tolColor;
            Gizmos.DrawWireSphere(slot, tol);
        }

        // 2) Draw engage radius around PLAYER (per role)
        if (player != null && agent != null)
        {
            float engage = agent.role == EnemyRole.Offender ? offenderEngage :
                           agent.role == EnemyRole.Ranger ? rangerEngage : 1.5f;

            Gizmos.color = engageColor;
            Gizmos.DrawWireSphere(player.position, engage);
        }

        // 3) If locked, draw big red marker
        if (agent != null && agent.attackPositionLocked)
        {
            Gizmos.color = lockColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
        }
    }
}