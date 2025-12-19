using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillUI : MonoBehaviour
{
    [SerializeField] Image cooldownFill;
    [SerializeField] TextMeshProUGUI chargesText;

    ActiveSkillSO skill;

    public void Bind(ActiveSkillSO boundSkill)
    {
        skill = boundSkill;
        Refresh();
    }

    void Update()
    {
        if (skill == null) return;
        Refresh();
    }

    void Refresh()
    {
        // Cooldown
        if (skill.cooldown > 0f)
        {
            float t = Mathf.Clamp01(skill.cooldownTimer / skill.cooldown);
            cooldownFill.fillAmount = t;
        }
        else
        {
            cooldownFill.fillAmount = 0f;
        }

        // Charges
        chargesText.text = skill.currentCharges.ToString();
        chargesText.enabled = skill.maxCharges > 1;
    }
}
