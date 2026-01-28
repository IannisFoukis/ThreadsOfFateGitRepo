using UnityEngine;

[CreateAssetMenu(menuName = "TOF/Combat/Encounter Doctrine")]
public class EncounterDoctrineSO : ScriptableObject
{
    [Header("Formation Permissions")]
    public bool allowPhalanx = true;
    public bool allowFlank = true;
    public bool allowEncircle = false;

    [Header("Role Ratios")]
    [Range(0f, 1f)] public float meleeRatio = 0.5f;
    [Range(0f, 1f)] public float defenderRatio = 0.3f;
    [Range(0f, 1f)] public float rangedRatio = 0.2f;

    [Header("Leadership")]
    public bool hasLeader = true;
    public bool collapseOnLeaderDeath = true;

    [Header("Behavior Modifiers")]
    public bool enforceHonestAttacks = false;
    public bool enforceSilence = false;
}
