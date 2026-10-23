using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;
internal static class MobRelicNaming
{
	private static readonly string[] Adjectives = ["Amber", "Ashen", "Belltouched", "Briarbound", "Cinderlit", "Dawnworn", "Deepwood", "Duskglass", "Echoing", "Fallow", "Farspark", "Gilded", "Gloaming", "Hearthbound", "Hollow", "Iridescent", "Lantern", "Moonworn", "Mossbound", "Nightglass", "Pale", "Rainworn", "Riftmarked", "Runed", "Sheltered", "Silent", "Starfallen", "Stormworn", "Sunken", "Threshold", "Wayward", "Wild"];
	private static readonly string[][] Nouns = [["Fragment", "Husk", "Knot", "Scrap", "Shard", "Trace", "Splinter", "Remnant"], ["Charm", "Curio", "Keepsake", "Memento", "Oddity", "Token", "Trinket", "Talisman"], ["Brand", "Cipher", "Crest", "Insignia", "Mark", "Seal", "Sign", "Writ"], ["Core", "Crown", "Heart", "Icon", "Relic", "Testament", "Totem", "Votive"], ["Chronicle", "Echo", "Legacy", "Legend", "Myth", "Omen", "Saga", "Wonder"]];
	internal static string DisplayName(MobRelicSourceDefinition source, MobRelicTier tier)
	{
		byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes($"{source.ContentKey}|{(byte)tier}|name"));
		string adjective = Adjectives[digest[0] % Adjectives.Length];
		string noun = Nouns[(int)tier][digest[1] % Nouns[(int)tier].Length];
		return $"{adjective} {noun} of {source.DisplayName}";
	}

	internal static string PlayerText(string localized, string fallback) => string.IsNullOrWhiteSpace(localized) || localized.Contains("Mods.DingDongBiomeHomes.", StringComparison.Ordinal) ? fallback : localized;
	internal static string SourceDisplayName(MobRelicSourceDefinition source)
	{
		string key = $"Mods.DingDongBiomeHomes.MobRelics.SourceNames.{source.StableId}";
		string value = Language.GetTextValue(key);
		return value == key ? source.DisplayName : value;
	}
}
