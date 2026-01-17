using UnityEngine;

public class EnemyFacing : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;

    private EnemyAgent agent;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();

        if (visualRoot == null)
            visualRoot = transform;
    }

    void LateUpdate()
    {
        if (agent == null)
            return;

        Vector2 dir = (agent.GetSmoothedTarget() - transform.position).normalized;

        if (dir.sqrMagnitude < 0.01f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        visualRoot.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
