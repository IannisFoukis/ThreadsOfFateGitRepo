using UnityEngine;

public class BossAgent : MonoBehaviour
{
    public int phase = 1;

    public float phase2Threshold = 0.6f;
    public float phase3Threshold = 0.3f;

    public void UpdatePhase(float healthPercent)
    {
        int newPhase = phase;

        if (healthPercent < phase3Threshold)
            newPhase = 3;
        else if (healthPercent < phase2Threshold)
            newPhase = 2;
        else
            newPhase = 1;

        if (newPhase != phase)
        {
            phase = newPhase;
            Debug.Log($"[BossAgent] Phase changed to {phase}");
        }
    }
}
