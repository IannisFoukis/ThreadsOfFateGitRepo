public enum FormationState
{
    Idle,       // Not enough enemies / no tactic active
    Hold,       // Front pressure, flanks passive
    Flank,      // Enemies take positions
    Collapse    // Coordinated attack window
}
