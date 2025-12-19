using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    [SerializeField] ActiveSkillSO dashSkill;

    PlayerController player;

    void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Dash key pressed");

            if (dashSkill != null)
                dashSkill.Activate(player);
            else
                Debug.LogWarning("Dash skill not assigned");
        }
    }
}
