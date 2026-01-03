using UnityEngine;

public abstract class BreatherInteraction : MonoBehaviour
{
    protected BreatherRoom room;

    protected virtual void Awake()
    {
        room = GetComponentInParent<BreatherRoom>();
    }

    public void Interact()
    {
        Execute();
        PlayVisualFeedback();
        room.ResolveBreather();
    }

    protected abstract void Execute();
    protected virtual void PlayVisualFeedback() { }
}
