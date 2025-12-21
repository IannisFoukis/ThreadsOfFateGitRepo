using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public static int AliveCount = 0;

    [Header("Aggro State")]
    public bool isAggro;
    public bool isAggressive;
    public bool ignoreAssistLogic;

    [Header("Scaling")]
    public float damageMultiplier = 1f;

    private void OnEnable()
    {
        AliveCount++;
    }

    private void OnDisable()
    {
        AliveCount--;
    }

    // GLOBAL forced aggro (used by tension spikes / elites)
    public static void ForceImmediateAggro(float duration = 3f)
    {
        foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemy.ForceAggro(duration);
        }
    }

    // Instant aggro (permanent until changed by AI)
    public virtual void ForceAggro()
    {
        isAggro = true;
        isAggressive = true;
    }

    // Timed aggro escalation
    public void ForceAggro(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(ForceAggroRoutine(duration));
    }

    private IEnumerator ForceAggroRoutine(float duration)
    {
        Debug.Log($"[ENEMY] Forced aggro on {name} for {duration}s");

        isAggro = true;
        isAggressive = true;
        ignoreAssistLogic = true;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isAggressive = false;
        ignoreAssistLogic = false;
    }
}
