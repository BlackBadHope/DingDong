using System;
using System.Collections.Generic;
using System.Linq;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static class AccessoryEnchantmentCatalog
{
	internal const int EnchantmentCount = 14;
	private static readonly IReadOnlyDictionary<AccessoryEnchantmentId, AccessoryEnchantmentDefinition> Definitions = new Dictionary<AccessoryEnchantmentId, AccessoryEnchantmentDefinition>
	{
		[AccessoryEnchantmentId.Bastion] = new(nameof(AccessoryEnchantmentId.Bastion)),
		[AccessoryEnchantmentId.Menace] = new(nameof(AccessoryEnchantmentId.Menace)),
		[AccessoryEnchantmentId.Precision] = new(nameof(AccessoryEnchantmentId.Precision)),
		[AccessoryEnchantmentId.Velocity] = new(nameof(AccessoryEnchantmentId.Velocity)),
		[AccessoryEnchantmentId.Rhythm] = new(nameof(AccessoryEnchantmentId.Rhythm)),
		[AccessoryEnchantmentId.Vitality] = new(nameof(AccessoryEnchantmentId.Vitality)),
		[AccessoryEnchantmentId.Arcana] = new(nameof(AccessoryEnchantmentId.Arcana)),
		[AccessoryEnchantmentId.Vanguard] = new(nameof(AccessoryEnchantmentId.Vanguard)),
		[AccessoryEnchantmentId.Ballistics] = new(nameof(AccessoryEnchantmentId.Ballistics)),
		[AccessoryEnchantmentId.Spellweave] = new(nameof(AccessoryEnchantmentId.Spellweave)),
		[AccessoryEnchantmentId.Command] = new(nameof(AccessoryEnchantmentId.Command)),
		[AccessoryEnchantmentId.Fortune] = new(nameof(AccessoryEnchantmentId.Fortune)),
		[AccessoryEnchantmentId.Echo] = new(nameof(AccessoryEnchantmentId.Echo)),
		[AccessoryEnchantmentId.SecondWind] = new(nameof(AccessoryEnchantmentId.SecondWind))
	};
	internal static IReadOnlyList<AccessoryEnchantmentId> All { get; } = Enum.GetValues<AccessoryEnchantmentId>().Where(value => value != AccessoryEnchantmentId.None).ToArray();

	internal static bool IsValid(AccessoryEnchantmentId id) => Definitions.ContainsKey(id);
	internal static AccessoryEnchantmentDefinition Get(AccessoryEnchantmentId id) => Definitions[id];
}
