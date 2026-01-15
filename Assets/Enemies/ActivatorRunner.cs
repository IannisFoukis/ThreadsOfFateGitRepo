using UnityEngine;

[RequireComponent(typeof(EnemyChase))]
public class ActivatorRunner : MonoBehaviour
{
    [SerializeField] private float activationRange = 0.8f;

    private Transform shrineTransform;

    // Used by runtime logic (preferred)
    public void SetShrine(Transform shrine)
    {
        shrineTransform = shrine;
        Debug.Log($"[ActivatorRunner] Shrine set to {shrine.name}");
    }

    // Used by tests / inspectors
    public void SetShrine(Shrine shrine)
    {
        if (shrine == null)
        {
            Debug.LogWarning("[ActivatorRunner] SetShrine called with null Shrine");
            return;
        }

        SetShrine(shrine.transform);
    }

    private void Update()
    {
        if (shrineTransform == null)
            return;

        float dist = Vector2.Distance(transform.position, shrineTransform.position);
        if (dist <= activationRange)
            ActivateShrine();
    }

    private void ActivateShrine()
    {
        // Shrine handles activation via trigger / collider
        Debug.Log("[ActivatorRunner] Shrine reached → activation triggered");
    }
}
