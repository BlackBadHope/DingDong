using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static class AccessoryWorkshopFormatting
{
    internal static string Coins(long copper, bool russian)
    {
        if (copper <= 0) return "—";
        long[] values = { copper / 1_000_000L, copper % 1_000_000L / 10_000L, copper % 10_000L / 100L, copper % 100L };
        string[] names = russian ? new[] { "плат.", "зол.", "сер.", "мед." } : new[] { "plat", "gold", "silver", "copper" };
        var parts = new List<string>();
        for (int i = 0; i < values.Length; i++) if (values[i] != 0) parts.Add($"{values[i]} {names[i]}");
        return string.Join(" ", parts);
    }
    internal static bool Confirmed(string armed, string current, long armedAt, long now) =>
        !string.IsNullOrEmpty(armed) && armed == current && unchecked((ulong)(now - armedAt)) < 10_000;
}
