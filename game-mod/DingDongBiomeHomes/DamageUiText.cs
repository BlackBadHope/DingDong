using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Terraria.GameInput;
using Terraria.Localization;

namespace DingDongBiomeHomes;
/// <summary>
/// Player-facing names and explanations for the server-authoritative damage
/// ontology. UI always reads the same immutable profiles used by combat.
/// </summary>
internal static partial class DamageUiText
{
	public static string Tag(string id)
	{
		string key = id switch
		{
			DamageTags.KineticContact => "KineticContact",
			DamageTags.Arrow => "Arrow",
			DamageTags.Bullet => "Bullet",
			DamageTags.Thrown => "Thrown",
			DamageTags.Rogue => "Rogue",
			DamageTags.MagicProjectile => "MagicProjectile",
			DamageTags.Beam => "Beam",
			DamageTags.Explosion => "Explosion",
			DamageTags.MinionContact => "MinionContact",
			DamageTags.MinionProjectile => "MinionProjectile",
			DamageTags.Sentry => "Sentry",
			DamageTags.Trap => "Trap",
			DamageTags.DamageOverTime => "DamageOverTime",
			DamageTags.Environment => "Environment",
			DamageTags.Neutral => "Neutral",
			DamageTags.Fire => "Fire",
			DamageTags.Frost => "Frost",
			DamageTags.Lightning => "Lightning",
			DamageTags.Water => "Water",
			DamageTags.Earth => "Earth",
			DamageTags.Poison => "Poison",
			DamageTags.Holy => "Holy",
			DamageTags.Shadow => "Shadow",
			DamageTags.Astral => "Astral",
			DamageTags.Void => "Void",
			DamageTags.Gel => "Gel",
			DamageTags.Nature => "Nature",
			DamageTags.ModElement => "ModElement",
			DamageTags.Single => "Single",
			DamageTags.Burst => "Burst",
			DamageTags.Sustained => "Sustained",
			DamageTags.Piercing => "Piercing",
			DamageTags.Area => "Area",
			DamageTags.Chain => "Chain",
			DamageTags.Delayed => "Delayed",
			DamageTags.RapidMultihit => "RapidMultihit",
			DamageTags.Crafted => "Crafted",
			DamageTags.CreatureDrop => "CreatureDrop",
			DamageTags.BossDrop => "BossDrop",
			DamageTags.BiomeChest => "BiomeChest",
			DamageTags.Worldgen => "Worldgen",
			DamageTags.Quest => "Quest",
			DamageTags.Shop => "Shop",
			DamageTags.Event => "Event",
			DamageTags.Fishing => "Fishing",
			DamageTags.Transmutation => "Transmutation",
			DamageTags.UnspecifiedAcquisition => "UnspecifiedAcquisition",
			DamageTags.UnknownAcquisition => "UnknownAcquisition",
			DamageTags.Gelatinous => "Gelatinous",
			_ => "Unknown"
		};
		return Language.GetTextValue($"Mods.DingDongBiomeHomes.Damage.Tags.{key}");
	}

	public static string Axis(WeightedDamageAxis axis) => string.Join(" + ", axis.Tags.Select(tag => tag.Weight >= 0.999d ? Tag(tag.Id) : $"{Tag(tag.Id)} {tag.Weight.ToString("P0", CultureInfo.CurrentCulture)}"));
	public static string Tags(IEnumerable<string> tags) => string.Join(" + ", tags.Distinct(StringComparer.Ordinal).Select(Tag));
	public static string GuideKey()
	{
		List<string> keys = DingDongBiomeHomes.OpenDamageGuideKeybind?.GetAssignedKeys(InputMode.Keyboard);
		return keys is { Count: > 0 } ? string.Join("/", keys) : Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.Guide.Common.Unbound");
	}

}
