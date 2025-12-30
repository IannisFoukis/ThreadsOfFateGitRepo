using System.Collections.Generic;
using UnityEngine;
using static Shrine;

public class CombatRoom : RoomController
{
    bool tensionSpike4Triggered;
    bool tensionSpike7Triggered;
    bool tensionSpike10Triggered;

    bool roomCompleted;

    GameStateManager gsm;

    readonly List<GameObject> spawnedEnemies = new();
    readonly List<GameObject> activeEnemies = new();

    [Header("Encounter")]
    public GameObject enemyPrefab;
    public EncounterType encounterType;
    public Shrine shrine;

    [Header("Mid-Fight Escalation")]
    [SerializeField] float escalationDelay = 8f;
    float escalationTimer;
    bool escalationTriggered;

    bool lockdownActive;

    protected override void Start()
    {
        base.Start();

        gsm = FindAnyObjectByType<GameStateManager>();

        if (gsm == null)
        {
            Debug.LogError("CombatRoom: GameStateManager not found");
            return;
        }

        ApplyCorruptedRoomRules();
        // Prepare shrine tier/visuals for room entry but DO NOT start hazards here.
        // The activator (ActivatorTest) should call shrine.ActivateByActivator() when triggered.
        if (shrine != null)
        {
            shrine.PrepareForRoom();
        }

        SpawnEncounter();
    }

    void ApplyCorruptedRoomRules()
    {
        int corruption = RunCorruptionState.Instance.Level;

        if (corruption >= 1)
        {
            Enemy.ForceImmediateAggro();
            Debug.Log("[ROOM] Corruption: Immediate enemy aggression");
        }

        if (corruption >= 2)
        {
            escalationDelay *= 0.5f;
            lockdownActive = true;
            Debug.Log("[ROOM] Corruption: Faster mid-fight escalation");
        }

        if (corruption >= 3 && shrine != null)
        {
            shrine.ForceHazards();
        }
    }

    void Update()
    {
        // Room completion logic
        if (Enemy.AliveCount <= 0 && !roomCompleted)
        {
            if (lockdownActive && EliteSpawner.Instance != null && EliteSpawner.Instance.EliteAlive)
            {
                Debug.Log("[ROOM] Lockdown: Elite still alive");
                return;
            }

            if (ChoiceManager.Instance != null &&
                ChoiceManager.Instance.ChoicePending)
                return;

            Debug.Log($"[RUN] Room cleared | roomsCleared BEFORE = {gsm.RunState.roomsCleared}");
            gsm.OnRoomCleared();
            Debug.Log($"[RUN] roomsCleared AFTER = {gsm.RunState.roomsCleared}");

            roomCompleted = true;
            // Notify listeners the room has completed
            GameEvents.RaiseRoomCompleted();
            CompleteRoom();
            return;
        }

        // Active combat logic
        if (!roomCompleted)
        {
            // Check tension spikes during combat
            CheckTensionSpikes();

            // Timed / corruption-based escalation (one-shot)
            escalationTimer += Time.deltaTime;

            if (!escalationTriggered &&
                (escalationTimer >= escalationDelay ||
                 RunCorruptionState.Instance.Level >= 2))
            {
                TriggerMidFightEscalation();
                escalationTriggered = true;
            }
        }
    }

    // ============================
    // ENCOUNTERS
    // ============================

    void SpawnEncounter()
    {
        switch (encounterType)
        {
            case EncounterType.Skirmish:
                Spawn(EnemyRole.Melee, 2);
                break;

            case EncounterType.Crossfire:
                Spawn(EnemyRole.Defender, 2);
                Spawn(EnemyRole.Melee, 1);
                break;

            case EncounterType.BurstThreat:
                Spawn(EnemyRole.Charger, 1);
                Spawn(EnemyRole.Melee, 1);
                break;

            case EncounterType.Mixed:
                Spawn(EnemyRole.Melee, 1);
                Spawn(EnemyRole.Defender, 1);
                Spawn(EnemyRole.Charger, 1);
                break;

            case EncounterType.Lockdown:
                Spawn(EnemyRole.Melee, 2);
                Spawn(EnemyRole.Defender, 2);
                Spawn(EnemyRole.Charger, 1);
                break;
        }

        TryAssignJoker();
        TrySpawnElite();
    }

    void Spawn(EnemyRole baseRole, int count)
    {
        for (int i = 0; i < count; i++)
        {
            EnemyRole finalRole = GetCorruptedRole(baseRole);

            Vector2 offset = Random.insideUnitCircle * 3f;
            GameObject enemy = Instantiate(
                enemyPrefab,
                transform.position + (Vector3)offset,
                Quaternion.identity
            );

            ConfigureEnemy(enemy, finalRole);
            // Notify listeners that an enemy was spawned
            GameEvents.RaiseEnemySpawned(enemy);
            ApplyEnemyScaling(enemy);
            ApplyShrineScaling(enemy);
            ApplyBehaviorEscalation(enemy);
            ApplyAbilityEscalation(enemy);
            ApplyEnemyEscalation(enemy, false);

            spawnedEnemies.Add(enemy);
            activeEnemies.Add(enemy);
        }
    }

