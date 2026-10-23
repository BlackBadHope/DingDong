using System;

namespace DingDongBiomeHomes.Compatibility;

internal static class SignatureBalanceRules
{
    internal static int StageCap(bool hardmode, bool evilBoss, bool plantera, bool moonLord) =>
        moonLord ? 100 : plantera ? 80 : hardmode ? 60 : evilBoss ? 40 : 20;
    internal static int CombatLevel(int raw, int stage) => Math.Clamp(raw, 0, stage);
    // Progress continues beyond the combat ceiling. Slot cost approaches zero
    // without becoming free, negative or imposing a separate gameplay level cap.
    internal static float MinionCost(float baseline, int raw) => baseline <= 0 ? baseline :
        (float)(baseline * SlotFactor(raw));
    private static double SlotFactor(int raw) => raw <= 180 ?
        1d - Math.Max(0, raw) * .005d : .1d / (1d + (raw - 180d) * .005d);
    internal static float SlotDiscountPercent(int raw) =>
        Math.Min(99.99999f, (float)(100d * (1d - SlotFactor(raw))));
    internal static int DoubleLimit(int baseline) => baseline > int.MaxValue / 2 ? int.MaxValue : baseline * 2;
    internal static int Penetration(int baseline, int current) => baseline < 0 ? baseline :
        baseline == 0 ? 0 : current < 0 ? DoubleLimit(baseline) : Math.Min(current, DoubleLimit(baseline));
    internal static int Lifetime(int baseline, int current) =>
        baseline <= 0 ? current : Math.Min(current, DoubleLimit(baseline));
    internal static int Defence(int baseline, int raw, int stage) =>
        (int)Math.Floor(Math.Max(0, baseline) * CombatLevel(raw, stage) * 0.0025d);
}
