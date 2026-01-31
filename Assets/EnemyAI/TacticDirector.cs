using System.Collections.Generic;
using UnityEngine;

public class TacticDirector : MonoBehaviour
{
    private RunDirector runDirector;
    private bool keeperDoctrineApplied = false;

    // Multipliers driven ONLY by Keeper DoctrineState (run-wide)
    [SerializeField] private float keeperFormationMult = 1f;
    [SerializeField] private float keeperCooldownMult = 1f;
    [SerializeField] private float keeperFlankBiasMult = 1f;
    [SerializeField] private float keeperFrontBiasMult = 1f;
    [SerializeField] private float keeperCommitDelayMult = 1f;
    private readonly List<Enemy> activeEnemies = new();

    [Header("Timing")]
    public float tacticCooldown = 3f;
    public float flankCooldown = 2f;

    [Header("Slot Layout")]
    public float flankDistance = 3f;
    public float frontDistance = 2.5f;

    private Transform player;
    private PlayerBehaviorTracker tracker;

    private float cooldownTimer;
    private float nextFlankTime;
    private bool flankActive;

    [Header("Base Tactical Values")]
    [SerializeField] private float baseSlotCooldown = 1.5f;
    [SerializeField] private float baseFormationPersistence = 1.0f;
    [SerializeField] private float baseInitialCommitDelay = 0.5f;
    [SerializeField] private float baseFlankWeight = 1.0f;
    [SerializeField] private float baseFrontWeight = 1.0f;

    [Header("Doctrine Bias Multipliers (Debug)")]
    [SerializeField] private float formationPersistenceMult = 1f;
    [SerializeField] private float slotCooldownMult = 1f;
    [SerializeField] private float flankBiasMult = 1f;
    [SerializeField] private float frontPriorityMult = 1f;
    [SerializeField] private float initialCommitDelayMult = 1f;

    [Header("Shrine Interference")]
    [SerializeField] private bool formationDisrupted = false;
    [SerializeField] private float disruptionTimer = 0f;

    [Header("Slot Scramble")]
    [SerializeField] private bool slotScrambleActive = false;
    [SerializeField] private float scrambleTimer = 0f;
    [SerializeField] private float scrambleInterval = 0.8f;
    [SerializeField] private float scrambleTickTimer = 0f;

    [Header("Tier 3 Collapse")]
    [SerializeField] private bool tier3Active = false;
    [SerializeField] private float tier3Timer = 0f;
    [SerializeField] private int scrambleBreaksPerTick = 1;

    [Header("Tactical Lockout")]
    [SerializeField] private bool tacticalLockout = false;
    public bool IsTacticalLockoutActive => tacticalLockout;


    private void Awake()
    {
        FindPlayer();
        runDirector = FindAnyObjectByType<RunDirector>(); // ✅ read-only source
    }

