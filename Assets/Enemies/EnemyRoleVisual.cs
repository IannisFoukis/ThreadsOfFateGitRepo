using UnityEngine;

public class EnemyRoleVisual : MonoBehaviour
{
    [Header("Sprites by Role")]
    public Sprite offenderSprite;
    public Sprite defenderSprite;
    public Sprite rangerSprite;

    private EnemyAgent agent;
    private SpriteRenderer sr;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        ApplyRoleSprite();
    }

    public void ApplyRoleSprite()
    {
        if (agent == null || sr == null)
            return;

        switch (agent.role)
        {
            case EnemyRole.Offender:
                sr.sprite = offenderSprite;
                break;

            case EnemyRole.Defender:
                sr.sprite = defenderSprite;
                break;

            case EnemyRole.Ranger:
                sr.sprite = rangerSprite;
                break;
        }
    }
}
