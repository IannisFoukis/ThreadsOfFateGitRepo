public class BreatherRoom : RoomController
{
    protected override void Start()
    {
        base.Start();
        Invoke(nameof(Finish), 1.5f);
    }

    void Finish()
    {
        CompleteRoom();
    }
}
