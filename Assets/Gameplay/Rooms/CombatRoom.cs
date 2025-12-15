public class CombatRoom : RoomController
{
    protected override void Start()
    {
        base.Start();
        Invoke(nameof(Finish), 2f);
    }

    void Finish()
    {
        CompleteRoom();
    }
}
