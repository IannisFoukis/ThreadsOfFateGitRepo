using UnityEngine;

public class EnemyBehaviorController : MonoBehaviour
{
    EnemyChase chase;
    EnemyAgent agent;

    void Awake()
    {
        TryGetComponent(out chase);
        TryGetComponent(out agent);
    }

    public bool CanAct()
    {
        return true;
    }


    // Used by doctrines / shrine effects
   
}
