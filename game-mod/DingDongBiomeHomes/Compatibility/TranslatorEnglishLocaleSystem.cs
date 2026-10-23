using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

// Project tRU supplies some Russian defaults through its English resource. Apply
// our English wording only after an English reload. A Russian reload restores
// tRU's own resource in memory; none of its Russian text is redistributed here.
public sealed class TranslatorEnglishLocaleSystem : ModSystem
{
	private Dictionary<string, string> english;
	private static readonly Action<LocalizedText, string> SetText =
		typeof(LocalizedText).GetMethod("SetValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.CreateDelegate<Action<LocalizedText, string>>();

	public override void OnLocalizationsLoaded()
	{
		if (Language.ActiveCulture != GameCulture.FromCultureName(GameCulture.CultureName.English)
			|| !ModLoader.TryGetMod("CalamityRuTranslate", out _)) return;
		english ??= JsonSerializer.Deserialize<Dictionary<string, string>>(
			Mod.GetFileBytes("Compatibility/TranslatorEnglish.json"));
		foreach (var pair in english)
		{
			if (!pair.Key.StartsWith("Mods.CalamityRuTranslate.", StringComparison.Ordinal)
				|| !Language.Exists(pair.Key)) continue;
			SetText(Language.GetText(pair.Key), pair.Value);
		}
	}

	public override void Unload() => english = null;
}
