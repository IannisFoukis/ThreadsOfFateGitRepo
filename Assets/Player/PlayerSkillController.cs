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
            dashSkill.ConsumeCharge();
        }
    }
}
