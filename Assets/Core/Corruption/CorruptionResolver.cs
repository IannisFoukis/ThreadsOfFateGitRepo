using UnityEngine;

namespace TOF.Core.Corruption
{
    public static class CorruptionResolver
    {
        // Keeps your existing entry point
        public static CorruptionTier ResolveFromKeeper()
        {
            return ResolveFromValue(KeeperWorldState.corruptionPressure);
        }

        // ✅ Add this method (the one your RoomDirector is asking for)
        public static CorruptionTier ResolveFromValue(float pressure01)
        {
            float p = Mathf.Clamp01(pressure01);

            if (p <= 0.05f) return CorruptionTier.None;
            if (p <= 0.25f) return CorruptionTier.Low;
            if (p <= 0.55f) return CorruptionTier.Medium;
            if (p <= 0.85f) return CorruptionTier.High;
            return CorruptionTier.Extreme;
        }
    }
}
