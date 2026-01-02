// ==================================================
// SCRIPT ROLE: DEFINES
// SYSTEM: Core
// RESPONSIBILITY: Holds runtime-only run data
// ==================================================

public class RunState
{
    public int currentRoomIndex;
    public int runTension;
    public int roomsCleared;
    public int corruption;
    public RunState()
    {
        currentRoomIndex = 0;
        runTension = 0;
    }

    public void Reset()
    {
        currentRoomIndex = 0;
        runTension = 0;
        roomsCleared = 0;
        corruption = 0;
    }
}
