using UnityEngine;
using TOF.Rooms.Contracts;
using TOF.Core.Corruption;

public class RoomDirector : MonoBehaviour
{
    [Header("Room Contract")]
    public RoomContract contract;

    public CorruptionTier corruption { get; private set; }
    public DoctrineState doctrineState { get; private set; }

    private DoctrineStressTracker stressTracker;
    private bool doctrineBroken = false;
    private bool completionSent = false;

    private void Start()
    {
        if (contract == null)
        {
            Debug.LogError($"[RoomDirector] RoomContract missing in scene {gameObject.scene.name}");
            return;
        }

        // 1) Resolve corruption (from Keeper / run state)
        corruption = CorruptionResolver.ResolveFromKeeper();

        // 2) Pull doctrine from RunDirector (single source of truth)
        var runDirector = FindFirstObjectByType<RunDirector>();
        if (runDirector == null || runDirector.ActiveDoctrine == null)
        {
            Debug.Log("[RoomDirector] No ActiveDoctrine yet (pre-Keeper). Skipping doctrine injection.");
            GameEvents.RaiseRoomStart(); // still allow spawn systems
            return;
        }

        // 3) Clone doctrine so the room can safely mutate it
        doctrineState = new DoctrineState
        {
            canRetreat = runDirector.ActiveDoctrine.canRetreat,
            canSacrifice = runDirector.ActiveDoctrine.canSacrifice,
            formationDiscipline = runDirector.ActiveDoctrine.formationDiscipline,
            fanatic = runDirector.ActiveDoctrine.fanatic,
            chaotic = runDirector.ActiveDoctrine.chaotic,
            aggressionMultiplier = runDirector.ActiveDoctrine.aggressionMultiplier,
            coordinationDelay = runDirector.ActiveDoctrine.coordinationDelay
        };

        // 4) Apply corruption as a modifier
        switch (corruption)
        {
            case CorruptionTier.Medium:
                doctrineState.formationDiscipline *= 0.9f;
                doctrineState.coordinationDelay *= 1.1f;
                break;

            case CorruptionTier.High:
                doctrineState.formationDiscipline *= 0.75f;
                doctrineState.coordinationDelay *= 1.25f;
                doctrineState.chaotic = true;
                break;

            case CorruptionTier.Extreme:
                doctrineState.formationDiscipline *= 1.15f;
                doctrineState.coordinationDelay *= 0.9f;
                doctrineState.fanatic = true;
                break;
        }

        // 5) Init stress tracker (enemy count comes later)
        stressTracker = new DoctrineStressTracker();

        Debug.Log($"[RoomDirector] Room started | Role={contract.roomRole} | Corruption={corruption}");

        // Let systems spawn enemies & subscribe
        GameEvents.RaiseRoomStart();

        // 6) Inject doctrine into EncounterCoordinator (Phase L/M)
        var encounter = FindFirstObjectByType<EncounterCoordinator>();
        if (encounter != null)
            encounter.ApplyDoctrine(doctrineState);

        // Optional: TacticDirector can consume run-wide doctrine too
        var tactic = FindFirstObjectByType<TacticDirector>();
        if (tactic != null)
            tactic.ApplyKeeperDoctrine(doctrineState);

        // 7) Room start speech
        EmitDoctrineSpeech(doctrineState);
    }

    public void RegisterInitialEnemyCount(int totalEnemies)
    {
        stressTracker?.Init(totalEnemies);
    }

    public void NotifyEnemyKilled()
    {
        if (doctrineBroken || stressTracker == null)
            return;

        if (stressTracker.RegisterDeath())
        {
            doctrineBroken = true;
            BreakDoctrine();
        }
    }

    private void BreakDoctrine()
    {
        if (doctrineState == null)
            return;

        if (doctrineState.fanatic)
        {
            doctrineState.formationDiscipline += 0.3f;
            SpeechBus.Emit(EnemySpeechEvent.FanaticLock);
        }
        else
        {
            doctrineState.formationDiscipline = 0.2f;
            doctrineState.chaotic = true;
            SpeechBus.Emit(EnemySpeechEvent.FormationBreak);
        }
    }

    private void EmitDoctrineSpeech(DoctrineState state)
    {
        if (state == null) return;

        SpeechBus.Emit(EnemySpeechEvent.DoctrineEngaged);

        if (state.chaotic)
            SpeechBus.Emit(EnemySpeechEvent.FormationBreak);

        if (state.fanatic)
            SpeechBus.Emit(EnemySpeechEvent.FanaticLock);

        // NOTE: SacrificeIntent removed (not in enum)
    }

    public void NotifyRoomCompleted()
    {
        if (completionSent)
            return;

        completionSent = true;

        Debug.Log("[RoomDirector] Room completed");
        GameEvents.RaiseRoomCompleted();

        var runDirector = FindFirstObjectByType<RunDirector>();
        if (runDirector != null)
            runDirector.OnRoomCompleted();
    }

    public void ApplyShrineDisruption(int tier)
    {
        if (doctrineState == null)
            return;

        Debug.Log($"[RoomDirector] Shrine disruption applied (Tier {tier})");

        switch (tier)
        {
            case 1:
                doctrineState.formationDiscipline -= 0.2f;
                break;
            case 2:
                doctrineState.formationDiscipline -= 0.4f;
                doctrineState.chaotic = true;
                break;
            case 3:
                doctrineState.formationDiscipline = 0.1f;
                doctrineState.chaotic = true;
                doctrineState.canRetreat = false;
                doctrineState.canSacrifice = true;
                break;
        }

        doctrineState.formationDiscipline = Mathf.Clamp01(doctrineState.formationDiscipline);
    }
}