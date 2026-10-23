using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
internal static partial class AiArsenalAudit
{
	private static readonly IReadOnlyDictionary<string, string> ExpectedElements = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		[nameof(PorchlightBlade)] = DamageTags.Neutral,
		[nameof(GildedGableGlaive)] = DamageTags.Neutral,
		[nameof(CinderChimneyCleaver)] = DamageTags.Fire,
		[nameof(HallowedHearthbrand)] = DamageTags.Holy,
		[nameof(SolarHomesteadGreatblade)] = DamageTags.Fire,
		[nameof(ShinglewoodShortbow)] = DamageTags.Neutral,
		[nameof(GildedWindowRepeater)] = DamageTags.Neutral,
		[nameof(CinderlockCarbine)] = DamageTags.Fire,
		[nameof(HallowedRooftopRepeater)] = DamageTags.Holy,
		[nameof(VortexEavestrike)] = DamageTags.Void,
		[nameof(CandlewickWand)] = DamageTags.Fire,
		[nameof(AtticStarScepter)] = DamageTags.Astral,
		[nameof(HearthflameTome)] = DamageTags.Fire,
		[nameof(SanctumLanternStaff)] = DamageTags.Holy,
		[nameof(NebulaThresholdGrimoire)] = DamageTags.Astral,
		[nameof(TwinebellLash)] = DamageTags.Neutral,
		[nameof(BriarThresholdWhip)] = DamageTags.Nature,
		[nameof(FurnaceChain)] = DamageTags.Fire,
		[nameof(PrismaticBanisterLash)] = DamageTags.Neutral,
		[nameof(StardustWelcomeCord)] = DamageTags.Astral
	};
}
