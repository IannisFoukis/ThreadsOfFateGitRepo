public class RunMemory
{
    public int soulsCarried;
    public int corruption;

    public int breatherRestCount;
    public int breatherRefuseCount;

    public bool jokerKilled;

    public int shrineEnduredCount;
    public int shrineDestroyedCount;

    public bool entryRushed;
    public bool rushPenaltyConsumed;
    public bool pendingRushPressure;
    public bool entryHesitated;

    public RunEndReason lastRunEndReason;

    public int hesitationCount;
    public int rushCount;

    // ───── Silence Phase ─────
    public bool silenceActive;
    public int silenceRoomsRemaining;

}
