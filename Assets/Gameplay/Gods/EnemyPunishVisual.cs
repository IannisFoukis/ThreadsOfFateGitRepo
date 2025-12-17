using UnityEngine;

public class EnemyPunishVisual : MonoBehaviour
{
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void ApplyPunish(int level)
    {
        sr.color = Color.Lerp(Color.white, Color.red, level * 0.25f);
    }
}