    // ============================
    // ROLE / CONFIG
    // ============================

    void ConfigureEnemy(GameObject enemy, EnemyRole role)
    {
        // ✅ ASSIGN ROLE (THIS WAS MISSING)
        var roleController = enemy.GetComponent<EnemyRoleController>();
        if (roleController != null)
            roleController.role = role;

        var melee = enemy.GetComponent<EnemyMelee>();
        var ranged = enemy.GetComponent<EnemyRanged>();
        var charger = enemy.GetComponent<EnemyCharger>();
        var chase = enemy.GetComponent<EnemyChase>();

        // Disable all movement scripts first
        if (melee) melee.enabled = false;
        if (ranged) ranged.enabled = false;
        if (charger) charger.enabled = false;
        if (chase) chase.enabled = false;

        // Ensure Enemy.ApplyRole is applied immediately so other components don't rely on Start ordering
        var enemyComp = enemy.GetComponent<Enemy>();
        if (enemyComp != null)
        {
            enemyComp.ApplyRole(role);
        }

        switch (role)
        {
            case EnemyRole.Offender:
            case EnemyRole.Melee:
                if (melee) melee.enabled = true;
                break;

            case EnemyRole.Ranged:
            case EnemyRole.Defender:
                if (ranged) ranged.enabled = true;
                break;

            case EnemyRole.Charger:
            case EnemyRole.Elite:
                if (charger) charger.enabled = true;
                break;

            case EnemyRole.Support:
            case EnemyRole.Flanker:
                if (chase) chase.enabled = true;
                break;

            case EnemyRole.Activator:
                // ✅ Activator MUST MOVE
                if (chase) chase.enabled = true;
                // If there's an ActivatorRunner, assign the room's shrine so it moves to the correct target
                var activatorRunner = enemy.GetComponent<ActivatorRunner>();
                if (activatorRunner != null && shrine != null)
                    activatorRunner.SetShrine(shrine);
                break;
        }
    }



    EnemyRole GetCorruptedRole(EnemyRole baseRole)
    {
        int corruption = RunCorruptionState.Instance.Level;

        if (corruption >= 3 && Random.value < 0.4f)
            return EnemyRole.Charger;

        if (corruption >= 2 && baseRole == EnemyRole.Melee)
            return EnemyRole.Charger;

        if (corruption >= 1 && Random.value < 0.3f)
            return EnemyRole.Ranged;

        return baseRole;
    }

    // ============================
    // SCALING
    // ============================

    void ApplyEnemyScaling(GameObject enemy)
    {
        if (gsm == null || gsm.RunState == null)
            return;

        RunState run = gsm.RunState;

        var health = enemy.GetComponent<Health>();
        var damage = enemy.GetComponent<DamageOnContact>();

        if (health != null)
        {
            int before = health.maxHealth;
            float hpMult = 1f + run.roomsCleared * 0.15f;
            health.ScaleMaxHealth(hpMult);

            Debug.Log($"[SCALING] HP {before} → {health.maxHealth} (x{hpMult:F2})");
        }

        if (damage != null)
        {
            float dmgMult = 1f + run.runTension * 0.10f;
            damage.damageMultiplier = dmgMult;

            Debug.Log($"[SCALING] DMG x{dmgMult:F2}");
        }
    }

    void ApplyShrineScaling(GameObject enemy)
    {
        // BUG FIX: don't search arbitrary shrine in scene; use room's shrine only
        if (shrine == null) return;

        var health = enemy.GetComponent<Health>();
        var damage = enemy.GetComponent<DamageOnContact>();

        if (health != null)
            health.ScaleMaxHealth(shrine.GetEnemyHpMultiplier());

        if (damage != null)
            damage.damageMultiplier *= shrine.GetEnemyDamageMultiplier();

        Debug.Log("[SHRINE] Enemy scaled");
    }

    // ============================
    // BEHAVIOR ESCALATION
    // ============================

