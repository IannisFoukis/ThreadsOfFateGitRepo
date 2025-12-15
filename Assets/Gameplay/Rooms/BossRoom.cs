public class BossRoom : RoomController
{
    protected override void Start()
    {
        base.Start();
        Invoke(nameof(Finish), 3f);
    }

    void Finish()
    {
        CompleteRoom();
    }
}
