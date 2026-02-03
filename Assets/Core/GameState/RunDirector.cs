using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunDirector : MonoBehaviour
{
    private GameStateManager gsm;

    public DoctrineState ActiveDoctrine { get; private set; }

    [Header("Configs")]
    [SerializeField] private BiomeConfig biomeConfig;
    [SerializeField] private KeeperPronouncement keeperPronouncement;
    [SerializeField] private RoomConfigController roomConfigController;

    private bool keeperTriggeredThisRun = false;
    private bool runStarted = false;
    private bool advancingRoom = false;

    private List<BiomeConfig.RoomEntry> biomeEntries;
    public bool IsRunActive { get; private set; }

    public int CurrentRoomNumber =>
        gsm != null && gsm.RunState != null
            ? gsm.RunState.currentRoomIndex + 1
            : 0;

    // ─────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────
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
        advancingRoom = false;
    }

    private void Start()
    {
        if (biomeConfig != null && biomeConfig.entries.Length > 0)
            biomeEntries = new List<BiomeConfig.RoomEntry>(biomeConfig.entries);

        if (roomConfigController == null)
            roomConfigController = FindAnyObjectByType<RoomConfigController>(FindObjectsInactive.Include);

        Debug.Log("RunDirector ready.");
    }

    // ─────────────────────────────
    // RUN FLOW
    // ─────────────────────────────
    public void BeginRun()
    {
        if (runStarted) return;

        runStarted = true;
        IsRunActive = true;

        Debug.Log("=== NEW RUN STARTED ===");

        keeperPronouncement?.Clear();

        if (gsm == null)
            gsm = FindAnyObjectByType<GameStateManager>();

        gsm.StartNewRun();
        GameEvents.RaiseRunStart();

        EnterNextRoom();
    }

    public void EnterNextRoom()
    {
        if (advancingRoom) return;
        advancingRoom = true;

        if (biomeEntries == null || biomeEntries.Count == 0)
        {
            gsm.EndRun(RunEndReason.BiomeCompleted);
            return;
        }

        RunState run = gsm.RunState;

        if (run.currentRoomIndex >= biomeEntries.Count)
        {
            gsm.EndRun(RunEndReason.BiomeCompleted);
            return;
        }

        int roomNumber = run.currentRoomIndex + 1;

        // ✅ APPLY BIOME 1 CHAPTER RULES
        if (roomConfigController != null)
            roomConfigController.ApplyBiome1ChapterRules(roomNumber);

        var entry = biomeEntries[run.currentRoomIndex];
        run.currentRoomIndex++;

        SceneManager.LoadScene(GetSceneName(entry.role));
    }

    public void OnCombatRoomCleared()
    {
        StartCoroutine(AdvanceAfterCombat());
    }

    private System.Collections.IEnumerator AdvanceAfterCombat()
    {
        yield return new WaitForSeconds(0.5f);
        EnterNextRoom();
    }

    // ─────────────────────────────
    // 🔴 RESTORED API (REQUIRED)
    // ─────────────────────────────

    // Used by RoomDirector
    public void OnRoomCompleted()
    {
        if (keeperTriggeredThisRun) return;
        EnterNextRoom();
    }

    // Used by GameStateManager
    public void ResetRun()
    {
        Debug.Log("[RunDirector] ResetRun");

        IsRunActive = false;
        runStarted = false;
        keeperTriggeredThisRun = false;
        advancingRoom = false;

        Time.timeScale = 1f;

        if (keeperPronouncement == null)
            keeperPronouncement = FindAnyObjectByType<KeeperPronouncement>(FindObjectsInactive.Include);

        keeperPronouncement?.Clear();

        var mem = RunContext.Instance?.memory;
        if (mem != null)
        {
            mem.pendingRushPressure = false;
            mem.rushPenaltyConsumed = false;
            mem.entryRushed = false;
            mem.entryHesitated = false;
            mem.jokerKilled = false;
        }

        SceneManager.LoadScene(GetSceneName(RoomRole.Entry));
    }

    // Used by BreatherEffectApplier
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

    // ─────────────────────────────
    // HELPERS
    // ─────────────────────────────
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