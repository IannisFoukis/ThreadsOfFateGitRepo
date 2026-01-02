public static class CombatModifiers
{
    public static int GlobalEnemyLifesteal = 0;
    public static bool RandomizeEnemyRoles = false;
    // When true, healing effects (shrines, items) are disabled for the current room/run
    public static bool DisableHealing = false;

    public static void Reset()
    {
        GlobalEnemyLifesteal = 0;
        RandomizeEnemyRoles = false;
    }
}
