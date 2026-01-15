using UnityEngine;

public class DashInvulnerable : IDashModifier
{
    public void OnDashStart(PlayerController player)
    {
        var health = player.GetComponent<Health>();
        if (health != null)
            health.SetInvulnerable(true);
    }

    public void OnDashEnd(PlayerController player)
    {
        var health = player.GetComponent<Health>();
        if (health != null)
            health.SetInvulnerable(false);
    }
}
