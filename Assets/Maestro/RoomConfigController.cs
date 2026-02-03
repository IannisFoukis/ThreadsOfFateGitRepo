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

    // ─────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────
    private void Awake()
    {
        ResolveDependencies();
        ApplyConfig();
    }

    // Manual re-apply hook (used by RunDirector)
    public void Apply()
    {
        ResolveDependencies();
        ApplyConfig();
    }

    // ─────────────────────────────
    // AUTHORITATIVE ENTRY POINT
    // ─────────────────────────────
    public void ApplyRoomContext(RoomContext context)
    {
        // Chapter-based interpretation (Biome-driven)
        switch (context.chapterIndex)
        {
            case 1: // Coast
                tacticalLevel = TacticalLevel.Instinct;
                initialFormation = FormationType.Swarm;
                pressureProfile = PressureProfile.Slow;

                allowOffenders = true;
                allowDefenders = false;
                allowRangers = false;
                break;

            case 2: // Watch
                tacticalLevel = TacticalLevel.Coordinated;
                initialFormation = FormationType.Swarm;
                pressureProfile = PressureProfile.Medium;

                allowOffenders = true;
                allowDefenders = true;
                allowRangers = true;
                break;

            case 3: // Fortress
                tacticalLevel = TacticalLevel.Coordinated;
                initialFormation = FormationType.Phalanx;
                pressureProfile = PressureProfile.Fast;

                allowOffenders = true;
                allowDefenders = true;
                allowRangers = true;
                break;

            default: // Late Biome / Shrine War / Fallback
                tacticalLevel = TacticalLevel.Coordinated;
                initialFormation = FormationType.Phalanx;
                pressureProfile = PressureProfile.Adaptive;

                allowOffenders = true;
                allowDefenders = true;
                allowRangers = true;
                break;
        }

        Debug.Log(
            $"[RoomConfig] Context Applied → " +
            $"Room={context.roomIndex}, Chapter={context.chapterIndex} ({context.chapterId}), " +
            $"Role={context.roomRole}"
        );

        Apply();
    }

    // ─────────────────────────────
    // INTERNAL WIRING
    // ─────────────────────────────
    private void ResolveDependencies()
    {
        tacticalAuthority = FindFirstObjectByType<TacticalAuthority>();
        formationResolver = FindFirstObjectByType<FormationResolver>();
        encounterCoordinator = FindFirstObjectByType<EncounterCoordinator>();
    }

    private void ApplyConfig()
    {
        ApplyTacticalLevel();
        ApplyFormation();
        ApplyPressureProfile();
        ApplyRolePermissions();

        Debug.Log(
            $"[RoomConfig] Applied → " +
            $"TAL={tacticalLevel}, Formation={initialFormation}, Pressure={pressureProfile}, " +
            $"Roles(O/D/R)=({allowOffenders}/{allowDefenders}/{allowRangers})"
        );
    }

    private void ApplyTacticalLevel()
    {
        tacticalAuthority?.SetLevel(tacticalLevel);
    }

    private void ApplyFormation()
    {
        formationResolver?.SetFormation(initialFormation);
    }

    private void ApplyPressureProfile()
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
                // Reserved for shrine-heavy / reactive rooms
                break;
        }
    }

    private void ApplyRolePermissions()
    {
        RolePermissionBus.SetPermissions(
            allowOffenders,
            allowDefenders,
            allowRangers
        );
    }
}