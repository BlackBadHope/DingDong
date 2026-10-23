// Curated relic-book roster. This is intentionally small: four recognizable
// bosses, five difficulty-tier books each, twenty registered items total.
namespace DingDongBiomeHomes.Items.MobRelics;

internal static class MobRelicGeneratedSources
{
	internal const int ExpectedMobCount = 4;
	internal const int ExpectedItemCount = 20;
	internal const int ExpectedTriggerCount = 20;
	internal const int ExpectedInvocationCount = 20;
	internal const int ExpectedTwistCount = 16;
	internal const string SourceCatalogSha256 = "D083B4E8E8059827EAB47706A4C4B8F423488DC16EDFB1942B05DFFF05A6554F";

	internal static readonly MobRelicSourceDefinition[] All =
	[
		new("Terraria/EyeofCthulhu", "Eye of Cthulhu", "Terraria", true, "CEBD35FEF6D09FB1"),
		new("CalamityMod/Crabulon", "Crabulon", "CalamityMod", true, "B567F6138312325F"),
		new("SOTS/Glowmoth", "Glowmoth", "SOTS", true, "83094BC28C7E215D"),
		new("ThoriumMod/TheGrandThunderBird", "The Grand Thunder Bird", "ThoriumMod", true, "9BA20D261B5D0FF3")
	];
}
