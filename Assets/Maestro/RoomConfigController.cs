using UnityEngine;

public class RoomConfigController : MonoBehaviour
{
    [Header("TACTICAL AUTHORITY")]
    public TacticalLevel tacticalLevel = TacticalLevel.Instinct;

    [Header("FORMATION")]
    public FormationType initialFormation = FormationType.Swarm;

    [Header("PRESSURE PROFILE")]
    public PressureProfile pressureProfile = PressureProfile.Medium;

    [Header("ROLE PERMISSIONS (INTENT ONLY)")]
    public bool allowOffenders = true;
    public bool allowDefenders = false;
    public bool allowRangers = false;

    private TacticalAuthority tacticalAuthority;
    private FormationResolver formationResolver;
    private EncounterCoordinator encounterCoordinator;

    void Awake()
    {
        ResolveDependencies();
        ApplyConfig();
    }

    public void Apply()
    {
        ResolveDependencies();
        ApplyConfig();
    }

    // ✅ BIOME 1 AUTHORITY
    public void ApplyBiome1ChapterRules(int roomNumber)
    {
        if (roomNumber <= 10)
        {
            // Chapter I — Coast
            tacticalLevel = TacticalLevel.Instinct;
            initialFormation = FormationType.Swarm;
            pressureProfile = PressureProfile.Slow;

            allowOffenders = true;
            allowDefenders = false;
            allowRangers = false;
        }
        else if (roomNumber <= 20)
        {
            // Chapter II — Watch
            tacticalLevel = TacticalLevel.Coordinated;
            initialFormation = FormationType.Swarm;
            pressureProfile = PressureProfile.Medium;

            allowOffenders = true;
            allowDefenders = true;
            allowRangers = true;
        }
        else if (roomNumber <= 30)
        {
            // Chapter III — Fortress
            tacticalLevel = TacticalLevel.Coordinated;
            initialFormation = FormationType.Phalanx;
            pressureProfile = PressureProfile.Fast;

            allowOffenders = true;
            allowDefenders = true;
            allowRangers = true;
        }
        else
        {
            // Chapter IV — Shrine War
            tacticalLevel = TacticalLevel.Coordinated;
            initialFormation = FormationType.Phalanx;
            pressureProfile = PressureProfile.Adaptive;

            allowOffenders = true;
            allowDefenders = true;
            allowRangers = true;
        }

        Apply();
    }

    void ResolveDependencies()
    {
        tacticalAuthority = FindFirstObjectByType<TacticalAuthority>();
        formationResolver = FindFirstObjectByType<FormationResolver>();
        encounterCoordinator = FindFirstObjectByType<EncounterCoordinator>();
    }

    void ApplyConfig()
    {
        ApplyTacticalLevel();
        ApplyFormation();
        ApplyPressureProfile();
        ApplyRolePermissions();

        Debug.Log(
            $"[RoomConfig] Applied → TAL={tacticalLevel}, " +
            $"Formation={initialFormation}, Pressure={pressureProfile}, " +
            $"Roles(O/D/R)=({allowOffenders}/{allowDefenders}/{allowRangers})"
        );
    }

    void ApplyTacticalLevel()
    {
        tacticalAuthority?.SetLevel(tacticalLevel);
    }

    void ApplyFormation()
    {
        formationResolver?.SetFormation(initialFormation);
    }

    void ApplyPressureProfile()
    {
        if (encounterCoordinator == null) return;

        switch (pressureProfile)
        {
            case PressureProfile.Slow:
                encounterCoordinator.holdCompression = 1.1f;
                encounterCoordinator.encircleCompression = 0.75f;
                break;

            case PressureProfile.Medium:
                encounterCoordinator.holdCompression = 0.7f;
                encounterCoordinator.encircleCompression = 0.45f;
                break;

            case PressureProfile.Fast:
                encounterCoordinator.holdCompression = 0.45f;
                encounterCoordinator.encircleCompression = 0.25f;
                break;

            case PressureProfile.Adaptive:
                // Reserved for shrine-heavy rooms
                break;
        }
    }

    void ApplyRolePermissions()
    {
        RolePermissionBus.SetPermissions(
            allowOffenders,
            allowDefenders,
            allowRangers
        );
    }
}