    void ApplyBehaviorEscalation(GameObject enemy)
    {
        var controller = enemy.GetComponent<EnemyBehaviorController>();
        if (controller == null)
        {
            Debug.Log("[BEHAVIOR] No EnemyBehaviorController found");
            return;
        }

        Shrine activeShrine = shrine;
        RunState run = gsm.RunState;

        EnemyBehaviorTier tier = EnemyBehaviorTier.Base;

        if ((activeShrine != null && activeShrine.currentTier == ShrineTier.Tier3) ||
            run.runTension >= 7)
            tier = EnemyBehaviorTier.Elite;
        else if ((activeShrine != null && activeShrine.currentTier == ShrineTier.Tier2) ||
                 run.runTension >= 4)
            tier = EnemyBehaviorTier.Aggressive;

        controller.ApplyTier(tier);

        if (RunCorruptionState.Instance.IsCorrupted)
            controller.ApplyCorruption(RunCorruptionState.Instance.Level);

        Debug.Log($"[BEHAVIOR] Tier={controller.currentTier} | Corruption={RunCorruptionState.Instance.Level}");
    }

    void TriggerMidFightEscalation()
    {
        Debug.Log("[ESCALATION] Mid-fight escalation");

        // Notify listeners about mid-fight escalation
        GameEvents.RaiseMidFightEscalation();

        Shrine activeShrine = shrine;
        ShrineTier shrineTier = activeShrine != null ? activeShrine.currentTier : ShrineTier.Tier1;
        RunState run = gsm.RunState;

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            var behavior = enemy.GetComponent<EnemyBehaviorController>();
            var abilities = enemy.GetComponent<EnemyAbilityController>();

            if (behavior != null)
            {
                if (behavior.currentTier == EnemyBehaviorTier.Base)
                    behavior.ApplyTier(EnemyBehaviorTier.Aggressive);
                else
                    behavior.ApplyTier(EnemyBehaviorTier.Elite);
            }

            if (abilities != null)
            {
                ApplyEnemyEscalation(enemy, true);
            }
        }
    }

    // ============================
    // JOKER / ELITE
    // ============================

    void TryAssignJoker()
    {
        if (spawnedEnemies.Count == 0) return;
        if (JokerManager.Instance == null) return;
        if (Random.value > 0.5f) return;

        GameObject enemy = spawnedEnemies[Random.Range(0, spawnedEnemies.Count)];
        JokerManager.Instance.AssignJoker(enemy);
        Debug.Log("JOKER ASSIGNED → " + enemy.name);
    }

    void TrySpawnElite()
    {
        if (EliteSpawner.Instance == null) return;

        RunState run = gsm.RunState;

        if (run.roomsCleared % 3 == 0 || run.runTension >= 5 || run.corruption >= 4)
        {
            EliteSpawner.Instance.SpawnElite();
            Debug.Log("[ELITE] Pressure applied");
        }
    }

    void CheckTensionSpikes()
    {
        if (gsm == null || gsm.RunState == null)
            return;

        RunState run = gsm.RunState;

        if (run.runTension >= 4 && !tensionSpike4Triggered)
        {
            tensionSpike4Triggered = true;
            Debug.Log("[TENSION] Spike 4 → Global Aggro");
            Enemy.ForceImmediateAggro(2.5f);
        }

        if (run.runTension >= 7 && !tensionSpike7Triggered)
        {
            tensionSpike7Triggered = true;
            Debug.Log("[TENSION] Spike 7 → Aggressive Panic");
            Enemy.ForceImmediateAggro(4f);
        }

        if (run.runTension >= 10 && !tensionSpike10Triggered)
        {
            tensionSpike10Triggered = true;
            Debug.Log("[TENSION] Spike 10 → FULL PANIC");
            Enemy.ForceImmediateAggro(6f);
        }

        // BUG FIX: removed per-frame restarting of shrine attacks here
    }

    // ============================
    // ABILITIES
    // ============================

    void ApplyAbilityEscalation(GameObject enemy)
    {
        var abilities = enemy.GetComponent<EnemyAbilityController>();
        if (abilities == null) return;

        var roleController = enemy.GetComponent<EnemyRoleController>();
        if (roleController == null) return;

        Shrine activeShrine = shrine;
        ShrineTier shrineTier = activeShrine != null ? activeShrine.currentTier : ShrineTier.Tier1;

        RunState run = gsm.RunState;

        abilities.ApplyEscalation(
            roleController.role,
            shrineTier,
            run.runTension,
            midFight: false
        );
    }

    void ApplyEnemyEscalation(GameObject enemy, bool midFight)
    {
        var ability = enemy.GetComponent<EnemyAbilityController>();
        if (ability == null) return;

        var roleController = enemy.GetComponent<EnemyRoleController>();
        if (roleController == null)
        {
            Debug.LogWarning("[ABILITY] EnemyRoleController missing");
            return;
        }

        Shrine activeShrine = shrine;
        RunState run = gsm.RunState;

        ability.ApplyEscalation(
            roleController.role,
            activeShrine != null ? activeShrine.currentTier : ShrineTier.Tier1,
            run.runTension,
            midFight
        );
    }
}
