using UnityEngine;

public enum DemandType
{
    KillQuickly,
    AcceptCorruption,
    ProtectShrine,
    NoHealing,
    PerfectCombat
}

[CreateAssetMenu(menuName = "TOF/Gods/God Demand")]
public class GodDemandSO : ScriptableObject
{
    [Header("Demand")]
    public DemandType demandType;

    [Header("Evaluation")]
    public bool requiresCombat = false;

    [Header("Consequences")]
    public int corruptionReward = 0;
    public int corruptionPenalty = 1;

    [Header("Keeper Weight")]
    public int keeperImpact = 1;
}
