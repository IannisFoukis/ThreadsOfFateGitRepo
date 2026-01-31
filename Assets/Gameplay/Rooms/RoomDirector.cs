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

        // 1️⃣ Resolve corruption (derived from Keeper / run state)
        corruption = CorruptionResolver.ResolveFromKeeper();

        
        // 🔗 Wire doctrine into EncounterCoordinator (read-only, if present)
        var encounter = FindFirstObjectByType<EncounterCoordinator>();
        if (encounter != null && doctrineState != null)
        {
            encounter.ApplyDoctrine(doctrineState);
        }
        // 2️⃣ Pull doctrine from RunDirector (SINGLE SOURCE OF TRUTH)
        var runDirector = FindFirstObjectByType<RunDirector>();
        if (runDirector == null || runDirector.ActiveDoctrine == null)
        {
            Debug.Log("[RoomDirector] No ActiveDoctrine yet (pre-Keeper). Skipping doctrine injection.");
            return;
        }

        // 3️⃣ Clone doctrine so the room can safely mutate it
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

        // 4️⃣ Apply corruption as a MODIFIER (never a creator)
        switch (corruption)
        {
            case CorruptionTier.Low:
                break;

            case CorruptionTier.Medium:
                doctrineState.formationDiscipline *= 0.9f;
                doctrineState.coordinationDelay *= 1.1f;
                break;

            case CorruptionTier.High:
                doctrineState.formationDiscipline *= 0.75f;
                doctrineState.coordinationDelay *= 1.25f;
                doctrineState.chaotic = true;
                break;
        }

        // 5️⃣ Init stress tracker (enemy count comes later)
        stressTracker = new DoctrineStressTracker();

        Debug.Log(
            $"[RoomDirector] Room started | Role={contract.roomRole} | Corruption={corruption}"
        );

        // 🔔 Let systems spawn enemies & subscribe
        GameEvents.RaiseRoomStart();

        // 🗣️ Emit initial doctrine intent
        EmitDoctrineSpeech(doctrineState);

        // 6️⃣ Hand doctrine to TacticDirector (read-only consumption)
        var tactic = FindFirstObjectByType<TacticDirector>();
        if (tactic != null)
        {
            tactic.ApplyKeeperDoctrine(doctrineState);
        }
    }

    // Called by EncounterCoordinator AFTER enemies spawn
    public void RegisterInitialEnemyCount(int totalEnemies)
    {
        stressTracker?.Init(totalEnemies);
    }

    // Called by EncounterCoordinator on every enemy death
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
            // Fanatics harden instead of scattering
            doctrineState.formationDiscipline += 0.3f;
            SpeechBus.Emit(EnemySpeechEvent.FanaticLock);
        }
        else
        {
            // Chaos explodes
            doctrineState.formationDiscipline = 0.2f;
            doctrineState.chaotic = true;
            SpeechBus.Emit(EnemySpeechEvent.FormationBreak);
        }
    }

    private void EmitDoctrineSpeech(DoctrineState state)
    {
        if (state == null)
            return;

        SpeechBus.Emit(EnemySpeechEvent.DoctrineEngaged);

        if (state.chaotic)
            SpeechBus.Emit(EnemySpeechEvent.FormationBreak);

        if (state.fanatic)
            SpeechBus.Emit(EnemySpeechEvent.FanaticLock);

        if (state.canSacrifice && !state.fanatic)
            SpeechBus.Emit(EnemySpeechEvent.SacrificeIntent);
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
    // ─────────────────────────────────────────────
    // SHRINE → DOCTRINE DISRUPTION
    // ─────────────────────────────────────────────
    public void ApplyShrineDisruption(int tier)
    {
        if (doctrineState == null)
            return;

        Debug.Log($"[RoomDirector] Shrine disruption applied (Tier {tier})");

        switch (tier)
        {
            case 1:
                // Temporary confusion
                doctrineState.formationDiscipline -= 0.2f;
                break;

            case 2:
                // Discipline erosion + chaos leak
                doctrineState.formationDiscipline -= 0.4f;
                doctrineState.chaotic = true;
                break;

            case 3:
                // Total collapse
                doctrineState.formationDiscipline = 0.1f;
                doctrineState.chaotic = true;
                doctrineState.canRetreat = false;
                doctrineState.canSacrifice = true;
                break;
        }

        doctrineState.formationDiscipline =
            Mathf.Clamp01(doctrineState.formationDiscipline);
    }
}