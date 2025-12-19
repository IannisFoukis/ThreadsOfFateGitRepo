public class DashInvulnerable : IDashModifier
{
    public void OnDashStart(PlayerController player)
    {
        player.Health.SetInvulnerable(true);
    }

    public void OnDashEnd(PlayerController player)
    {
        player.Health.SetInvulnerable(false);
    }
}
