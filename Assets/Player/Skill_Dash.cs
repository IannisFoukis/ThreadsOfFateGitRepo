using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Dash")]
public class Skill_Dash : ActiveSkillSO
{
    public float dashForce = 12f;
    public float dashDuration = 0.15f;

    readonly List<IDashModifier> modifiers = new();

    public override void Activate(PlayerController player)
    {
        if (player == null) return;
        player.StartCoroutine(DashRoutine(player));
    }

    IEnumerator DashRoutine(PlayerController player)
    {
        var motor = player.GetComponent<PlayerMotor>();
        if (motor == null) yield break;

        // Dash based on current input (top-down). If no input, fallback to right.
        Vector2 dashDir = player.MoveInput;
        if (dashDir.sqrMagnitude < 0.01f)
            dashDir = Vector2.right;
        dashDir.Normalize();

        foreach (var m in modifiers)
            m.OnDashStart(player);

        motor.ApplyDashVelocity(dashDir * dashForce);

        yield return new WaitForSeconds(dashDuration);

        motor.ClearDashVelocity();

        foreach (var m in modifiers)
            m.OnDashEnd(player);
    }

    public void AddModifier(IDashModifier modifier)
    {
        if (modifier == null) return;
        if (!modifiers.Contains(modifier))
            modifiers.Add(modifier);
    }
}
