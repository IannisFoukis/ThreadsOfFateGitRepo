using UnityEngine;

public enum ChapterId
{
    Coast,      // Rooms 1-10
    Watch,      // Rooms 11-20
    Fortress,   // Rooms 21-30
    ShrineWar   // Rooms 31-40
}

public enum FormationLevel
{
    None,
    Loose,
    Phalanx,
    Reactive
}

public enum DashImpactMode
{
    None,
    LightDisrupt,
    FormationBreak,
    HighRiskHighReward
}

public enum FailurePolicy
{
    RestartRoom,
    RestartRoom_WithMinorPersistence,
    RestartRoom_WithEscalation,
    RestartRoom_WithMutation
}

[System.Serializable]
public struct RoomRuleSet
{
    public ChapterId chapter;

    public bool allowRoleSynergy;
    public bool allowFormations;
    public FormationLevel formationLevel;

    public bool shrinePassive;
    public bool shrineBuffs;
    public bool shrineReactive;
    public bool shrineActiveCombatant;

    public bool allowJoker;

    public DashImpactMode dashImpactMode;
    public FailurePolicy failurePolicy;
}