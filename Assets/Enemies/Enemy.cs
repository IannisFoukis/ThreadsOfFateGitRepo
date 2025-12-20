using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static int AliveCount = 0;

    public float damageMultiplier { get; internal set; }

    private void OnEnable()
    {
        AliveCount++;
    }

    private void OnDisable()
    {
        AliveCount--;
    }
}
