using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ==================================================
// SCRIPT ROLE: CONTROLS FLOW
// SYSTEM: Run
// RESPONSIBILITY: Advances the run room by room
// ==================================================

public enum RoomRole
{
    Entry,
    Combat,
    Breather,
    Combat1,
    PressureSpike,
    Combat2,
    Boss
}

public class RunDirector : MonoBehaviour
{
    private GameStateManager gsm;
    private List<RoomRole> demoRun;

    [Header("Data")]
    [SerializeField] BiomeConfig biomeConfig;

    [Header("UI")]
    [SerializeField] KeeperPronouncement keeperPronouncement;

    private bool keeperTriggeredThisRun = false;
    private bool runStarted = false;

    // internal convenience list built from biomeConfig or fallback demoRun
    private List<BiomeConfig.RoomEntry> biomeEntries;
    public bool IsRunActive { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        gsm = FindAnyObjectByType<GameStateManager>();
        if (gsm == null)
            Debug.LogError("RunDirector: GameStateManager not found!");

        Debug.Log("RunDirector persistent.");
    }

    private void Start()
    {
        demoRun = new List<RoomRole>
        {
            RoomRole.Entry,
            RoomRole.Combat,
            RoomRole.Breather,
            RoomRole.Combat1,
            RoomRole.PressureSpike,
            RoomRole.Combat2,
            RoomRole.Boss
        };

        Debug.Log("RunDirector ready.");

        // If a biomeConfig is assigned, build the internal biome entries list
        if (biomeConfig != null && biomeConfig.entries != null && biomeConfig.entries.Length > 0)
        {
            biomeEntries = new List<BiomeConfig.RoomEntry>(biomeConfig.entries);
            Debug.Log($"RunDirector: Loaded biome config with {biomeEntries.Count} entries");
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (Time.timeScale != 0f || !keeperTriggeredThisRun) return;
            Debug.Log("[KeeperInput] Choice 1: BindSouls");
            KeeperResolver.ApplyChoice(KeeperChoice.BindSouls);
            EndKeeperMoment();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (Time.timeScale != 0f || !keeperTriggeredThisRun) return;
            Debug.Log("[KeeperInput] Choice 2: EnforceOrder");
            KeeperResolver.ApplyChoice(KeeperChoice.EnforceOrder);
            EndKeeperMoment();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (Time.timeScale != 0f || !keeperTriggeredThisRun) return;
            Debug.Log("[KeeperInput] Choice 3: AccelerateChaos");
            KeeperResolver.ApplyChoice(KeeperChoice.AccelerateChaos);
            EndKeeperMoment();
        }
#endif
    }

    public void BeginRun()
    {
        IsRunActive = true;

        if (runStarted)
            return;

        runStarted = true;

        Debug.Log("=== NEW RUN STARTED ===");

        // Clear lingering Keeper text (UIRoot is persistent)
        if (keeperPronouncement != null)
            keeperPronouncement.Clear();

        // Resolve GameStateManager safely
        if (gsm == null)
            gsm = FindAnyObjectByType<GameStateManager>();

        if (gsm == null)
        {
            Debug.LogError("RunDirector: GameStateManager missing; cannot begin run.");
            return;
        }

        gsm.StartNewRun();

        GameEvents.RaiseRunStart();

        EnterNextRoom();
    }

