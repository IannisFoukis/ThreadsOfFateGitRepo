using UnityEngine;

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


}
