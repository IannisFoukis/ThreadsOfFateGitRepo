using UnityEngine;

public enum EnemyState
{
    Idle,
    Chasing,
    Hit,
    Dead
}

public class EnemyStateController : MonoBehaviour
{
    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    public void SetState(EnemyState newState)
    {
        if (CurrentState == EnemyState.Dead) return;

        CurrentState = newState;
    }
}
