using UnityEngine;
using System.Collections;

public class EntryRoom : RoomController
{
    static bool entryCommitted = false;
    static float entryStartTime;

    [SerializeField] private KeeperPronouncement keeperPronouncement;
    [SerializeField] private AudioSource entryAudio;

    protected override void Start()
    {
        base.Start();

        // Reset per Entry scene load
        entryCommitted = false;
        entryStartTime = Time.time;

        Debug.Log("[EntryRoom] Entry room started");

        // Camera settle (pure polish)
        StartCoroutine(SettleCamera());

        // Subtle audio cue
        if (entryAudio != null)
            entryAudio.Play();

        // One-line Keeper reaction AFTER death only
        StartCoroutine(DelayedKeeperReaction());
    }

    private IEnumerator DelayedKeeperReaction()
    {
        // Wait one frame so persistent UI has time to exist
        yield return null;

        var context = RunContext.Instance;
        if (context == null || context.memory == null)
            yield break;

        // Only react if previous run ended in death
        if (context.memory.lastRunEndReason != RunEndReason.PlayerDied)
            yield break;

        if (keeperPronouncement == null)
        {
            keeperPronouncement = FindAnyObjectByType<KeeperPronouncement>(
                FindObjectsInactive.Include
            );
        }

        if (keeperPronouncement == null)
        {
            Debug.LogError("[EntryRoom] KeeperPronouncement not found.");
            yield break;
        }

        string line = KeeperResolver.GetEntryReaction();
        if (!string.IsNullOrEmpty(line))
        {
            keeperPronouncement.Show(line);
        }
    }

    public static void CommitEntry()
    {
        if (entryCommitted)
            return;

        entryCommitted = true;

        float timeSpent = Time.time - entryStartTime;
        Debug.Log($"[EntryRoom] Time in entry room: {timeSpent:F2}s");

        var mem = RunContext.Instance?.memory;
        if (mem != null)
        {
            if (timeSpent < 5f)
            {
                mem.entryRushed = true;
                Debug.Log("[EntryRoom] Entry rushed");
            }
            else
            {
                mem.entryHesitated = true;
                Debug.Log("[EntryRoom] Entry hesitated");
            }
        }

        var runDirector = FindAnyObjectByType<RunDirector>();
        if (runDirector == null)
        {
            Debug.LogError("[EntryRoom] RunDirector not found.");
            return;
        }

        runDirector.BeginRun();
    }

    private IEnumerator SettleCamera()
    {
        var cam = Camera.main;
        if (cam == null)
            yield break;

        Vector3 target = cam.transform.position;
        Vector3 start = target + Vector3.up * 0.5f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 0.8f;
            cam.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
    }
}
