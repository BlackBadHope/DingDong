namespace DingDongBiomeHomes.Items.MobRelics;

internal static class MobRelicSourceCatalog
{
	internal const int ExpectedMobCount = MobRelicGeneratedSources.ExpectedMobCount;
	internal const int ExpectedItemCount = MobRelicGeneratedSources.ExpectedItemCount;
	internal const int ExpectedTextureCount = MobRelicGeneratedSources.ExpectedItemCount;

	internal static readonly MobRelicSourceDefinition[] All =
	[
		.. MobRelicGeneratedSources.All
	];
}
