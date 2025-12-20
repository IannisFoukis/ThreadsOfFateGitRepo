using System;

[Serializable]
public class RunData
{
    public int roomsCleared;
    public int corruption;
    public int runSeed;
    public int tension;

    public void Reset()
    {
        corruption = 0;
        roomsCleared = 0;
        runSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }
}
