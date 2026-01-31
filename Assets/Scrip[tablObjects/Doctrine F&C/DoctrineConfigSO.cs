using UnityEngine;

[CreateAssetMenu(
    fileName = "DoctrineConfig",
    menuName = "TOF/Doctrine/Doctrine Config"
)]
public class DoctrineConfigSO : ScriptableObject
{
    [Header("Attack Rhythm")]
    public float attackCooldownMult = 1f;
    public float windupDurationMult = 1f;

    [Header("Commitment")]
    [Range(0f, 1f)] public float attackCancelChance = 0f;
    [Range(0f, 1f)] public float targetPersistence = 1f;

    [Header("Formation")]
    public float formationStickinessMult = 1f;
    public float slotPatienceMult = 1f;

    [Header("Movement / Retreat")]
    [Range(0f, 1f)] public float retreatChance = 0f;
    public float repositionDelayMult = 1f;

    [Header("Stress (Pre-Break)")]
    public float stressGainMult = 1f;
    public float stressBreakThresholdMult = 1f;

    public enum DoctrineType { Fanatic, Chaotic }
    public DoctrineType doctrineType;


}