    public void EnterNextRoom()
    {
        if (gsm == null)
        {
            Debug.LogError("RunDirector: GameStateManager missing");
            return;
        }

        RunState run = gsm.RunState;
        RoomRole chosenRole;

        // === BIOME FLOW ===
        if (biomeEntries != null)
        {
            while (run.currentRoomIndex < biomeEntries.Count)
            {
                var entry = biomeEntries[run.currentRoomIndex];
                chosenRole = entry.role;

                // Skip Entry in biome flow (Entry is handled by Bootstrap + gate)
                if (chosenRole == RoomRole.Entry)
                {
                    Debug.LogWarning("RunDirector: Skipping Entry room in biome flow");
                    run.currentRoomIndex++;
                    continue;
                }

                Debug.Log($"Loading biome room {run.currentRoomIndex}: {chosenRole}");
                ApplyTension(chosenRole);
                run.currentRoomIndex++;

                GameEvents.RaiseRoomStart();

                // Rush pressure one-shot
                var mem = RunContext.Instance?.memory;
                if (mem != null &&
                    !mem.rushPenaltyConsumed &&
                    mem.entryRushed &&
                    (chosenRole == RoomRole.Combat || chosenRole == RoomRole.Combat1))
                {
                    Debug.Log("[RunPressure] Entry rush penalty armed for this room");
                    mem.pendingRushPressure = true;
                    mem.rushPenaltyConsumed = true;
                }

                SceneManager.LoadScene(GetSceneName(chosenRole));
                return;
            }

            gsm.EndRun(RunEndReason.BiomeCompleted);
            return;
        }

        // === DEMO FLOW ===
        while (run.currentRoomIndex < demoRun.Count)
        {
            chosenRole = demoRun[run.currentRoomIndex];

            if (chosenRole == RoomRole.Entry)
            {
                Debug.LogWarning("RunDirector: Skipping Entry room in demo flow");
                run.currentRoomIndex++;
                continue;
            }

            Debug.Log($"Loading room {run.currentRoomIndex}: {chosenRole}");
            ApplyTension(chosenRole);
            run.currentRoomIndex++;

            GameEvents.RaiseRoomStart();

            var mem = RunContext.Instance?.memory;
            if (mem != null &&
                !mem.rushPenaltyConsumed &&
                mem.entryRushed &&
                (chosenRole == RoomRole.Combat || chosenRole == RoomRole.Combat1))
            {
                Debug.Log("[RunPressure] Entry rush penalty armed for this room");
                mem.pendingRushPressure = true;
                mem.rushPenaltyConsumed = true;
            }

            SceneManager.LoadScene(GetSceneName(chosenRole));
            return;
        }

        gsm.EndRun(RunEndReason.BiomeCompleted);
    }

    private string GetSceneName(RoomRole role)
    {
        return role switch
        {
            RoomRole.Entry => "Room_Entry",
            RoomRole.Combat => "Room_Combat",
            RoomRole.Combat1 => "Room_Combat1",
            RoomRole.Combat2 => "Room_Combat2",
            RoomRole.Breather => "Room_Breather",
            RoomRole.PressureSpike => "Room_PressureSpike",
            RoomRole.Boss => "Room_Boss",
            _ => "Room_Entry"
        };
    }

    private void ApplyTension(RoomRole role)
    {
        if (gsm == null)
        {
            Debug.LogError("RunDirector: GameStateManager missing");
            return;
        }

        RunState run = gsm.RunState;

        if (role == RoomRole.Combat || role == RoomRole.Combat1)
            run.runTension += 5;

        if (role == RoomRole.PressureSpike)
            run.runTension += 3;

        Debug.Log("Run tension: " + run.runTension);
    }

    public void OnRoomCompleted()
    {
        CheckForKeeperTrigger();

        if (!keeperTriggeredThisRun)
            EnterNextRoom();
    }

    public void ApplyRoomNPC(RoomNPC npc)
    {
        Debug.Log($"[RunDirector] ApplyRoomNPC called with {npc.displayName}");

        switch (npc.effect)
        {
            case RoomNPC.EffectType.Corrupt:
                Debug.Log("[RunDirector] Applying CORRUPTION");
                ApplyCorruption(npc.effectValue);
                break;

            case RoomNPC.EffectType.EnvironmentalInstability:
                if (RunContext.Instance?.rules == null)
                {
                    Debug.LogError("[RunDirector] RunRules missing; cannot apply Environmental Instability");
                    return;
                }
                RunContext.Instance.rules.environmentalInstability = true;
                Debug.Log("[RunRules] Environmental Instability ENABLED");
                break;

            case RoomNPC.EffectType.None:
            default:
                Debug.Log("[RunDirector] No effect applied");
                break;
        }
    }

