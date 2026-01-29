using UnityEngine;

public static class KeeperWorldState
{
    public static bool silenceEnforced;
    public static bool honestCombat;
    public static int pressureBias;
    public static int shrineHatred;

    public static void Reset()
    {
        silenceEnforced = false;
        honestCombat = false;
        pressureBias = 0;
        shrineHatred = 0;
    }
}
