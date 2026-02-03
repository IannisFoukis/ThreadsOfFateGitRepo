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

        Debug.Log("[RunDirector] Persistent.");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only reset guard AFTER a scene is loaded
        advancingRoom = false;
    }

    private void Start()
    {
        if (biomeConfig != null && biomeConfig.entries != null && biomeConfig.entries.Length > 0)
            biomeEntries = new List<BiomeConfig.RoomEntry>(biomeConfig.entries);
        else
            Debug.LogWarning("[RunDirector] BiomeConfig has no entries.");

        if (roomConfigController == null)
            roomConfigController = FindAnyObjectByType<RoomConfigController>(FindObjectsInactive.Include);

        Debug.Log("[RunDirector] Ready.");
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

        if (gsm == null)
        {
            Debug.LogError("[RunDirector] GameStateManager missing.");
            return;
        }

        gsm.StartNewRun();
        GameEvents.RaiseRunStart();

        EnterNextRoom();
    }

    public void EnterNextRoom()
    {
        if (advancingRoom) return;
        advancingRoom = true;

        if (gsm == null || gsm.RunState == null)
        {
            Debug.LogError("[RunDirector] RunState missing.");
            advancingRoom = false;
            return;
        }

        if (biomeEntries == null || biomeEntries.Count == 0)
        {
            Debug.LogWarning("[RunDirector] No biome entries. Ending run.");
            gsm.EndRun(RunEndReason.BiomeCompleted);
            return;
        }

        RunState run = gsm.RunState;

        if (run.currentRoomIndex >= biomeEntries.Count)
        {
            Debug.Log("[RunDirector] Biome completed.");
            gsm.EndRun(RunEndReason.BiomeCompleted);
            return;
        }

        int roomIndex = run.currentRoomIndex + 1;
        var entry = biomeEntries[run.currentRoomIndex];

        // ── BUILD ROOM CONTEXT ───────────────────
        RoomContext context = new RoomContext
        {
            roomIndex = roomIndex,
            roomRole = entry.role,
            chapterIndex = 0,
            chapterId = "Unknown"
        };

        if (biomeConfig != null && biomeConfig.progressionProfile != null)
        {
            if (biomeConfig.progressionProfile.TryGetChapter(roomIndex, out var chapter))
            {
                context.chapterIndex = chapter.chapterIndex;
                context.chapterId = chapter.chapterId;
            }
        }

        // ── APPLY ROOM RULES ─────────────────────
        if (roomConfigController != null)
            roomConfigController.ApplyRoomContext(context);
        else
            Debug.LogWarning("[RunDirector] RoomConfigController missing.");

        Debug.Log(
            $"[RunDirector] Loading Room {context.roomIndex} " +
            $"(Chapter {context.chapterIndex} – {context.chapterId}) " +
            $"Role={context.roomRole}"
        );

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
    // REQUIRED PUBLIC API
    // ─────────────────────────────
    public void OnRoomCompleted()
    {
        if (keeperTriggeredThisRun) return;
        EnterNextRoom();
    }

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

    public void ApplyRoomNPC(RoomNPC npc)
    {
        if (npc == null)
        {
            Debug.LogWarning("[RunDirector] ApplyRoomNPC called with null");
            return;
        }

        Debug.Log($"[RunDirector] ApplyRoomNPC → {npc.displayName}");

        switch (npc.effect)
        {
            case RoomNPC.EffectType.Corrupt:
                ApplyCorruption(npc.effectValue);
                break;

            case RoomNPC.EffectType.EnvironmentalInstability:
                if (RunContext.Instance?.rules == null)
                {
                    Debug.LogError("[RunDirector] RunRules missing.");
                    return;
                }

                RunContext.Instance.rules.environmentalInstability = true;
                Debug.Log("[RunRules] Environmental Instability ENABLED");
                break;

            default:
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