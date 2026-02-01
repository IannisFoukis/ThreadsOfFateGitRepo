using UnityEngine;

public class FormationResolver : MonoBehaviour
{
    public FormationType Current { get; private set; } = FormationType.Swarm;

    public void SetFormation(FormationType formation)
    {
        Current = formation;
    }
}