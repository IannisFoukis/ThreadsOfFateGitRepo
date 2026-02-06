using System.Collections.Generic;
using UnityEngine;

namespace TOF.EnemyAI.Groups
{
    /// <summary>
    /// EnemyGroup is a command unit: owns members, leader, formation/phalanx,
    /// interprets doctrine into intent, and pushes group directives to members.
    /// </summary>
    public class EnemyGroup
    {
        public int GroupId { get; private set; }
        public FormationType Formation { get; private set; }
        public DoctrineState Doctrine { get; private set; }
        public GroupIntent Intent { get; private set; }
        public bool PhalanxActive { get; private set; }

        public EnemyAgent Leader { get; private set; }
        public IReadOnlyList<EnemyAgent> Members => _members;

        private readonly List<EnemyAgent> _members = new List<EnemyAgent>();
        private GroupScaling _scaling = GroupScaling.Default;

        // Degradation when leader dies (group remains functional but weaker)
        private const float LEADERLESS_COHESION_PENALTY = 0.75f;
        private const float LEADERLESS_AGGRESSION_PENALTY = 0.85f;

        public EnemyGroup(int groupId, FormationType formation, DoctrineState doctrine)
        {
            GroupId = groupId;
            Formation = formation;
            Doctrine = doctrine;

            Intent = GroupIntent.Advance;
            PhalanxActive = (formation == FormationType.Phalanx);
        }

        // ─────────────────────────────────────────────
        // MEMBERSHIP
        // ─────────────────────────────────────────────

        public void AddMember(EnemyAgent agent)
        {
            if (agent == null) return;
            if (_members.Contains(agent)) return;

            _members.Add(agent);
            agent.AssignGroup(this);

            if (Leader == null)
                SetLeader(agent);

            PushDirectivesTo(agent);
        }

        public void RemoveMember(EnemyAgent agent)
        {
            if (agent == null) return;
            if (!_members.Remove(agent)) return;

            if (Leader == agent)
            {
                Leader = null;
                ElectNewLeader();
            }
        }

        // ─────────────────────────────────────────────
        // LEADER
        // ─────────────────────────────────────────────

        public void SetLeader(EnemyAgent agent)
        {
            if (agent == null) return;
            if (!_members.Contains(agent)) _members.Add(agent);

            Leader = agent;
            foreach (var m in _members)
                m.SetLeaderFlag(m == Leader);
        }

        public void ElectNewLeader()
        {
            for (int i = 0; i < _members.Count; i++)
            {
                var a = _members[i];
                if (a != null && a.isActiveAndEnabled)
                {
                    SetLeader(a);
                    return;
                }
            }

            // Leaderless state
            foreach (var m in _members)
                if (m != null)
                    m.SetLeaderFlag(false);
        }

        // ─────────────────────────────────────────────
        // AUTHORITY INPUTS
        // ─────────────────────────────────────────────

        public void SetDoctrine(DoctrineState doctrine)
        {
            Doctrine = doctrine;
            RecomputeIntent();
            PushDirectivesToAll();
        }

        public void SetFormation(FormationType formation, bool phalanxActive)
        {
            Formation = formation;
            PhalanxActive = phalanxActive;
            RecomputeIntent();
            PushDirectivesToAll();
        }

        public void SetScaling(GroupScaling scaling)
        {
            _scaling = scaling;
            PushDirectivesToAll();
        }

        public void Tick(float pressure01)
        {
            RecomputeIntent(pressure01);
            PushDirectivesToAll();
        }

        // ─────────────────────────────────────────────
        // INTENT LOGIC (G1 minimal, deterministic)
        // ─────────────────────────────────────────────

        private void RecomputeIntent(float pressure01 = 0.5f)
        {
            // Phase G1 rule:
            // EnemyGroup does NOT depend on specific DoctrineState enum values.
            // Doctrine meaning is interpreted upstream (EncounterCoordinator).

            if (PhalanxActive)
            {
                Intent = (pressure01 < 0.6f)
                    ? GroupIntent.HoldLine
                    : GroupIntent.Advance;
            }
            else
            {
                Intent = (pressure01 > 0.55f)
                    ? GroupIntent.BreakFormation
                    : GroupIntent.Encircle;
            }
        }


        // ─────────────────────────────────────────────
        // DIRECTIVES
        // ─────────────────────────────────────────────

        private GroupDirectives BuildDirectives()
        {
            var s = _scaling;

            if (Leader == null)
            {
                s.cohesionMult *= LEADERLESS_COHESION_PENALTY;
                s.aggressionMult *= LEADERLESS_AGGRESSION_PENALTY;
            }

            return new GroupDirectives
            {
                intent = Intent,
                phalanxActive = PhalanxActive,
                scaling = s
            };
        }

        private void PushDirectivesToAll()
        {
            var d = BuildDirectives();
            for (int i = 0; i < _members.Count; i++)
            {
                var m = _members[i];
                if (m == null) continue;
                m.ApplyGroupDirectives(d);
            }
        }

        private void PushDirectivesTo(EnemyAgent agent)
        {
            if (agent == null) return;
            agent.ApplyGroupDirectives(BuildDirectives());
        }
    }
}
