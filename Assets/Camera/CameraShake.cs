using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    Vector3 startPos;

    void Awake()
    {
        Instance = this;
        startPos = transform.localPosition;
    }

    public void Shake(float strength, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(DoShake(strength, duration));
    }

    System.Collections.IEnumerator DoShake(float strength, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            transform.localPosition = startPos + (Vector3)Random.insideUnitCircle * strength;
            t += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = startPos;
    }
}
