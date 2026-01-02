using System;

[Serializable]
public class RunData
{
    public int roomsCleared;
    public int corruption;
    public int runSeed;
    public int tension;
    // New persistent run counters
    public int deaths;
    public int biomesCompleted;
    public int currentBiomeIndex;

    public void Reset()
    {
        // Reset per-run values only. Do not clear meta/progression counters.
        corruption = 0;
        roomsCleared = 0;
        runSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }
}
