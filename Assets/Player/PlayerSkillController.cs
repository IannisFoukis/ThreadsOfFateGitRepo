using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    [SerializeField] ActiveSkillSO dashSkill;
    [SerializeField] SkillUI dashUI;
    PlayerController player;

    void Awake()
    {
        player = GetComponent<PlayerController>();

        if (dashSkill != null)
        {
            dashSkill.OnEquip();
            dashUI.Bind(dashSkill);
        }

    }
    private void Start()
    {
        if (dashSkill is Skill_Dash dash)
        {
            dash.AddModifier(new DashInvulnerable());
        }

    }
    void Update()
    {
        if (dashSkill == null) return;

        dashSkill.TickCooldown(Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!dashSkill.CanActivate())
            {
                Debug.Log("[SKILL] Dash on cooldown");
                return;
            }

            Debug.Log("[SKILL] Dash activated");

            dashSkill.Activate(player);
            dashUI?.Pulse();

            dashSkill.ConsumeCharge();
        }
    }
}
