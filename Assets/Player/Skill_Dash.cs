using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Dash (Debug)")]
public class Skill_Dash : ActiveSkillSO
{
    public override void Activate(PlayerController player)
    {
        Debug.Log("[DASH] Triggered successfully");
    }
}
