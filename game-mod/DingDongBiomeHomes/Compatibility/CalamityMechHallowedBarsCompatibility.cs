using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

/// <summary>
/// Calamity replaces the Hallowed Bar drops of the three mechanical bosses and their
/// treasure bags with rules gated by <c>DropHelper.HallowedBarsCondition</c>. With its
/// "Early Hardmode Progression Rework" server option (on by default) bars pay out only
/// once all three mechanical bosses are already defeated, and loot is rolled before the
/// killed boss's own flag is set, so first kills and early bags give no bars at all.
/// Ding Dong adds the exact complement of Calamity's own condition with the vanilla
/// amounts: per kill or bag exactly one of the two rules can pay out, never both.
/// Calamity's files and configuration stay untouched.
/// </summary>
internal static class CalamityMechHallowedBars
{
	private static bool resolved;
	private static IItemDropRuleCondition calamityCondition;

	internal static int NpcRulesAdded { get; private set; }
	internal static int BagRulesAdded { get; private set; }
	internal static readonly List<string> Skipped = [];

	internal static bool IsMechBoss(int type) => type is NPCID.TheDestroyer or NPCID.Retinazer
		or NPCID.Spazmatism or NPCID.SkeletronPrime;

	internal static bool IsMechBag(int type) => type is ItemID.DestroyerBossBag or ItemID.TwinsBossBag
		or ItemID.SkeletronPrimeBossBag;

	internal static IItemDropRuleCondition CalamityCondition
	{
		get
		{
			if (resolved) return calamityCondition;
			resolved = true;
			if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity)) return null;
			try
			{
				calamityCondition = calamity.Code.GetType("CalamityMod.DropHelper")
					?.GetField("HallowedBarsCondition", BindingFlags.Public | BindingFlags.Static)
					?.GetValue(null) as IItemDropRuleCondition;
			}
			catch (Exception error)
			{
				Log($"verdict=SKIPPED|reason=calamity-condition-unreadable|error={error.GetType().Name}");
			}
			if (calamityCondition is null)
				Log($"verdict=SKIPPED|reason=calamity-condition-missing|calamity={calamity.Version}");
			return calamityCondition;
		}
	}

	/// <summary>
	/// Adds the complement only when Calamity's gated bar rule is present and nothing
	/// else in the table can produce Hallowed Bars, so a future Calamity change can
	/// never lead to doubled bars.
	/// </summary>
	internal static bool TryAdd(ILoot loot, string scope, IItemDropRule rule)
	{
		IItemDropRuleCondition calamity = CalamityCondition;
		if (calamity is null) return false;
		int gated = 0, other = 0;
		foreach (IItemDropRule root in loot.Get(false))
		{
			List<DropRateInfo> rates = [];
			try { root.ReportDroprates(rates, new DropRateInfoChainFeed(1f)); }
			catch (Exception error)
			{
				Skip(scope, $"report-failed:{root.GetType().FullName}:{error.GetType().Name}");
				return false;
			}
			foreach (DropRateInfo rate in rates.Where(rate => rate.itemId == ItemID.HallowedBar))
			{
				if (rate.conditions?.Any(condition => ReferenceEquals(condition, calamity)) == true) gated++;
				else other++;
			}
		}
		if (gated == 0 || other != 0)
		{
			Skip(scope, $"unexpected-bar-rules|calamityGated={gated}|other={other}");
			return false;
		}
		loot.Add(rule);
		return true;
	}

	internal static void CountNpc() => NpcRulesAdded++;
	internal static void CountBag() => BagRulesAdded++;

	private static void Skip(string scope, string reason)
	{
		Skipped.Add($"{scope}:{reason}");
		Log($"verdict=SKIPPED|scope={scope}|reason={reason}");
	}

	private static void Log(string fields) =>
		ModLoader.GetMod("DingDongBiomeHomes").Logger.Warn("DINGDONG_CALAMITY_MECH_BARS|" + fields);

	internal static void Unload()
	{
		resolved = false;
		calamityCondition = null;
		NpcRulesAdded = 0;
		BagRulesAdded = 0;
		Skipped.Clear();
	}
}

/// <summary>Drops exactly when Calamity's own Hallowed Bar condition withholds the bars.</summary>
internal sealed class CalamityWithheldHallowedBarsCondition : IItemDropRuleCondition
{
	private static readonly Conditions.MissingTwin MissingTwin = new();
	private readonly IItemDropRuleCondition calamity;
	private readonly bool normalOnly;
	private readonly bool lastTwin;

	internal CalamityWithheldHallowedBarsCondition(IItemDropRuleCondition calamity, bool normalOnly, bool lastTwin)
	{
		this.calamity = calamity;
		this.normalOnly = normalOnly;
		this.lastTwin = lastTwin;
	}

	public bool CanDrop(DropAttemptInfo info) =>
		(!normalOnly || !info.IsExpertMode) &&
		(!lastTwin || info.npc is not null && MissingTwin.CanDrop(info)) &&
		!calamity.CanDrop(info);

	public bool CanShowItemDropInUI() => true;

	public string GetConditionDescription() =>
		Language.GetTextValue("Mods.DingDongBiomeHomes.CalamityMechBars.Condition");
}

public sealed class CalamityMechHallowedBarsGlobalNpc : GlobalNPC
{
	public override bool AppliesToEntity(NPC entity, bool lateInstantiation) =>
		lateInstantiation && CalamityMechHallowedBars.IsMechBoss(entity.type) && ModLoader.HasMod("CalamityMod");

	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		IItemDropRuleCondition calamity = CalamityMechHallowedBars.CalamityCondition;
		if (calamity is null) return;
		bool twin = npc.type is NPCID.Retinazer or NPCID.Spazmatism;
		IItemDropRule rule = ItemDropRule.ByCondition(
			new CalamityWithheldHallowedBarsCondition(calamity, normalOnly: true, lastTwin: twin),
			ItemID.HallowedBar, 1, 15, 30);
		if (CalamityMechHallowedBars.TryAdd(npcLoot, $"npc:{npc.type}", rule))
			CalamityMechHallowedBars.CountNpc();
	}

	public override void Unload() => CalamityMechHallowedBars.Unload();
}

public sealed class CalamityMechHallowedBarsGlobalItem : GlobalItem
{
	public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
		lateInstantiation && CalamityMechHallowedBars.IsMechBag(entity.type) && ModLoader.HasMod("CalamityMod");

	public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
	{
		IItemDropRuleCondition calamity = CalamityMechHallowedBars.CalamityCondition;
		if (calamity is null) return;
		IItemDropRule rule = ItemDropRule.ByCondition(
			new CalamityWithheldHallowedBarsCondition(calamity, normalOnly: false, lastTwin: false),
			ItemID.HallowedBar, 1, 20, 35);
		if (CalamityMechHallowedBars.TryAdd(itemLoot, $"item:{item.type}", rule))
			CalamityMechHallowedBars.CountBag();
	}
}
