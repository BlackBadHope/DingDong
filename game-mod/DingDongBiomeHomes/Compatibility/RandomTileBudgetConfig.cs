using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DingDongBiomeHomes.Compatibility;

public sealed class RandomTileBudgetConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	// Configured by the server/host and synchronized by tModLoader's config system.
	// Opt-in experiment: unchanged native growth is the public default until live profiling.
	[DefaultValue(false)]
	public bool AutoNormalizeExpandedWorldGrowth = false;
}
