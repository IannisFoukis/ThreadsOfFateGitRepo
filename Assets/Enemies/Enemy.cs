using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public static int AliveCount = 0;

    [Header("Aggro State")]
    public bool isAggro;
    public bool isAggressive;
    public bool ignoreAssistLogic;

    [Header("Scaling")]
    public float damageMultiplier = 1f;

    public Transform Target { get; private set; }

    // Optional global config loaded from Resources/EnemyConfig.asset
    static EnemyConfig s_config;

    void Awake()
    {
        // Apply role-driven behaviour as early as possible so other components' Start methods
        // see the correct enabled/disabled state.
        var roleController = GetComponent<EnemyRoleController>();
        if (roleController != null)
        {
            ApplyRole(roleController.role);
        }
        else
        {
            // Warn if no role controller is present — designer may have forgotten to assign a role on prefab
            Debug.LogWarning($"[ENEMY] No EnemyRoleController on {name}. Default behaviour components will remain as-is.");
        }
    }

    void Start()
    {
        Target = FindAnyObjectByType<PlayerController>()?.transform;
    }
    private void OnEnable()
    {
        AliveCount++;
    }

    // Enable/disable role-specific behaviour components based on role
    public void ApplyRole(EnemyRole role)
    {
        // Lazy-load config
        if (s_config == null)
            s_config = Resources.Load<EnemyConfig>("EnemyConfig");

        var entry = s_config != null ? s_config.GetEntry(role) : null;

        var melee = GetComponent<EnemyMelee>();
        var ranged = GetComponent<EnemyRanged>();
        var charger = GetComponent<EnemyCharger>();
        var chase = GetComponent<EnemyChase>();
        var ability = GetComponent<EnemyAbilityController>();
        var behavior = GetComponent<EnemyBehaviorController>();
        var activator = GetComponent<ActivatorRunner>();

        // Default mappings (recommended):
        // Melee / Offender / Flanker -> melee (and chase if present)
        bool wantMelee = (role == EnemyRole.Melee || role == EnemyRole.Offender || role == EnemyRole.Flanker);

        // Ranged -> ranged
        bool wantRanged = (role == EnemyRole.Ranged);

        // Charger -> charger
        bool wantCharger = (role == EnemyRole.Charger);

        // Activator -> activator runner
        bool wantActivator = (role == EnemyRole.Activator);

        // Ability-using roles
        bool wantAbility = (role == EnemyRole.Offender || role == EnemyRole.Defender || role == EnemyRole.Support || role == EnemyRole.Elite);

        // Movement chase for some roles (support/defender/support should follow/move)
        bool wantChase = (role == EnemyRole.Offender || role == EnemyRole.Defender || role == EnemyRole.Support || role == EnemyRole.Flanker);

        // If we have a config entry, use it to decide enables, otherwise use defaults
        if (entry != null)
        {
            if (melee != null) melee.enabled = entry.enableMelee;
            if (chase != null) chase.enabled = entry.enableChase;
            if (ranged != null) ranged.enabled = entry.enableRanged;
            if (charger != null) charger.enabled = entry.enableCharger;
            if (activator != null) activator.enabled = entry.enableActivator;

            if (ability != null) ability.enabled = entry.enableAbility;

            damageMultiplier = entry.damageMultiplier;
            if (behavior != null)
                behavior.ApplyTier(entry.defaultBehaviorTier);
        }
        else
        {
            if (melee != null) melee.enabled = wantMelee;
            if (chase != null) chase.enabled = wantChase;
            if (ranged != null) ranged.enabled = wantRanged;
            if (charger != null) charger.enabled = wantCharger;
            if (activator != null) activator.enabled = wantActivator;

            if (ability != null) ability.enabled = wantAbility;
        }

        // Behavior controller should typically stay enabled so rooms can call ApplyTier/ApplyCorruption.
        if (behavior != null)
        {
            behavior.enabled = true;
            if (role == EnemyRole.Elite)
                behavior.ApplyTier(EnemyBehaviorTier.Elite);
        }

        // keep role value in the RoleController if present
        var rc = GetComponent<EnemyRoleController>();
        if (rc != null) rc.role = role;

        // Diagnostics: warn if some commonly required components are missing
        if (GetComponent<EnemyStateController>() == null)
            Debug.LogWarning($"[ENEMY] {name} is missing EnemyStateController — behaviors expect it.");
        if (GetComponent<Rigidbody2D>() == null)
            Debug.LogWarning($"[ENEMY] {name} is missing Rigidbody2D — movement behaviours expect it.");
    }

    // Helper to change role at runtime
    public void SetRole(EnemyRole role)
    {
        ApplyRole(role);
    }

    private void OnDisable()
    {
        AliveCount--;
    }

    // GLOBAL forced aggro (used by tension spikes / elites)
    public static void ForceImmediateAggro(float duration = 3f)
    {
        foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemy.ForceAggro(duration);
        }
    }

    // Instant aggro (permanent until changed by AI)
    public virtual void ForceAggro()
    {
        isAggro = true;
        isAggressive = true;
    }

    // Timed aggro escalation
    public void ForceAggro(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(ForceAggroRoutine(duration));
    }

    private IEnumerator ForceAggroRoutine(float duration)
    {
        Debug.Log($"[ENEMY] Forced aggro on {name} for {duration}s");

        isAggro = true;
        isAggressive = true;
        ignoreAssistLogic = true;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isAggressive = false;
        ignoreAssistLogic = false;
    }
}
