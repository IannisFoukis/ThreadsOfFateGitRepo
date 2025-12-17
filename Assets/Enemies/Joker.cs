#if UNITY_EDITOR
using UnityEngine;

[ExecuteAlways]
public class Joker : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}
#endif
