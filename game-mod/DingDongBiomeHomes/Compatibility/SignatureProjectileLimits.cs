using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

public sealed class SignatureProjectileLimits : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private int basePenetrate, baseLifetime;
    private float baseSlots;
    private bool captured;
    internal void Capture(Projectile p)
    {
        if (captured) return;
        captured=true; basePenetrate=p.penetrate; baseLifetime=p.timeLeft; baseSlots=p.minionSlots;
    }
    internal void Normalize(Projectile p, int level)
    {
        if (!captured) return;
        p.penetrate=SignatureBalanceRules.Penetration(basePenetrate,p.penetrate);
        p.maxPenetrate=SignatureBalanceRules.Penetration(basePenetrate,p.maxPenetrate);
        p.timeLeft=SignatureBalanceRules.Lifetime(baseLifetime,p.timeLeft);
        if (p.minion && baseSlots>0) p.minionSlots=SignatureBalanceRules.MinionCost(baseSlots,level);
    }
}
