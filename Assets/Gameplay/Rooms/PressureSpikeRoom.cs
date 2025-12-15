public class PressureSpikeRoom : RoomController
{
    protected override void Start()
    {
        base.Start();
        Invoke(nameof(Finish), 2.5f);
    }

    void Finish()
    {
        CompleteRoom();
    }
}
