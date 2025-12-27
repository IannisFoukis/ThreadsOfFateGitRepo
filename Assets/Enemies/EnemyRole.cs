using UnityEngine;

public enum EnemyRole
{
    Offender, 
    Defender,
    Support,
    Activator,
    Flanker,
    Melee,
    Ranged,
    Charger,
    Elite
}

public class EnemyRoleController : MonoBehaviour
{
    public EnemyRole role;
}



/////////////////Escalation Axes(what modifies behavior)///////////////

///////////Enemy behavior escalates based on://////////////////////////

//Shrine Tier(1–3)

//Run Tension

//Mid-Fight Escalation Trigger

//Role Synergy (multiple enemies alive)

//The matrix must support layered escalation, not replacements.

//(tiers, shrine escalation, corruption, tension

//Enemy roles

//Behavior tiers (Base / Aggressive / Elite)

//Shrine tier escalation

//Run tension & corruption