    private void Update()
    {
        // 🔒 Tier-3 Tactical Lockout
        if (tacticalLockout)
        {
            tier3Timer -= Time.deltaTime;

            if (tier3Timer <= 0f)
            {
                tacticalLockout = false;
                tier3Active = false;

                Debug.Log("[TacticDirector] Tactical control restored (Tier-3 ended)");
            }

            return; // ⛔ absolutely no tactical logic runs
        }


        if (tier3Active)
        {
            tier3Timer -= Time.deltaTime;

            if (tier3Timer <= 0f)
            {
                tier3Active = false;
                Debug.Log("[TacticDirector] Tier-3 collapse ended");
            }

            return; // ⛔ No formations, no flanks, no tactics
        }


        if (player == null || tracker == null)
        {
            FindPlayer();
            return;
        }
        if (player == null || tracker == null)
        {
            FindPlayer();
            return;
        }
        // ✅ Apply Keeper doctrine ONCE per run (read-only)
        if (!keeperDoctrineApplied && runDirector != null && runDirector.ActiveDoctrine != null)
        {
            ApplyKeeperDoctrine(runDirector.ActiveDoctrine);
            keeperDoctrineApplied = true;
        }
        if (formationDisrupted)
        {
            disruptionTimer -= Time.deltaTime;

            if (disruptionTimer <= 0f)
            {
                formationDisrupted = false;
                Debug.Log("[TacticDirector] Formation control restored");
            }

            return; // 🔒 Block all tactic logic while disrupted
        }
        if (slotScrambleActive)
        {
            scrambleTimer -= Time.deltaTime;
            scrambleTickTimer -= Time.deltaTime;

            if (scrambleTickTimer <= 0f)
            {
                for (int i = 0; i < scrambleBreaksPerTick; i++)
                    ScrambleOneSlot();

                scrambleTickTimer = scrambleInterval;
            }


            if (scrambleTimer <= 0f)
            {
                slotScrambleActive = false;
                Debug.Log("[TacticDirector] Slot scramble ended");
            }
        }



        // 🔍 AUTO-END FLANK WHEN FORMATION BREAKS
        if (flankActive && !AnyEnemyHasSlot())
        {
            Debug.Log("[TacticDirector] Flank ended (all slots broken)");
            EndFlank();
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (flankActive || Time.time < nextFlankTime)
            return;

        TryStartFlank();
    }


    private void LateUpdate()
    {
        if (!player || !tracker) return;

        Vector2 p = player.position;

        Vector2 forward = tracker.LastMoveDir.sqrMagnitude < 0.01f
            ? Vector2.right
            : tracker.LastMoveDir.normalized;

        Vector2 perp = new(-forward.y, forward.x);

        foreach (var e in activeEnemies)
        {
            if (!e) continue;

            var slot = e.GetComponent<EnemySlotLock>();
            if (slot == null || !slot.HasSlot) continue;

            switch (slot.CurrentSlot)
            {
                case TacticSlotType.LeftFlank:
                    slot.UpdateSlotPosition(p - perp * flankDistance);
                    break;

                case TacticSlotType.RightFlank:
                    slot.UpdateSlotPosition(p + perp * flankDistance);
                    break;

                case TacticSlotType.Front:
                    slot.UpdateSlotPosition(p + forward * frontDistance);
                    break;
            }
        }
    }
    public void ConfigureDoctrine(RoomDoctrine doctrine)
    {
        Debug.Log($"[TacticDirector] Applying doctrine: {doctrine}");

        switch (doctrine)
        {
            case RoomDoctrine.Phalanx:
                formationPersistenceMult = 1.6f;
                slotCooldownMult = 1.4f;
                flankBiasMult = 0.5f;
                frontPriorityMult = 1.8f;
                initialCommitDelayMult = 0.7f;
                break;

            case RoomDoctrine.Swarm:
                formationPersistenceMult = 0.6f;
                slotCooldownMult = 0.4f;
                flankBiasMult = 1.7f;
                frontPriorityMult = 0.5f;
                initialCommitDelayMult = 0.4f;
                break;

            case RoomDoctrine.Hunter:
                formationPersistenceMult = 1.0f;
                slotCooldownMult = 1.8f;
                flankBiasMult = 1.0f;
                frontPriorityMult = 1.0f;
                initialCommitDelayMult = 1.8f;
                break;

            case RoomDoctrine.Guardian:
                formationPersistenceMult = 1.4f;
                slotCooldownMult = 1.0f;
                flankBiasMult = 0.6f;
                frontPriorityMult = 2.0f;
                initialCommitDelayMult = 1.0f;
                break;
        }
        // Layer Keeper doctrine on top of RoomDoctrine (run-wide pressure)
        formationPersistenceMult *= keeperFormationMult;
        flankBiasMult *= keeperFlankBiasMult;
        frontPriorityMult *= keeperFrontBiasMult;
        initialCommitDelayMult *= keeperCommitDelayMult;

    }
    public void ApplyKeeperDoctrine(DoctrineState ds)
    {
        if (ds == null) return;

        // Formation stability comes from Keeper discipline
        keeperFormationMult = Mathf.Clamp(ds.formationDiscipline, 0.35f, 1.8f);

        // CoordinationDelay: >1 means slower coordination, <1 means tighter
        keeperCooldownMult = Mathf.Clamp(ds.coordinationDelay, 0.6f, 1.6f);

        // AggressionMultiplier biases flank vs front
        float aggr = Mathf.Clamp(ds.aggressionMultiplier, 0.6f, 1.6f);

        if (ds.chaotic)
        {
            // Chaos: flanks more likely, front less committed
            keeperFlankBiasMult = 1.25f * aggr;
            keeperFrontBiasMult = 0.85f;
            keeperCommitDelayMult = 0.85f;
        }
        else
        {
            // Order/Bind: front priority and persistence
            keeperFlankBiasMult = 0.85f;
            keeperFrontBiasMult = 1.15f / Mathf.Max(0.8f, aggr);
            keeperCommitDelayMult = 1.05f;
        }

        Debug.Log(
            $"[TacticDirector] Keeper doctrine applied " +
            $"(disc={ds.formationDiscipline:0.00}, chaotic={ds.chaotic}, aggr={ds.aggressionMultiplier:0.00}, coord={ds.coordinationDelay:0.00})"
        );
    }
    private void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (!go) return;

        player = go.transform;
        tracker = go.GetComponent<PlayerBehaviorTracker>();
    }
    private void ScrambleOneSlot()
    {
        var slotted = new List<EnemySlotLock>();

        foreach (var e in activeEnemies)
        {
            if (!e) continue;

            var slot = e.GetComponent<EnemySlotLock>();
            if (slot != null && slot.HasSlot)
                slotted.Add(slot);
        }

        if (slotted.Count == 0)
            return;

        var chosen = slotted[Random.Range(0, slotted.Count)];

        // Elite resistance
        var roleCtrl = chosen.GetComponent<EnemyRoleController>();
        if (roleCtrl != null && roleCtrl.CurrentRole == EnemyRole.Elite)
        {
            if (Random.value < 0.5f)
            {
                Debug.Log("[TacticDirector] Elite resisted scramble");
                return;
            }
        }

        Debug.Log("[TacticDirector] Scrambling slot");
        chosen.ReleaseSlot(breakForce: 999f);
    }

