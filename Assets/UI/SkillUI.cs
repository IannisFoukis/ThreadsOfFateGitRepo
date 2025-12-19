using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillUI : MonoBehaviour
{
    [SerializeField] Image cooldownFill;
    [SerializeField] TextMeshProUGUI chargesText;

    [Header("Corruption Look")]
    [SerializeField] Color corruptedColor = new Color(0.8f, 0.1f, 0.1f, 0.8f);

    ActiveSkillSO skill;
    Color normalFillColor;
    Coroutine pulseRoutine;

    void Awake()
    {
        if (cooldownFill != null)
            normalFillColor = cooldownFill.color;
    }

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
        // Cooldown ring
        if (cooldownFill != null)
            cooldownFill.fillAmount = skill.CooldownNormalized;

        // Charges
        if (chargesText != null)
        {
            chargesText.text = skill.currentCharges.ToString();
            chargesText.enabled = skill.maxCharges > 1;
        }

        // Corruption tint (optional, only if you have RunCorruptionState.Instance)
        bool corrupted = RunCorruptionState.Instance != null && RunCorruptionState.Instance.IsCorrupted;

        if (cooldownFill != null)
            cooldownFill.color = corrupted ? corruptedColor : normalFillColor;
    }

    // Call this when the skill successfully activates
    public void Pulse()
    {
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    IEnumerator PulseRoutine()
    {
        Vector3 baseScale = Vector3.one;
        Vector3 upScale = Vector3.one * 1.15f;

        transform.localScale = upScale;

        float t = 0f;
        const float dur = 0.12f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime; // UI should animate even if timescale changes
            float k = Mathf.Clamp01(t / dur);
            transform.localScale = Vector3.Lerp(upScale, baseScale, k);
            yield return null;
        }

        transform.localScale = baseScale;
        pulseRoutine = null;
    }
}
