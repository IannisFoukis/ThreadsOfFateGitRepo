using UnityEngine;
using System.Collections;

public class EntryRoom : RoomController
{
    static bool entryCommitted = false;
    static float entryStartTime;

    [SerializeField] private KeeperPronouncement keeperPronouncement;

    protected override void Start()
    {
        base.Start();

        // Reset per Entry scene load
        entryCommitted = false;
        entryStartTime = Time.time;

        Debug.Log("[EntryRoom] Entry room started");

        // 🔒 Delay Keeper reaction until persistent UI is ready
        StartCoroutine(DelayedKeeperReaction());
    }

    private IEnumerator DelayedKeeperReaction()
    {
        yield return null; // wait 1 frame

        var mem = RunContext.Instance.memory;
        if (mem == null)
            yield break;

        if (mem.lastRunEndReason != RunEndReason.PlayerDied)
            yield break;

        if (keeperPronouncement == null)
        {
            keeperPronouncement = FindAnyObjectByType<KeeperPronouncement>(
                FindObjectsInactive.Include
            );
        }

        if (keeperPronouncement == null)
        {
            Debug.LogError("[EntryRoom] KeeperPronouncement not found (even inactive).");
            yield break;
        }

        string line = KeeperResolver.GetEntryReaction();
        if (!string.IsNullOrEmpty(line))
        {
            keeperPronouncement.Show(line);
        }

        mem.entryHesitated = false;
        mem.entryRushed = false;
    }


    public static void CommitEntry()
    {
        Debug.Log("[EntryRoom] CommitEntry called");

        if (entryCommitted)
            return;

        entryCommitted = true;

        float timeSpent = Time.time - entryStartTime;
        Debug.Log($"[EntryRoom] Time in entry room: {timeSpent:F2}s");

        if (RunContext.Instance != null && RunContext.Instance.memory != null)
        {
            if (timeSpent < 5f)
            {
                RunContext.Instance.memory.entryRushed = true;
                RunContext.Instance.memory.rushCount++;
                Debug.Log("[EntryRoom] Entry rushed");
            }
            else
            {
                RunContext.Instance.memory.entryHesitated = true;
                RunContext.Instance.memory.hesitationCount++;
                Debug.Log("[EntryRoom] Entry hesitated");
            }
        }
        else
        {
            Debug.LogError("[EntryRoom] RunContext or memory missing");
        }

        var runDirector = Object.FindAnyObjectByType<RunDirector>();
        if (runDirector == null)
        {
            Debug.LogError("[EntryRoom] RunDirector not found. Cannot begin run.");
            return;
        }

        runDirector.BeginRun();
    }
}
