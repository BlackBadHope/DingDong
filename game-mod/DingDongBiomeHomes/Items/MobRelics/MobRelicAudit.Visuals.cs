using Terraria;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static partial class MobRelicAudit
{
	private static void AuditVisualAndLocalization(MobRelicAuditContext context, MobRelicVisualPlayer visualProbe, GeneratedMobRelicItem relic, Item original)
	{
		const string modPrefix = "DingDongBiomeHomes/";
		string texturePath = relic.Texture.StartsWith(modPrefix) ? $"{relic.Texture[modPrefix.Length..]}.rawimg" : string.Empty;
		if (context.Mod.FileExists(texturePath))
			context.TexturesPassed++;
		else
			Failure(context, $"texture:{relic.Name}");

		visualProbe.ResetEffects();
		visualProbe.RegisterVisible(relic.Type, relic.Tier);
		if (visualProbe.VisibleRelics.Count == 1 && visualProbe.VisibleRelics[0].ItemType == relic.Type && visualProbe.VisibleRelics[0].Tier == relic.Tier && context.Mod.FileExists(texturePath))
			context.PlayerVisualsPassed++;
		else
			Failure(context, $"player-visual:{relic.Name}");

		if (Localized(relic.DisplayName.Value)) context.LocalizationPassed++; else Failure(context, $"display:{relic.Name}");
		if (HasPlayerFacingTooltip(relic)) context.LocalizationPassed++; else Failure(context, $"tooltip:{relic.Name}");
		AuditFunctionality(context, relic, original);
	}
}
