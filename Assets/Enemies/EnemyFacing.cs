using UnityEngine;

public class EnemyFacing : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float turnSpeed = 720f;

    private EnemyAgent agent;
    private Transform player;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();

        if (visualRoot == null)
            visualRoot = transform;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void LateUpdate()
    {
        if (agent == null || player == null || agent.coordinator == null)
            return;

        Vector3 lookDir;

        // 🔑 FORMATION RULE
        if (agent.IsChangingFormation())
        {
            lookDir = agent.GetFormationTarget() - transform.position;
        }
        else
        {
            lookDir = player.position - transform.position;
        }

        if (lookDir.sqrMagnitude < 0.001f)
            return;

        // 🔑 UP is forward → subtract 90 degrees
        float angle =
            Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;

        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);

        visualRoot.localRotation = Quaternion.RotateTowards(
            visualRoot.localRotation,
            targetRot,
            turnSpeed * Time.deltaTime
        );
    }
}
