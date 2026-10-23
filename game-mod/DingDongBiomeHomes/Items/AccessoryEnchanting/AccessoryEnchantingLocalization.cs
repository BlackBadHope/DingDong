using Terraria.Localization;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static class AccessoryEnchantingLocalization
{
	private const string Root = "Mods.DingDongBiomeHomes.AccessoryEnchanting";

	internal static string Text(string suffix, params object[] args) =>
		Language.GetTextValue($"{Root}.{suffix}", args);
}
