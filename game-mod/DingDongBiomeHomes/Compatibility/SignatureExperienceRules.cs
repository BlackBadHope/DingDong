using System;

namespace DingDongBiomeHomes.Compatibility;

// XP depends on active combat time, not inflated damage or projectile count.
internal static class SignatureExperienceRules
{
    internal static double PerSecond(int mastery, int stage, bool melee)
    {
        double early = 1d + 2d * Math.Max(0d, 1d - Math.Max(0, mastery) / 40d);
        double veteran = 1d / (1d + Math.Max(0d, mastery - 40d) / 80d);
        double progress = stage >= 100 ? .35d : stage >= 80 ? .5d : stage >= 60 ? .65d : stage >= 40 ? .85d : 1d;
        return 12d * early * veteran * progress * (melee ? 1.25d : 1d);
    }
}
