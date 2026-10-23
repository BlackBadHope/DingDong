namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal readonly record struct AccessoryEnchantmentDefinition(string LocalizationId)
{
	internal string Name => AccessoryEnchantingLocalization.Text($"Enchantments.{LocalizationId}.Name");
	internal string Effect => AccessoryEnchantingLocalization.Text($"Enchantments.{LocalizationId}.Effect");
}
