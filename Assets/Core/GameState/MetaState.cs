// ==================================================
// SCRIPT ROLE: DEFINES
// SYSTEM: Core
// RESPONSIBILITY: Holds persistent meta progression
// ==================================================

[System.Serializable]
public class MetaState
{
    public int runsCompleted;
    public int godDisposition; // -1 hostile, 0 neutral, 1 favorable
    public bool keeperAwareOfCorruption;
}
