using System;

namespace TOF.EnemyAI.Groups
{
    [Serializable]
    public struct GroupScaling
    {
        public float cooldownMult;     // < 1 = faster attacks
        public float aggressionMult;   // > 1 = more willing to push
        public float cohesionMult;     // > 1 = tighter formation adherence

        public static GroupScaling Default => new GroupScaling
        {
            cooldownMult = 1f,
            aggressionMult = 1f,
            cohesionMult = 1f
        };
    }

    [Serializable]
    public struct GroupDirectives
    {
        public GroupIntent intent;
        public bool phalanxActive;
        public GroupScaling scaling;
    }
}
