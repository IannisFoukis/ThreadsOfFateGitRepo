public class EntryRoom : RoomController
{
    protected override void Start()
    {
        base.Start();
        Invoke(nameof(Finish), 1f);
    }

    void Finish()
    {
        CompleteRoom();
    }
}
