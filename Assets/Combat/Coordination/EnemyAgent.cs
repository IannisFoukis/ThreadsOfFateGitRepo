using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    public EnemySlotType? assignedSlot;
    public EncounterCoordinator coordinator;

    [Header("Slot Behavior")]
    public float slotArrivalThreshold = 0.4f;

    [Header("Movement")]
    public float followSmoothTime = 0.15f;

    private Vector3 velocity;

    public EnemyRole role;

    private EnemyMelee melee;
    private EnemyRanged ranged;

    [Header("Reaction")]
    public float reactionDelayMin = 1f;
    public float reactionDelayMax = 2f;
    private float reactionTimer = 0f;

    // ===== Formation smoothing =====
    private Vector3 smoothedSlotPosition;
    public float formationLerpSpeed = 6f;

    // 🔒 ATTACK OWNERSHIP
    [HideInInspector] public bool attackLock = false;

    void Start()
    {
        if (coordinator == null)
            coordinator = Object.FindFirstObjectByType<EncounterCoordinator>();

        if (coordinator != null)
        {
            coordinator.Register(this);
            formationLerpSpeed = coordinator.globalFormationLerp;
        }

        smoothedSlotPosition = transform.position;
        reactionTimer = Random.Range(reactionDelayMin, reactionDelayMax);

        melee = GetComponent<EnemyMelee>();
        ranged = GetComponent<EnemyRanged>();
    }

    void Update()
    {
        // 🔒 If attacking, DO NOTHING (attack owns movement)
        if (attackLock)
            return;

        if (coordinator == null)
            return;

        if (coordinator.phalanxState != EncounterCoordinator.PhalanxState.BreakChase)
        {
            if (!coordinator.IsLeaderDead())
                reactionTimer = Random.Range(reactionDelayMin, reactionDelayMax);
        }

        if (assignedSlot == EnemySlotType.Reserve)
        {
            DisableMelee();
            DisableRanged();
            return;
        }

        if (coordinator.IsLeader(this))
        {
            if (coordinator.phalanxState != EncounterCoordinator.PhalanxState.Encircle)
                MoveTowardPlayer();

            DisableMelee();
            DisableRanged();
            return;
        }

        if (coordinator.phalanxState == EncounterCoordinator.PhalanxState.BreakChase)
        {
            if (reactionTimer > 0f)
            {
                reactionTimer -= Time.deltaTime;
                DisableMelee();
                DisableRanged();
                return;
            }

            MoveTowardPlayer();
            EnableMelee();
            EnableRanged();
            return;
        }

        Vector3 desiredSlot = coordinator.GetWorldPositionFor(this);

        smoothedSlotPosition = Vector3.Lerp(
            smoothedSlotPosition,
            desiredSlot,
            Time.deltaTime * formationLerpSpeed
        );

        float distToSlot = Vector3.Distance(transform.position, smoothedSlotPosition);

        if (distToSlot > slotArrivalThreshold * 2f)
        {
            MoveToFormation(smoothedSlotPosition);
            DisableMelee();
            DisableRanged();
            return;
        }
        else if (distToSlot > slotArrivalThreshold)
        {
            MoveToFormation(smoothedSlotPosition);
            ActBasedOnSlot();
            return;
        }

        ActBasedOnSlot();
    }

    // ================= HELPERS =================

    public Vector3 GetFormationTarget()
    {
        if (coordinator == null)
            return transform.position;

        return coordinator.GetWorldPositionFor(this);
    }

    void MoveTowardPlayer()
    {
        if (coordinator.playerTransform == null)
            return;

        Vector3 toPlayer = coordinator.playerTransform.position - transform.position;
        if (toPlayer.magnitude < 0.1f)
            return;

        float playerSpeed = GetPlayerSpeedEstimate();
        float chaseSpeed = playerSpeed * coordinator.breakChaseSpeedMultiplier;

        float roleMultiplier = role switch
        {
            EnemyRole.Offender => 0.85f,
            EnemyRole.Defender => 0.65f,
            EnemyRole.Ranger => 0.55f,
            _ => 1f
        };

        chaseSpeed *= roleMultiplier;
        transform.position += toPlayer.normalized * chaseSpeed * Time.deltaTime;
    }

    float GetPlayerSpeedEstimate()
    {
        var rb2d = coordinator.playerTransform.GetComponent<Rigidbody2D>();
        if (rb2d != null)
            return rb2d.linearVelocity.magnitude;

        return 6f;
    }

    void MoveToFormation(Vector3 target)
    {
        transform.position = Vector3.SmoothDamp(
            transform.position,
            target,
            ref velocity,
            followSmoothTime,
            20f
        );
    }

    void ActBasedOnSlot()
    {
        var state = coordinator.phalanxState;

        if (state == EncounterCoordinator.PhalanxState.Encircle)
        {
            if (role == EnemyRole.Ranger)
            {
                EnableRanged();
                DisableMelee();
            }
            else
            {
                EnableMelee();
                DisableRanged();
            }
            return;
        }


        if (state == EncounterCoordinator.PhalanxState.HoldFire)
        {
            if (role == EnemyRole.Ranger)
            {
                EnableRanged();
                DisableMelee();
            }
            else if (role == EnemyRole.Offender)
            {
                EnableMelee();
                DisableRanged();
            }
            else
            {
                DisableMelee();
                DisableRanged();
            }
            return;
        }

        DisableMelee();
        DisableRanged();
    }
    public bool IsChangingFormation(float thresholdMultiplier = 1.5f)
    {
        Vector3 target = GetFormationTarget();
        float dist = Vector3.Distance(transform.position, target);
        return dist > slotArrivalThreshold * thresholdMultiplier;
    }

    public bool IsAtSlot(float toleranceMultiplier = 1f)
    {
        Vector3 target = GetFormationTarget();
        float dist = Vector3.Distance(transform.position, target);
        return dist <= slotArrivalThreshold * toleranceMultiplier;
    }

    void EnableMelee() { if (melee) melee.enabled = true; }
    void DisableMelee() { if (melee) melee.enabled = false; }
    void EnableRanged() { if (ranged) ranged.enabled = true; }
    void DisableRanged() { if (ranged) ranged.enabled = false; }

    void OnDisable()
    {
        if (coordinator != null)
            coordinator.Unregister(this);
    }
}