    private void ApplyCorruption(int amount)
    {
        // Prefer singleton if you have it, but never hard-crash if it’s not set.
        var god = GodDirector.Instance != null ? GodDirector.Instance : FindAnyObjectByType<GodDirector>();
        if (god == null)
        {
            Debug.LogError("[RunDirector] GodDirector not found.");
            return;
        }

        god.OnCorruptionAccepted(amount);
    }

    private void EndKeeperMoment()
    {
        Time.timeScale = 1f;

        if (keeperPronouncement != null)
            keeperPronouncement.Clear();

        keeperTriggeredThisRun = false;

        Debug.Log("[Keeper] Judgment sealed, run continues");

        EnterNextRoom();
    }

    private void CheckForKeeperTrigger()
    {
        if (keeperTriggeredThisRun)
            return;

        if (RunContext.Instance == null)
        {
            Debug.LogError("[Keeper] RunContext missing during CheckForKeeperTrigger");
            return;
        }

        if (RunContext.Instance.memory == null)
        {
            Debug.LogError("[Keeper] RunMemory missing during CheckForKeeperTrigger");
            return;
        }

        if (RunContext.Instance.memory.jokerKilled)
            TriggerKeeperFromJoker();
    }

    private void TriggerKeeperFromJoker()
    {
        keeperTriggeredThisRun = true;

        // consume the cause
        RunContext.Instance.memory.jokerKilled = false;

        if (keeperPronouncement == null)
        {
            Debug.LogError("[Keeper] keeperPronouncement is not assigned on RunDirector. Cannot show keeper moment; continuing run.");
            keeperTriggeredThisRun = false;
            EnterNextRoom();
            return;
        }

        Time.timeScale = 0f;

        keeperPronouncement.Show(
            "You silenced the question.\n\n" +
            "1. Bind Souls\n" +
            "2. Enforce Order\n" +
            "3. Accelerate Chaos"
        );

        Debug.Log("[Keeper] Triggered by Joker death");
    }

    public void ResetRun()
    {
        IsRunActive = false;

        Debug.Log("[RunDirector] ResetRun");

        // Always unpause in case we died during/after a Keeper moment
        Time.timeScale = 1f;

        // Reset run flags
        runStarted = false;
        keeperTriggeredThisRun = false;

        // Clear lingering Keeper UI (UIRoot is persistent)
        if (keeperPronouncement == null)
        {
            keeperPronouncement = FindAnyObjectByType<KeeperPronouncement>(FindObjectsInactive.Include);
        }
        keeperPronouncement?.Clear();

        // Reset Keeper one-shot logic
        KeeperResolver.ResetForNewRun();

        // Clear one-shot / per-run memory flags (keep long-term counters if you want)
        var mem = RunContext.Instance?.memory;
        if (mem != null)
        {
            mem.pendingRushPressure = false;
            mem.rushPenaltyConsumed = false;

            mem.entryRushed = false;
            mem.entryHesitated = false;

            mem.jokerKilled = false;
        }

        // IMPORTANT: load the correct Entry scene name
        SceneManager.LoadScene(GetSceneName(RoomRole.Entry)); // -> "Room_Entry"
    }
    public void OnCombatRoomCleared()
    {
        Debug.Log("[RunDirector] Combat room cleared");

        // Delay here is SAFE — RunDirector is persistent
        StartCoroutine(AdvanceAfterCombat());
    }

    public int CurrentTension => gsm != null ? gsm.RunState.runTension : 5;

    public int CurrentCorruption
    {
        get
        {
            if (RunCorruptionState.Instance == null)
                return 0;

            return RunCorruptionState.Instance.CorruptionLevel;
        }
    }




    private System.Collections.IEnumerator AdvanceAfterCombat()
    {
        yield return new WaitForSeconds(0.5f);
        EnterNextRoom();
    }

}
