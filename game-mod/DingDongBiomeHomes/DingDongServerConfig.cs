using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace DingDongBiomeHomes;

public sealed class DingDongServerConfig : ModConfig
{
	public static DingDongServerConfig Instance;

	public override ConfigScope Mode => ConfigScope.ServerSide;

	[DefaultValue(true)]
	public bool EnableDamageSystem { get; set; } = true;

	[DefaultValue(false)]
	public bool StrictMultiplayerDamageValidation { get; set; } = false;

	[DefaultValue(true)]
	public bool EnableNpcQuestsAndEscort { get; set; } = true;

	[DefaultValue(3)]
	[Range(1, 20)]
	public int RequiredResidentQuests { get; set; } = 3;

	[DefaultValue(true)]
	public bool EnableFiveTileBaseJump { get; set; } = true;

	[DefaultValue(true)]
	public bool EnableNightPressure { get; set; } = true;

	[DefaultValue(true)]
	public bool EnableCreatureTeleporters { get; set; } = true;

	[DefaultValue(6)]
	[Range(3, 12)]
	public int MaximumTeleportedCreatureWidthTiles { get; set; } = 6;

	[DefaultValue(6)]
	[Range(3, 12)]
	public int MaximumTeleportedCreatureHeightTiles { get; set; } = 6;

	// Presets 28.10 (owner decision B3): world presets are always on. The former visible toggle
	// is now derived from a hidden emergency switch "restore vanilla sizes" (default off).
	// tModLoader 1.4.4 builds the config UI and localization keys from PUBLIC members only
	// (ConfigManager.GetFieldsAndProperties, BindingFlags.Public | Instance) and skips
	// [JsonIgnore] members, while Newtonsoft still persists the private [JsonProperty] field.
	// To use it: set "RestoreVanillaWorldSizes": true in DingDongBiomeHomes_DingDongServerConfig.json
	// while the game is closed.
	[JsonProperty("RestoreVanillaWorldSizes")]
	private bool restoreVanillaWorldSizes;

	[JsonIgnore]
	public bool EnableContinentalWorlds => !restoreVanillaWorldSizes;

	[DefaultValue(true)]
	[ReloadRequired]
	public bool GenerateAuthoredSettlements { get; set; } = true;

	[DefaultValue(3)]
	[Range(1, 3)]
	[ReloadRequired]
	public int HousesPerDistrict { get; set; } = 3;

	[DefaultValue(true)]
	[ReloadRequired]
	public bool GenerateRoads { get; set; } = true;

	[DefaultValue(false)]
	[ReloadRequired]
	public bool GenerateCivicHubs { get; set; } = false;
}
