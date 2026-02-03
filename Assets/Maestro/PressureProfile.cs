using UnityEngine;

[CreateAssetMenu(menuName = "TOF/Combat/Pressure Profile")]
public class PressureProfile : ScriptableObject
{
    [Header("Base Lane Weights")]
    [Range(0f, 5f)] public float front = 1.0f;
    [Range(0f, 5f)] public float flank = 0.7f;
    [Range(0f, 5f)] public float rear = 0.4f;

    [Header("Escalation")]
    [Range(0f, 1.5f)] public float tierRamp = 0.25f;
    [Range(0f, 1.5f)] public float timeRamp = 0.15f;

    [Header("Room Modifiers")]
    [Range(-1f, 2f)] public float shrineActiveFrontBonus = 0.35f;
    [Range(-1f, 2f)] public float lowPlayerHPFrontBonus = 0.50f;
    [Range(-1f, 2f)] public float lowIntegrityFlankBonus = 0.45f;

    [Header("Caps")]
    [Range(0.1f, 10f)] public float maxWeight = 3.5f;

    public Vector3 EvaluateLaneWeights(CombatRoomContext ctx)
    {
        float t = Mathf.Max(0f, ctx.timeInRoom);
        float tier = Mathf.Max(0, ctx.difficultyTier);

        float wFront = front;
        float wFlank = flank;
        float wRear = rear;

        float ramp = 1f + (tier * tierRamp) + (t * timeRamp * 0.01f);
        wFront *= ramp;
        wFlank *= ramp;
        wRear *= ramp;

        if (ctx.shrinePresent && ctx.shrineActive)
            wFront += shrineActiveFrontBonus;

        if (ctx.playerLowHealth)
            wFront += lowPlayerHPFrontBonus;

        if (ctx.formationIntegrity < 0.45f)
            wFlank += lowIntegrityFlankBonus * (1f - ctx.formationIntegrity);

        float p = Mathf.Clamp01(ctx.playerPressure);
        wFlank += 0.35f * p;
        wRear += 0.25f * p;

        wFront = Mathf.Clamp(wFront, 0.01f, maxWeight);
        wFlank = Mathf.Clamp(wFlank, 0.01f, maxWeight);
        wRear = Mathf.Clamp(wRear, 0.01f, maxWeight);

        return new Vector3(wFront, wFlank, wRear);
    }
}