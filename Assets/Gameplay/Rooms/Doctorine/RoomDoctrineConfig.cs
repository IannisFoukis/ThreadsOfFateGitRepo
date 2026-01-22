using UnityEngine;
using static EncounterCoordinator;

public enum DoctrineType
{
    Phalanx,
    Swarm,
    FlankAndFire,
    Ritual
}

[CreateAssetMenu(menuName = "Combat/Room Doctrine")]

public class RoomDoctrineConfig : ScriptableObject
{
    [Header("Identity")]
    public DoctrineType doctrineType;

    [Header("Slot Limits")]
    public int maxFrontSlots = 6;
    public int maxFlankSlots = 6;
    public bool allowRearSlots = true;

    [Header("Anchor Rules")]
    public bool hasAnchorObjective;
    public bool anchorHasPriority;

    [Header("Coordination Rules")]
    [Range(0f, 1f)]
    public float reassignmentAggression = 0.3f;

    [Tooltip("Minimum time before an enemy may request a new slot")]
    public float slotRequestCooldown = 1.5f;

    [Header("Spatial Layout")]
    public DoctrineLayout layout = new DoctrineLayout();

    public bool usesAnchor;

    [Header("Role Distances")]
    public float offenderDistance = 2.0f;
    public float defenderDistance = 3.5f;
    public float rangerDistance = 6.0f;
    public float activatorDistance = 8.0f;

   

    [Header("Phalanx Slot Limits")]
    public int maxOffenders = 5;
    public int maxDefenders = 3;
    public int maxRangers = 4;

    [Header("Phalanx Angles")]
    public float offenderArc = 140f;
    public float defenderArc = 120f;
    public float rangerArc = 180f;

    [Header("Role Radiuses")]
    public float offenderRadius = 2.5f;
    public float defenderRadius = 4.5f;
    public float rangerRadius = 6.5f;

    [Header("Slot Spacing")]
    public float slotSpacingMultiplier = 1.3f;

    [Header("Visual Identity")]
    public float leaderTurnSmooth = 4f;
    public bool useFacingRotation = true;

    [Header("Formation Style")]
    public bool allowAsymmetry = false;
    public float verticalBias = 0f;

    [Header("Behavior Emphasis")]
    public bool prioritizeRangers = false;
    public bool aggressiveOffenders = true;



}
