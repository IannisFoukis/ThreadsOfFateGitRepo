// CombatRoomContext.cs
public struct CombatRoomContext
{
    // Link to your existing room identity
    public RoomContext roomContext;

    // --- Dynamic combat signals ---
    public float timeInRoom;           // seconds since combat start
    public float enemiesAliveRatio;     // 0..1
    public float formationIntegrity;    // 0..1 (phalanx / ring cohesion)
    public float playerPressure;        // 0..1 (how boxed-in player is)

    // Shrine & player state
    public bool shrinePresent;
    public bool shrineActive;
    public bool playerLowHealth;

    // Run / escalation
    public int difficultyTier;
}