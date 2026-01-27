using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ✅ RESTORED ENUM (THIS WAS MISSING)
public enum RoomRole
{
    Entry,
    Combat,
    Breather,
    Combat1,
    PressureSpike,
    Combat2,
    Boss,
    Keeper
}

public class RunDirector : MonoBehaviour
{
    private GameStateManager gsm;
    private List<RoomRole> demoRun;

    [SerializeField] BiomeConfig biomeConfig;
    [SerializeField] KeeperPronouncement keeperPronouncement;

    private bool keeperTriggeredThisRun = false;
    private bool runStarted = false;
    private bool advancingRoom = false; // 🔒 HARD GUARD

    private List<BiomeConfig.RoomEntry> biomeEntries;
    public bool IsRunActive { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        gsm = FindAnyObjectByType<GameStateManager>();
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("RunDirector persistent.");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        advancingRoom = false; // 🔓 reset ONLY here
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

        if (biomeConfig != null && biomeConfig.entries.Length > 0)
            biomeEntries = new List<BiomeConfig.RoomEntry>(biomeConfig.entries);

        Debug.Log("RunDirector ready.");
    }

    public void BeginRun()
    {
        if (runStarted)
            return;

        runStarted = true;
        IsRunActive = true;

        Debug.Log("=== NEW RUN STARTED ===");

        keeperPronouncement?.Clear();

        if (gsm == null)
            gsm = FindAnyObjectByType<GameStateManager>();

        if (gsm == null)
        {
            Debug.LogError("RunDirector: GameStateManager missing.");
            return;
        }

        gsm.StartNewRun();
        GameEvents.RaiseRunStart();

        EnterNextRoom();
    }

    public void EnterNextRoom()
    {
        if (advancingRoom)
        {
            Debug.LogWarning("[RunDirector] EnterNextRoom blocked (already advancing)");
            return;
        }

        advancingRoom = true;

        if (gsm == null)
        {
            Debug.LogError("RunDirector: GameStateManager missing");
            return;
        }

        RunState run = gsm.RunState;

        if (biomeEntries != null)
        {
            while (run.currentRoomIndex < biomeEntries.Count)
            {
                var entry = biomeEntries[run.currentRoomIndex];
                var chosenRole = entry.role;

                if (chosenRole == RoomRole.Entry)
                {
                    run.currentRoomIndex++;
                    continue;
                }

                Debug.Log($"Loading biome room {run.currentRoomIndex}: {chosenRole}");
                ApplyTension(chosenRole);
                run.currentRoomIndex++;

                SceneManager.LoadScene(GetSceneName(chosenRole));
                return;
            }

            SceneManager.LoadScene(GetSceneName(RoomRole.Keeper));
            return;
        }

        gsm.EndRun(RunEndReason.BiomeCompleted);
    }

    public void OnRoomCompleted()
    {
        if (keeperTriggeredThisRun)
            return;

        EnterNextRoom();
    }

    public void OnCombatRoomCleared()
    {
        Debug.Log("[RunDirector] Combat room cleared");
        StartCoroutine(AdvanceAfterCombat());
    }

    private System.Collections.IEnumerator AdvanceAfterCombat()
    {
        yield return new WaitForSeconds(0.5f);
        EnterNextRoom();
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
            RoomRole.Keeper => "Room_Keeper",
            _ => "Room_Entry"
        };
    }

    private void ApplyTension(RoomRole role)
    {
        if (role == RoomRole.Combat || role == RoomRole.Combat1)
            gsm.RunState.runTension += 5;

        if (role == RoomRole.PressureSpike)
            gsm.RunState.runTension += 3;

        Debug.Log("Run tension: " + gsm.RunState.runTension);
    }
    public void ResetRun()
    {
        Debug.Log("[RunDirector] ResetRun");

        IsRunActive = false;
        runStarted = false;
        keeperTriggeredThisRun = false;
        advancingRoom = false;

        // Always unpause (safety)
        Time.timeScale = 1f;

        // Clear Keeper UI if present
        if (keeperPronouncement == null)
            keeperPronouncement = FindAnyObjectByType<KeeperPronouncement>(FindObjectsInactive.Include);

        keeperPronouncement?.Clear();

        // Reset per-run memory safely
        var mem = RunContext.Instance?.memory;
        if (mem != null)
        {
            mem.pendingRushPressure = false;
            mem.rushPenaltyConsumed = false;
            mem.entryRushed = false;
            mem.entryHesitated = false;
            mem.jokerKilled = false;
        }

        // Return to Entry
        SceneManager.LoadScene(GetSceneName(RoomRole.Entry));
    }
    public void ApplyRoomNPC(RoomNPC npc)
    {
        if (npc == null)
        {
            Debug.LogWarning("[RunDirector] ApplyRoomNPC called with null");
            return;
        }

        Debug.Log($"[RunDirector] ApplyRoomNPC called with {npc.displayName}");

        switch (npc.effect)
        {
            case RoomNPC.EffectType.Corrupt:
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
        var god = GodDirector.Instance != null
            ? GodDirector.Instance
            : FindAnyObjectByType<GodDirector>();

        if (god == null)
        {
            Debug.LogError("[RunDirector] GodDirector not found.");
            return;
        }

        god.OnCorruptionAccepted(amount);
    }

}
