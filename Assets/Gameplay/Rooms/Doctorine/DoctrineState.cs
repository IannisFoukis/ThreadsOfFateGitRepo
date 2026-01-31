public class DoctrineState
{
    // Core permissions
    public bool canRetreat;
    public bool canSacrifice;

    // Formation quality
    // 0 = total chaos, 1 = disciplined, >1 = fanatic
    public float formationDiscipline;

    // Path flags
    public bool fanatic;
    public bool chaotic;

    // Optional: behavioral hints
    public float aggressionMultiplier;
    public float coordinationDelay;
    public bool IsFormationBreaking(float threshold = 0.5f)
    {
        return chaotic && formationDiscipline < threshold;
    }

}
