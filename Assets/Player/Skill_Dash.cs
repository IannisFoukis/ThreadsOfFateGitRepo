using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Dash")]
public class Skill_Dash : ActiveSkillSO
{
    public float dashForce = 12f;
    public float dashDuration = 0.15f;

    readonly List<IDashModifier> modifiers = new();

    public void AddModifier(IDashModifier mod)
    {
        if (!modifiers.Contains(mod))
            modifiers.Add(mod);
    }

    public override void Activate(PlayerController player)
    {
        player.StartCoroutine(DashRoutine(player));
    }

    IEnumerator DashRoutine(PlayerController player)
    {
        foreach (var m in modifiers)
            m.OnDashStart(player);

        Vector2 dir = player.LastMoveDir;

        // 🛡 HARD GUARD — NEVER ZERO
        if (dir.sqrMagnitude < 0.01f)
        {
            Debug.LogWarning("Dash dir was zero, using fallback");
            dir = Vector2.right;
        }

        player.Motor.ForceMove(
            dir,
            dashForce,
            dashDuration
        );

        yield return new WaitForSeconds(dashDuration);

        foreach (var m in modifiers)
            m.OnDashEnd(player);
    }
}
