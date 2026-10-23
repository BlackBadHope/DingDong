using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static partial class MobRelicAudit
{
	private static bool HasPlayerFacingTooltip(GeneratedMobRelicItem relic)
	{
		List<TooltipLine> lines = new();
		relic.ModifyTooltips(lines);
		string[] required = ["MobRelicDrop", "MobRelicEffect", "MobRelicSynergy"];
		return required.All(name => lines.Count(line => string.Equals(line.Name, name, StringComparison.Ordinal) && Localized(line.Text)) == 1);
	}
}
