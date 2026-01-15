using UnityEngine;

public class EnemyFacing : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    // This can be the same GameObject or a child (recommended)

    private EnemyChase chase;

    void Awake()
    {
        chase = GetComponent<EnemyChase>();

        if (visualRoot == null)
            visualRoot = transform;
    }

    void LateUpdate()
    {
        if (chase == null) return;

        Vector2 dir = chase.CurrentDir;
        if (dir.sqrMagnitude < 0.01f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        visualRoot.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
