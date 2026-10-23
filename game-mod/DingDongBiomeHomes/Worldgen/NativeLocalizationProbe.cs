using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria.Localization;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeLocalizationProbe
{
	internal static object Capture()
	{
		if (Environment.GetEnvironmentVariable("DINGDONG_LOCALIZATION_PROBE") != "1") return null;
		GameCulture before = Language.ActiveCulture;
		Dictionary<string,string> Read(GameCulture.CultureName culture)
		{
			LanguageManager.Instance.SetLanguage(GameCulture.FromCultureName(culture));
			return Language.FindAll((key, text) => key.StartsWith("Mods.", StringComparison.Ordinal)).ToDictionary(t => t.Key, t => t.Value);
		}
		try
		{
			var english = Read(GameCulture.CultureName.English); var russian = Read(GameCulture.CultureName.Russian);
			bool Cyr(string value) => Regex.IsMatch(value, "[А-Яа-яЁё]");
			string[] englishCyrillic = english.Where(p => Cyr(p.Value)).Select(p => p.Key).ToArray();
			string[] latinOnly = english.Where(p => Regex.IsMatch(p.Value,"[A-Za-z]") &&
				russian.TryGetValue(p.Key,out string value) && Regex.IsMatch(value,"[A-Za-z]") && !Cyr(value)).Select(p => p.Key).ToArray();
			var latinOnlyValues = latinOnly.ToDictionary(key => key, key => new { english = english[key], russian = russian[key] });
			string[] englishRawKeys = english.Where(p => p.Key == p.Value).Select(p => p.Key).ToArray();
			string[] russianRawKeys = russian.Where(p => p.Key == p.Value).Select(p => p.Key).ToArray();
			string[] unresolvedReferences = english.Where(p => p.Value.Contains("{$", StringComparison.Ordinal)).Select(p => p.Key)
				.Concat(russian.Where(p => p.Value.Contains("{$", StringComparison.Ordinal)).Select(p => p.Key)).Distinct().ToArray();
			bool signature = russian.TryGetValue("Mods.SignatureEquipment.Configs.SignatureEquipmentServerConfig.Headers.Mode",out string mode) && mode == "Режим";
			bool calamity = russian.Any(p => p.Key.StartsWith("Mods.CalamityMod.Items.") && Cyr(p.Value));
			bool thorium = russian.Any(p => p.Key.StartsWith("Mods.ThoriumMod.Items.") && Cyr(p.Value));
			bool translatorCultureRestored = !Terraria.ModLoader.ModLoader.HasMod("CalamityRuTranslate") ||
				(english.TryGetValue("Mods.CalamityRuTranslate.Configs.TRuConfig.DisplayName", out string enConfig) && enConfig == "Configuration" &&
				russian.TryGetValue("Mods.CalamityRuTranslate.Configs.TRuConfig.DisplayName", out string ruConfig) && ruConfig == "Конфигурация");
			if(!signature || !calamity || !thorium || englishCyrillic.Length != 0 || !translatorCultureRestored)
				throw new InvalidOperationException("Actual language switch or authored localization override failed.");
			return new { verdict = "PASS", englishKeys = english.Count, russianKeys = russian.Count,
				signatureOverride = signature, calamityRussianLoaded = calamity, thoriumRussianLoaded = thorium,
				translatorCultureRestored,
				englishCyrillicKeys = englishCyrillic, latinOnlyRussianKeys = latinOnly,
				latinOnlyRussianValues = latinOnlyValues, englishRawKeys, russianRawKeys, unresolvedReferences,
				runtimeEnglishValues = english, runtimeRussianValues = russian,
				fullTranslationAccepted = false, visualAcceptance = false };
		}
		finally { LanguageManager.Instance.SetLanguage(before); }
	}
}
