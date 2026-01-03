using UnityEngine;
using System.Collections;

public class BreatherVisualPulse : MonoBehaviour
{
    [SerializeField] SpriteRenderer target;
    [SerializeField] float duration = 0.25f;

    Color original;

    void Awake()
    {
        if (target == null)
            target = GetComponent<SpriteRenderer>();

        if (target != null)
            original = target.color;
    }

    public void Pulse(Color color)
    {
        if (target == null) return;
        StopAllCoroutines();
        StartCoroutine(PulseRoutine(color));
    }

    IEnumerator PulseRoutine(Color color)
    {
        target.color = color;
        yield return new WaitForSeconds(duration);
        target.color = original;
    }
}
