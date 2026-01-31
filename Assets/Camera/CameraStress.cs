using UnityEngine;

public class CameraStress : MonoBehaviour
{
    [Header("Stress")]
    public float maxShake = 0.15f;
    public float shakeSpeed = 6f;

    private Vector3 basePos;
    private float stressLevel;

    void Awake()
    {
        basePos = transform.localPosition;
    }

    void Update()
    {
        var coord = FindAnyObjectByType<EncounterCoordinator>();
        if (coord == null)
            return;

        stressLevel = GetStress(coord);

        Vector2 shake = Random.insideUnitCircle * maxShake * stressLevel;
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            basePos + (Vector3)shake,
            Time.deltaTime * shakeSpeed
        );
    }

    float GetStress(EncounterCoordinator coord)
    {
        switch (coord.phalanxState)
        {
            case EncounterCoordinator.PhalanxState.Encircle:
                return 0.3f;
            case EncounterCoordinator.PhalanxState.Collapse:
                return 1f;
            default:
                return 0f;
        }
    }
}