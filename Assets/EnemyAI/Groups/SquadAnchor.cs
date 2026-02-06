using System.Collections.Generic;
using UnityEngine;
using TOF.EnemyAI.Groups;

public class SquadAnchor : MonoBehaviour
{
    [Header("Squad Definition")]
    public FormationType formation = FormationType.Swarm;
    public DoctrineState doctrine;
    public EnemyRole preferredLeaderRole = EnemyRole.Offender;

    [Header("Idle State")]
    public float idleRadius = 1.5f;

    private readonly List<EnemyAgent> agents = new();
    private bool activated;

    // ─────────────────────────────────────────────
    // CLAIM + STAGING
    // ─────────────────────────────────────────────

    void ClaimNearbyAgents()
    {
        agents.Clear();

        var found = Object.FindObjectsByType<EnemyAgent>(
            FindObjectsSortMode.None
        );

        // Phase G1: claim ALL agents spawned for this room
        foreach (var a in found)
        {
            if (a == null) continue;
            agents.Add(a);
        }

        Debug.Log($"[SquadAnchor] Claimed {agents.Count} agents");
    }

    void PutAgentsInDormantState()
    {
        foreach (var a in agents)
        {
            if (a == null) continue;

            // Phase G1: staging ONLY (no engagement authority)
            a.movementLocked = true;
            a.SetAttackPositionLocked(true);

            Vector2 offset = Random.insideUnitCircle * idleRadius;
            a.transform.position = transform.position + (Vector3)offset;
        }
    }

    /// <summary>
    /// Called once after enemies spawn.
    /// This does NOT activate combat.
    /// </summary>
    public void InitializeAfterSpawn()
    {
        ClaimNearbyAgents();
        PutAgentsInDormantState();
    }

    // ─────────────────────────────────────────────
    // ACTIVATION HANDOFF (Phase G1)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Hands agents to EncounterCoordinator.
    /// Does NOT unlock, move, or engage enemies.
    /// </summary>
    public void Activate(EncounterCoordinator coordinator)
    {
        if (activated) return;
        activated = true;

        if (agents.Count == 0)
        {
            Debug.LogError("[SquadAnchor] Activate called before InitializeAfterSpawn()");
            return;
        }

        if (coordinator == null)
        {
            Debug.LogError("[SquadAnchor] EncounterCoordinator is NULL");
            return;
        }

        coordinator.BuildGroupsFromEnemies(agents, formation, doctrine);

        TryAssignLeader();

        Debug.Log($"[SquadAnchor] Squad handed off ({agents.Count} agents)");
    }

    // ─────────────────────────────────────────────
    // LEADER SELECTION (INTENT ONLY)
    // ─────────────────────────────────────────────

    void TryAssignLeader()
    {
        foreach (var a in agents)
        {
            if (a != null && a.role == preferredLeaderRole && a.Group != null)
            {
                a.Group.SetLeader(a);
                return;
            }
        }
    }
}
