using UnityEngine;

public enum ShrineResolution
{
    Destroyed,
    Endured,
    Ignored
}

[CreateAssetMenu(menuName = "TOF/Shrines/Shrine Behavior")]
public class ShrineBehaviorSO : ScriptableObject
{
    [Header("Identity")]
    public string shrineId;

    [Header("Combat Effects")]
    public bool buffsEnemies = false;
    public bool spawnsReinforcements = false;
    public bool enforcesFormation = false;

    [Header("Meta Effects")]
    public int corruptionDeltaOnDestroy = 1;
    public int corruptionDeltaOnEndure = 0;

    [Header("God Reaction")]
    public int godApprovalWeight = 1;

    [Header("Keeper Memory")]
    public bool trackedByKeeper = true;
}
