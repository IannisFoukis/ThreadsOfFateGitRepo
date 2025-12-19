public class EntryRoom : RoomController
{
    protected override void Start()
    {
        base.Start();
        Invoke(nameof(Finish), 4f);
    }

    void Finish()
    {
        CompleteRoom();
    }
}