    private void TryStartFlank()
    {
        if (tacticalLockout)
        {
            Debug.Log("[TacticDirector] Flank attempt blocked — tactical lockout active");
            return;
        }

        if (formationDisrupted)
        {
            Debug.Log("[TacticDirector] Flank blocked — formation jammed");
            return;
        }

        List<Enemy> free = new();

        foreach (var e in activeEnemies)
        {
            if (!e) continue;

            var slot = e.GetComponent<EnemySlotLock>();
            if (slot == null) continue;

            // we want enemies WITHOUT slots
            if (!slot.HasSlot)
                free.Add(e);
        }


        if (free.Count < 3)
            return;

        free.Sort((a, b) =>
            (a.transform.position - player.position).sqrMagnitude
            .CompareTo((b.transform.position - player.position).sqrMagnitude));

        Vector2 p = player.position;

        Vector2 forward = tracker.LastMoveDir.sqrMagnitude < 0.01f
            ? Vector2.right
            : tracker.LastMoveDir.normalized;

        Vector2 perp = new(-forward.y, forward.x);

        AssignSlot(free, TacticSlotType.LeftFlank, p - perp * flankDistance);
        AssignSlot(free, TacticSlotType.RightFlank, p + perp * flankDistance);
        AssignSlot(free, TacticSlotType.Front, p + forward * frontDistance);

        if (tacticalLockout)
        {
            Debug.Log("[TacticDirector] Slot assignment blocked — tactical lockout active");
            return;
        }

        if (!AnyEnemyHasSlot())
            return;

        flankActive = true;
        cooldownTimer = tacticCooldown * keeperCooldownMult;
        Debug.Log("[TacticDirector] Flank started");
    }

    public void EndFlank()
    {
        flankActive = false;
        nextFlankTime = Time.time + flankCooldown * keeperCooldownMult;
        foreach (var e in activeEnemies)
            e?.GetComponent<EnemySlotLock>()?.ReleaseSlot();

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            var e = activeEnemies[i];
            if (!e)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            var slot = e.GetComponent<EnemySlotLock>();
            if (slot && slot.HasSlot)
                slot.ReleaseSlot();
        }

    }

    public void Register(Enemy enemy)
    {
        if (enemy != null && !activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void Unregister(Enemy enemy)
    {
        if (enemy != null)
            activeEnemies.Remove(enemy);
    }

    private void OnDrawGizmos()
    {
        if (!player || !tracker) return;

        Vector2 p = player.position;

        Vector2 forward = tracker.LastMoveDir.sqrMagnitude < 0.01f
            ? Vector2.right
            : tracker.LastMoveDir.normalized;

        Vector2 perp = new(-forward.y, forward.x);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(p - perp * flankDistance, Vector3.one * 0.4f);
        Gizmos.DrawWireCube(p + perp * flankDistance, Vector3.one * 0.4f);
        Gizmos.DrawWireCube(p + forward * frontDistance, Vector3.one * 0.4f);
    }
    private bool AnyEnemyHasSlot()
    {
        foreach (var e in activeEnemies)
        {
            if (!e) continue;

            var slot = e.GetComponent<EnemySlotLock>();
            if (slot != null && slot.HasSlot)
                return true;
        }
        return false;
    }
    private void AssignSlot(List<Enemy> enemies, TacticSlotType slot, Vector2 pos)
    {
        foreach (var e in enemies)
        {
            var lockComp = e.GetComponent<EnemySlotLock>();
            if (lockComp == null) continue;

            if (!lockComp.HasSlot && lockComp.CanTakeSlot(slot))
            {
                lockComp.LockSlot(slot, pos);
                return;
            }
        }
    }
    public void ApplyFormationJam(float duration)
    {
        formationDisrupted = true;
        disruptionTimer = duration;

        Debug.Log($"[TacticDirector] FORMATION JAM — {duration:0.0}s");

        // Optional but recommended: immediately destabilize formation
        BreakAllSlots();
    }
    private void BreakAllSlots()
    {
        foreach (var e in activeEnemies)
        {
            if (!e) continue;

            var slot = e.GetComponent<EnemySlotLock>();
            if (slot != null && slot.HasSlot)
            {
                slot.ReleaseSlot(breakForce: 999f);
            }
        }

        flankActive = false;
        Debug.Log("[TacticDirector] Formation forcibly broken");
    }
    public void ApplySlotScramble(float duration, int breaksPerTick)
    {
        slotScrambleActive = true;
        scrambleTimer = duration;
        scrambleTickTimer = scrambleInterval;
        scrambleBreaksPerTick = breaksPerTick;

        Debug.Log(
        $"[TacticDirector] SLOT SCRAMBLE — {duration:0.0}s | breaks/tick = {breaksPerTick}"
    );
    }
    public void ApplyTier3Collapse(float jamDuration, float scrambleDuration)
    {
        tier3Active = true;
        tacticalLockout = true;

        tier3Timer = Mathf.Max(jamDuration, scrambleDuration);

        Debug.Log("[TacticDirector] TIER 3 COLLAPSE — TACTICAL LOCKOUT ACTIVE");

        ApplyFormationJam(jamDuration);
        ApplySlotScramble(scrambleDuration, 2);

        BreakAllSlots();
    }


}
