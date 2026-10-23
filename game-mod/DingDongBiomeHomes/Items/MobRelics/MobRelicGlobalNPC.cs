using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal readonly record struct MobRelicDropRegistration(
	string ContentKey,
	bool Boss,
	int CommonDenominator,
	int ExpertDenominator,
	int MasterDenominator,
	int LegendaryDenominator,
	int CommonTraceType,
	int CommonCurioType,
	int ExpertType,
	int MasterType,
	int LegendaryType);

internal sealed class LegendaryWorldDropCondition : IItemDropRuleCondition
{
	public bool CanDrop(DropAttemptInfo info) => info.IsMasterMode && Main.getGoodWorld;
	public bool CanShowItemDropInUI() => Main.masterMode && Main.getGoodWorld;
	public string GetConditionDescription() => Language.GetTextValue("Mods.DingDongBiomeHomes.MobRelics.LegendaryCondition");
}

internal sealed class ClassicWorldDropCondition : IItemDropRuleCondition
{
	public bool CanDrop(DropAttemptInfo info) => !info.IsExpertMode;
	public bool CanShowItemDropInUI() => !Main.expertMode;
	public string GetConditionDescription() => Language.GetTextValue("Mods.DingDongBiomeHomes.MobRelics.ClassicCondition");
}

internal sealed class ExactExpertWorldDropCondition : IItemDropRuleCondition
{
	public bool CanDrop(DropAttemptInfo info) => info.IsExpertMode && !info.IsMasterMode;
	public bool CanShowItemDropInUI() => Main.expertMode && !Main.masterMode;
	public string GetConditionDescription() => Language.GetTextValue("Mods.DingDongBiomeHomes.MobRelics.ExactExpertCondition");
}

internal sealed class ExactMasterWorldDropCondition : IItemDropRuleCondition
{
	public bool CanDrop(DropAttemptInfo info) => info.IsMasterMode && !Main.getGoodWorld;
	public bool CanShowItemDropInUI() => Main.masterMode && !Main.getGoodWorld;
	public string GetConditionDescription() => Language.GetTextValue("Mods.DingDongBiomeHomes.MobRelics.ExactMasterCondition");
}

public sealed class MobRelicGlobalNPC : GlobalNPC
{
	internal const int NormalCommonDenominator = 250;
	internal const int NormalExpertDenominator = 250;
	internal const int NormalMasterDenominator = 250;
	internal const int NormalLegendaryDenominator = 250;
	internal const int BossCommonDenominator = 4;
	internal const int BossDifficultyDenominator = 4;

	private static readonly Dictionary<string, MobRelicDropRegistration> Registrations = new(StringComparer.Ordinal);

	internal static IReadOnlyDictionary<string, MobRelicDropRegistration> RegisteredDrops => Registrations;

	internal static bool IsEligible(NPC npc) => NpcLootEligibility.IsEligible(npc);

	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		if (!IsEligible(npc))
			return;
		string contentKey = QuestContentRegistry.ContentKey(npc);
		if (!MobSpecificRelicCatalog.TryGet(contentKey, out MobRelicSet relics))
			return;

		int commonDenominator = npc.boss ? BossCommonDenominator : NormalCommonDenominator;
		int expertDenominator = npc.boss ? BossDifficultyDenominator : NormalExpertDenominator;
		int masterDenominator = npc.boss ? BossDifficultyDenominator : NormalMasterDenominator;
		int legendaryDenominator = npc.boss ? BossDifficultyDenominator : NormalLegendaryDenominator;
		int commonTraceType = relics.CommonTrace.Type;
		int commonCurioType = relics.CommonCurio.Type;
		int expertType = relics.ExpertSeal.Type;
		int masterType = relics.MasterRelic.Type;
		int legendaryType = relics.LegendaryMyth.Type;

		AddExactPool(npcLoot, new ClassicWorldDropCondition(), commonDenominator, commonTraceType, commonCurioType);
		AddExactPool(npcLoot, new ExactExpertWorldDropCondition(), expertDenominator, commonTraceType, commonCurioType, expertType);
		AddExactPool(npcLoot, new ExactMasterWorldDropCondition(), masterDenominator, commonTraceType, commonCurioType, masterType);
		AddExactPool(npcLoot, new LegendaryWorldDropCondition(), legendaryDenominator, commonTraceType, commonCurioType, legendaryType);

		MobRelicDropRegistration registration = new(
			contentKey,
			npc.boss,
			commonDenominator,
			expertDenominator,
			masterDenominator,
			legendaryDenominator,
			commonTraceType,
			commonCurioType,
			expertType,
			masterType,
			legendaryType);
		if (!Registrations.TryAdd(contentKey, registration) && Registrations[contentKey] != registration)
			throw new InvalidOperationException($"Conflicting mob relic drop registration for {contentKey}.");
	}

	private static void AddExactPool(NPCLoot npcLoot, IItemDropRuleCondition condition, int denominator, params int[] itemTypes)
	{
		LeadingConditionRule exactWorld = new(condition);
		exactWorld.OnSuccess(ItemDropRule.OneFromOptions(denominator, itemTypes));
		npcLoot.Add(exactWorld);
	}

	internal static void Reset() => Registrations.Clear();
}
