using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal static class DingDongLocalizationContract
{
	private static readonly string[] RepresentativeKeys =
	{
		"Mods.DingDongBiomeHomes.Items.ClassicResidentCache.DisplayName",
		"Mods.DingDongBiomeHomes.Items.ExpertResidentCache.DisplayName",
		"Mods.DingDongBiomeHomes.Items.MasterResidentCache.DisplayName",
		"Mods.DingDongBiomeHomes.Items.LegendaryResidentCache.DisplayName",
		"Mods.DingDongBiomeHomes.Damage.WeaponAffinity",
		"Mods.DingDongBiomeHomes.Damage.WeaponBehavior",
		"Mods.DingDongBiomeHomes.Damage.WeaponGuide",
		"Mods.DingDongBiomeHomes.Damage.Guide.Pages.Overview.Title",
		"Mods.DingDongBiomeHomes.Damage.Guide.Pages.Overview.Subtitle",
		"Mods.DingDongBiomeHomes.Damage.Guide.Pages.Overview.StatusBody",
		"Mods.DingDongBiomeHomes.Quests.Journal.Title",
		"Mods.DingDongBiomeHomes.Quests.Journal.EntryBody",
		"Mods.DingDongBiomeHomes.Quests.Journal.Subtitle",
		"Mods.DingDongBiomeHomes.Quests.Journal.Summary",
		"Mods.DingDongBiomeHomes.Quests.RewardGranted",
		"Mods.DingDongBiomeHomes.Quests.ThemedBonus",
		"Mods.DingDongBiomeHomes.Quests.RewardStreak",
		"Mods.DingDongBiomeHomes.Quests.Objectives.Boss",
		"Mods.DingDongBiomeHomes.Quests.Objectives.Banner",
		"Mods.DingDongBiomeHomes.Quests.Objectives.Progression",
		"Mods.DingDongBiomeHomes.Quests.Difficulty.Legendary",
		"Mods.DingDongBiomeHomes.Quests.RequestSent",
		"Mods.DingDongBiomeHomes.Quests.IdentityPending",
		"Mods.DingDongBiomeHomes.Quests.ButtonAccept",
		"Mods.DingDongBiomeHomes.Quests.ButtonAcceptContinued",
		"Mods.DingDongBiomeHomes.Quests.ButtonTurnIn",
		"Mods.DingDongBiomeHomes.Quests.ButtonSurvey",
		"Mods.DingDongBiomeHomes.Quests.ButtonPatrol",
		"Mods.DingDongBiomeHomes.Quests.ButtonSupplies",
		"Mods.DingDongBiomeHomes.Quests.ButtonDefense",
		"Mods.DingDongBiomeHomes.Quests.ButtonBoss",
		"Mods.DingDongBiomeHomes.Quests.ButtonBiome",
		"Mods.DingDongBiomeHomes.Quests.ButtonBanner",
		"Mods.DingDongBiomeHomes.Quests.ButtonProgression",
		"Mods.DingDongBiomeHomes.Quests.ButtonInvite",
		"Mods.DingDongBiomeHomes.Quests.ButtonFollowing",
		"Mods.DingDongBiomeHomes.Quests.ButtonBusy",
		"Mods.DingDongBiomeHomes.NpcDialogue.GoblinFusionButton",
		"Mods.DingDongBiomeHomes.NpcDialogue.GoblinFusionHint",
		"Mods.DingDongBiomeHomes.NpcDialogue.HomeGoodsButton",
		"Mods.DingDongBiomeHomes.Hub.Title",
		"Mods.DingDongBiomeHomes.Hub.Tabs.Quests",
		"Mods.DingDongBiomeHomes.Hub.Residences.Summary",
		"Mods.DingDongBiomeHomes.Hub.Help.ControlsBody",
		"Mods.DingDongBiomeHomes.Teleporter.CreatureTooltip",
		"Mods.DingDongBiomeHomes.ResidenceFlag.MapEntry",
		"Mods.DingDongBiomeHomes.ResidenceDiscovery.Discovered",
		"Mods.DingDongBiomeHomes.Keybinds.OpenDamageGuide.DisplayName",
		"Mods.DingDongBiomeHomes.Keybinds.OpenQuestJournal.DisplayName"
	};

	internal static int KeyCount => RepresentativeKeys.Length;

	internal static bool Pass(out string failure)
	{
		List<string> raw = new();
		foreach (string key in RepresentativeKeys)
		{
			string value = Language.GetTextValue(key);
			if (string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal))
				raw.Add(key);
		}
		failure = raw.Count == 0 ? string.Empty : string.Join(',', raw.Select(key => key[(key.LastIndexOf('.') + 1)..]));
		return raw.Count == 0;
	}
}

public sealed class DingDongLocalizationContractSystem : ModSystem
{
	public override void PostSetupContent()
	{
		bool pass = DingDongLocalizationContract.Pass(out string failure);
		string marker = $"DINGDONG_LOCALIZATION_CONTRACT|verdict={(pass ? "PASS" : "FAIL")}|language={Language.ActiveCulture.Name}|keys={DingDongLocalizationContract.KeyCount}/{DingDongLocalizationContract.KeyCount}|raw={(pass ? "none" : failure)}";
		if (pass)
			Mod.Logger.Info(marker);
		else
			Mod.Logger.Error(marker);
	}
}
