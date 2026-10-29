using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DingDongBiomeHomes.Compatibility;

public sealed class RandomTileBudgetConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	// Configured by the server/host and synchronized by tModLoader's config system.
	// Disable to retain the original full-area growth budget on oversized worlds.
	[DefaultValue(true)]
	public bool AutoNormalizeExpandedWorldGrowth = true;
}
