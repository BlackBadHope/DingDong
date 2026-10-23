using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Data-driven runtime classifier. Optional-mod content is inspected only via
/// public Terraria/tModLoader metadata, so no compatibility assembly is a hard
/// dependency.
/// </summary>
public static partial class DamageRuntimeClassifier
{
	public static DamageSourceClassification ClassifyDamageOverTime(NPC npc)
	{
		string names = string.Join(' ', npc.buffType.Where(type => type > 0).Select(StableBuffIdentity));
		string element = npc.lavaWet ? DamageTags.Fire : InferElement(names);
		return new DamageSourceClassification(CreateSource("dingdong:source/dot/runtime", DamageTags.DamageOverTime, element, DamageTags.Sustained, DamageTags.UnspecifiedAcquisition), npc.buffType.Any(type => type >= BuffID.Count) ? DamageCoverageKind.Adapter : DamageCoverageKind.Fallback, false);
	}
}
