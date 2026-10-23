using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static class DamageTagWire
{
	private const byte CustomTag = byte.MaxValue;
	private static readonly string[] KnownTags =
	{
		DamageTags.Neutral,
		DamageTags.Fire,
		DamageTags.Frost,
		DamageTags.Lightning,
		DamageTags.Water,
		DamageTags.Earth,
		DamageTags.Poison,
		DamageTags.Holy,
		DamageTags.Shadow,
		DamageTags.Astral,
		DamageTags.Void,
		DamageTags.Gel,
		DamageTags.Nature,
		DamageTags.ModElement,
		DamageTags.Crafted,
		DamageTags.CreatureDrop,
		DamageTags.BossDrop,
		DamageTags.BiomeChest,
		DamageTags.Worldgen,
		DamageTags.Quest,
		DamageTags.Shop,
		DamageTags.Event,
		DamageTags.Fishing,
		DamageTags.Transmutation,
		DamageTags.UnspecifiedAcquisition,
		DamageTags.UnknownAcquisition
	};
	private static readonly Dictionary<string, byte> Codes = BuildCodes();
	public static void Write(BinaryWriter writer, string tag)
	{
		if (Codes.TryGetValue(tag, out byte code))
		{
			writer.Write(code);
			return;
		}

		writer.Write(CustomTag);
		writer.Write(tag ?? string.Empty);
	}

	public static string Read(BinaryReader reader)
	{
		byte code = reader.ReadByte();
		return code == CustomTag ? reader.ReadString() : code < KnownTags.Length ? KnownTags[code] : string.Empty;
	}

	public static string SanitizeAcquisition(string value, string fallback) => IsNamespacedCategory(value, "dingdong:acquisition/") ? value : fallback;
	public static string SanitizeElement(string value, string fallback) => IsNamespacedCategory(value, "dingdong:element/") ? value : fallback;
	private static bool IsNamespacedCategory(string value, string prefix) => !string.IsNullOrWhiteSpace(value) && value.StartsWith(prefix, StringComparison.Ordinal) && value.Length <= 96 && value.IndexOfAny(new[] { '\r', '\n', '\0' }) < 0;
	private static Dictionary<string, byte> BuildCodes()
	{
		Dictionary<string, byte> result = new(StringComparer.Ordinal);
		for (byte index = 0; index < KnownTags.Length; index++)
			result.Add(KnownTags[index], index);
		return result;
	}
}
