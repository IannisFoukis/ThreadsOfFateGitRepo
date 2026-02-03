using UnityEngine;

public enum Lane
{
    Front,
    Flank,
    Rear
}

public class LaneDirector : MonoBehaviour
{
    [Header("Inputs")]
    public PressureProfile profile;

    [Header("Quota")]
    [Tooltip("Minimum guaranteed slots per lane (if enough agents exist)")]
    public int minFront = 1;
    public int minFlank = 0;
    public int minRear = 0;

    [Tooltip("Soft cap, prevents all agents piling into one lane")]
    public float laneSaturationPenalty = 0.65f;

    private CombatRoomContext ctx;

    // current live counts (you feed this from coordinator each tick)
    private int countFront, countFlank, countRear;
    private int totalAgents;

    public void SetContext(CombatRoomContext context)
    {
        ctx = context;
    }

    public void SetLaneCounts(int front, int flank, int rear)
    {
        countFront = front;
        countFlank = flank;
        countRear = rear;
        totalAgents = Mathf.Max(0, front + flank + rear);
    }

    public Vector3 GetLaneWeights()
    {

        if (profile == null)
            return Vector3.one;

        var w = profile.EvaluateLaneWeights(ctx);
        Debug.Log($"[LANES] F={w.x:F2} L={w.y:F2} R={w.z:F2}");
        float satFront = SaturationMultiplier(countFront);
        float satFlank = SaturationMultiplier(countFlank);
        float satRear = SaturationMultiplier(countRear);

        return new Vector3(w.x * satFront, w.y * satFlank, w.z * satRear);
    }

    private float SaturationMultiplier(int laneCount)
    {
        if (laneCount <= 0) return 1f;
        return 1f / (1f + laneCount * laneSaturationPenalty);
    }

    public (int front, int flank, int rear) ComputeQuotas(int desiredAgents)
    {
        desiredAgents = Mathf.Max(0, desiredAgents);
        var w = GetLaneWeights();

        float sum = w.x + w.y + w.z;
        if (sum <= 0.001f)
            return (desiredAgents, 0, 0);

        int qFront = Mathf.RoundToInt(desiredAgents * (w.x / sum));
        int qFlank = Mathf.RoundToInt(desiredAgents * (w.y / sum));
        int qRear = desiredAgents - qFront - qFlank;

        ApplyMins(desiredAgents, ref qFront, ref qFlank, ref qRear);

        int drift = desiredAgents - (qFront + qFlank + qRear);
        if (drift != 0) qFront += drift;

        qFront = Mathf.Max(0, qFront);
        qFlank = Mathf.Max(0, qFlank);
        qRear = Mathf.Max(0, qRear);

        return (qFront, qFlank, qRear);
    }

    private void ApplyMins(int total, ref int qF, ref int qL, ref int qR)
    {
        if (total <= 0)
        {
            qF = qL = qR = 0;
            return;
        }

        int minSum = minFront + minFlank + minRear;
        if (minSum <= 0 || total < minSum)
            return;

        qF = Mathf.Max(qF, minFront);
        qL = Mathf.Max(qL, minFlank);
        qR = Mathf.Max(qR, minRear);

        while (qF + qL + qR > total)
        {
            if (qF >= qL && qF >= qR) qF--;
            else if (qL >= qF && qL >= qR) qL--;
            else qR--;
        }
    }

    public Lane ChooseLaneForRole(EnemyRole role)
    {
        var w = GetLaneWeights();

        float bF = 1f, bL = 1f, bR = 1f;

        switch (role)
        {
            case EnemyRole.Defender: bF = 1.35f; bL = 0.9f; bR = 0.6f; break;
            case EnemyRole.Offender: bF = 1.0f; bL = 1.25f; bR = 0.75f; break;
            case EnemyRole.Ranger: bF = 0.55f; bL = 0.95f; bR = 1.45f; break;
            case EnemyRole.Activator: bF = 0.7f; bL = 1.1f; bR = 1.0f; break;
            case EnemyRole.Joker: bF = 1.0f; bL = 1.0f; bR = 1.0f; break;
        }

        float wf = w.x * bF;
        float wl = w.y * bL;
        float wr = w.z * bR;

        if (wf >= wl && wf >= wr) return Lane.Front;
        if (wl >= wf && wl >= wr) return Lane.Flank;
        return Lane.Rear;
    }
}

