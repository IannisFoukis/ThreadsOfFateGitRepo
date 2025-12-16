using UnityEngine;
using System.Collections;

public class Invincibility : MonoBehaviour
{
    public float duration = 0.5f;
    bool invincible = false;

    public bool IsInvincible => invincible;

    public void Trigger()
    {
        if (!invincible)
            StartCoroutine(IFrameRoutine());
    }

    IEnumerator IFrameRoutine()
    {
        invincible = true;
        yield return new WaitForSeconds(duration);
        invincible = false;
    }
}
