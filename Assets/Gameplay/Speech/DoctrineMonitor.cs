using UnityEngine;

public class DoctrineMonitor : MonoBehaviour
{
    private DoctrineState state;
    private bool formationBreakAnnounced = false;
    private bool fanaticAnnounced = false;

    public void Init(DoctrineState doctrine)
    {
        state = doctrine;
    }

    private void Update()
    {
        if (state == null)
            return;

        // 🟥 CHAOTIC BREAK
        if (!formationBreakAnnounced && state.chaotic && state.formationDiscipline < 0.4f)
        {
            formationBreakAnnounced = true;
            SpeechBus.Emit(EnemySpeechEvent.FormationBreak);
        }

        // ⬛ FANATIC LOCK
        if (!fanaticAnnounced && state.fanatic)
        {
            fanaticAnnounced = true;
            SpeechBus.Emit(EnemySpeechEvent.FanaticLock);
        }
    }
}
