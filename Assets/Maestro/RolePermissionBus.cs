using UnityEngine;

public static class RolePermissionBus
{
    private static bool allowOffenders = true;
    private static bool allowDefenders = true;
    private static bool allowRangers = true;

    public static void SetPermissions(
        bool offenders,
        bool defenders,
        bool rangers)
    {
        allowOffenders = offenders;
        allowDefenders = defenders;
        allowRangers = rangers;

        Debug.Log(
            $"[RolePermissionBus] Permissions set → " +
            $"O:{allowOffenders} D:{allowDefenders} R:{allowRangers}"
        );
    }

    public static bool IsRoleAllowed(EnemyRole role)
    {
        return role switch
        {
            EnemyRole.Offender => allowOffenders,
            EnemyRole.Defender => allowDefenders,
            EnemyRole.Ranger => allowRangers,
            _ => true
        };
    }
}