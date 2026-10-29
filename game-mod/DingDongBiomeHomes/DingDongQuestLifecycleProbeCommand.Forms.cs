using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

// "dingdongquestprobe forms": every First Word Was form end to end (accept -> progress ->
// turn in -> Completed+1 -> item and coins at the performer), the failure/restart rules,
// abandon, pause/handover between two synthetic performers, save (forward-compatible v2 tag
// with optional keys) / load of a release-line world, and the Dialogue-Choice second option
// through a save round-trip.
public sealed partial class DingDongQuestLifecycleProbeCommand
{
	private const string ProbeIdentityB = "ffeeddccbbaa99887766554433221100";
	private static readonly string[] FormKinds = { "Deliver", "Hunt", "Visit", "Observe", "GatherEnv", "Trial", "DialogueChoice", "Delayed", "Expedition" };

	internal static string RunFormsProbe(Mod mod, string worldFile)
	{
		InitializeLifecycleProbe(out var failures, out var ledger, out Player player, out NPC npc, out _, out var questPlayer, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _);
		NPC target = Main.npc[SyntheticNpcIndex - 1];
		var verdicts = new List<string>();
		var restore = new List<(int X, int Y, bool Has, ushort Type, short FX, short FY)>();
		Player playerB = null;
		try
		{
			foreach (string kind in FormKinds)
			{
				var form = new List<string>();
				RunForm(kind, player, npc, target, questPlayer, form, restore);
				verdicts.Add(kind + "=" + (form.Count == 0 ? "PASS" : "FAIL"));
				failures.AddRange(form.Select(f => kind + ":" + f));
			}
			verdicts.Add("abandon=" + Verdict(RunAbandon(player, npc, target, questPlayer), failures, "abandon"));
			playerB = Main.player[SyntheticPlayerIndex - 1];
			verdicts.Add("pauseHandover=" + Verdict(RunPauseHandover(player, playerB, npc, target), failures, "pause"));
			verdicts.Add("persistence=" + Verdict(RunPersistence(ledger, player, npc, questPlayer, restore), failures, "persistence"));
		}
		finally
		{
			foreach (var t in restore) { Tile tile = Main.tile[t.X, t.Y]; tile.HasTile = t.Has; tile.TileType = t.Type; tile.TileFrameX = t.FX; tile.TileFrameY = t.FY; }
			if (playerB != null) { playerB.active = false; }
			target.active = false; npc.active = false; player.active = false;
			ledger.ClearWorld();
		}
		bool lore = LoreCatalog.ContractPass(out string loreFailure, out int residents, out int stages, out int codex);
		string verdict = failures.Count == 0 ? "PASS" : "FAIL";
		string summary = $"DINGDONG_QUEST_FORMS_PROBE|verdict={verdict}|{string.Join("|", verdicts)}|lore={(lore ? "PASS" : "FAIL")}|loreResidents={residents}/{NpcTownManifest.AuthoredCount}|loreStages={stages}|codex={codex}|loreFailure={OneLine(loreFailure)}|failures={OneLine(string.Join(',', failures.Take(30)))}|world={OneLine(worldFile)}|syntheticPlayer=True|userSavesTouched=False";
		mod.Logger.Info(summary);
		return summary;
	}

	private static string Verdict(List<string> local, List<string> all, string prefix)
	{
		all.AddRange(local.Select(f => prefix + ":" + f));
		return local.Count == 0 ? "PASS" : "FAIL";
	}

	private static bool TryPickStage(string kind, out NpcTownIdentity identity, out int stage, Func<ResidentStoryStep, bool> extra = null)
	{
		foreach (var candidate in NpcTownManifest.All.OrderBy(n => ResidentStoryRoles.IsVanilla(n.NpcKey) ? 0 : 1).ThenBy(n => n.NpcKey, StringComparer.Ordinal))
		{
			if (ResidentEncounterSystem.IsPending(candidate.NpcKey) || !NpcResidentRegistry.TryGetType(candidate.NpcKey, out _) || !SettlementWorldState.TryGetResidence(candidate.NpcKey, out _)) continue;
			var story = PersonalResidentQuestCatalog.Story(candidate.NpcKey);
			for (int i = 0; story != null && i < story.Steps.Length; i++)
			{
				var s = story.Steps[i];
				bool vanillaTarget = s.TargetNpcType >= 0 && s.TargetNpcType < NPCID.Count && (s.Alternatives.Length == 0 || s.Alternatives.All(a => a < ItemID.Count));
				if (s.Kind == kind && vanillaTarget && (extra == null || extra(s))) { identity = candidate; stage = i; return true; }
			}
		}
		identity = default; stage = -1; return false;
	}

	private static void StageResident(NpcTownIdentity identity, int stage, Player player, NPC npc, params Player[] others)
	{
		var shared = NpcQuestWorldLedger.GetShared(identity.NpcKey);
		shared.ClearActive(); shared.Completed = stage;
		NpcResidentRegistry.TryGetType(identity.NpcKey, out int npcType);
		SettlementWorldState.TryGetResidence(identity.NpcKey, out SettlementResidenceReport residence);
		foreach (Item item in player.inventory) item.TurnToAir();
		Vector2 center = new(residence.Area.Center.X * 16f, residence.Area.Center.Y * 16f);
		player.position = center;
		foreach (var other in others) other.position = center;
		SetNpcDefaultsForEarlyProbe(npc, npcType, others.Prepend(player).ToArray());
		npc.whoAmI = SyntheticNpcIndex; npc.active = true; npc.position = center + new Vector2(32f, 0f);
	}

	private static void PlaceTarget(NPC target, int type, Player player, params Player[] others)
	{
		SetNpcDefaultsForEarlyProbe(target, type, others.Prepend(player).ToArray());
		target.whoAmI = SyntheticNpcIndex - 1; target.active = true; target.position = player.position + new Vector2(64f, 0f);
	}

	private static (long Coins, int Items) Holdings(Player player, int rewardType)
	{
		long coins = 0; int items = 0;
		foreach (Item item in player.inventory)
		{
			if (item.IsAir) continue;
			coins += item.type switch { ItemID.PlatinumCoin => item.stack * 1_000_000L, ItemID.GoldCoin => item.stack * 10_000L, ItemID.SilverCoin => item.stack * 100L, ItemID.CopperCoin => item.stack, _ => 0L };
			if (item.type == rewardType) items += item.stack;
		}
		return (coins, items);
	}

	private static void Kill(Player killer, NPC target, int times)
	{
		for (int i = 0; i < times; i++) NpcQuestSystem.RecordNpcKill(killer, target);
	}

	private static void Tick(Player player, int calls)
	{
		for (int i = 0; i < calls; i++) NpcQuestSystem.UpdateLocationProgress(player);
	}

	private static void PlaceTiles(Point center, int row, int count, int tileType, List<(int, int, bool, ushort, short, short)> restore)
	{
		for (int i = 0; i < count; i++)
		{
			int x = center.X - 20 + i % 40, y = center.Y - 4 - row - i / 40;
			if (!WorldGen.InWorld(x, y, 2)) continue;
			Tile tile = Main.tile[x, y];
			restore.Add((x, y, tile.HasTile, tile.TileType, tile.TileFrameX, tile.TileFrameY));
			tile.HasTile = true; tile.TileType = (ushort)tileType; tile.TileFrameX = 0; tile.TileFrameY = 0;
		}
	}

	private static void RunForm(string kind, Player player, NPC npc, NPC target, NpcQuestPlayer questPlayer, List<string> f, List<(int, int, bool, ushort, short, short)> restore)
	{
		if (!TryPickStage(kind, out var identity, out int stage)) { f.Add("no-vanilla-stage"); return; }
		string key = identity.NpcKey;
		var step = PersonalResidentQuestCatalog.Step(key, stage);
		StageResident(identity, stage, player, npc);
		Point gatherCenter = NpcQuestSystem.GatherCenter(player, key);
		if (kind == "GatherEnv") PlaceTiles(gatherCenter, 0, 40, step.TileType, restore);
		InteractWithSyntheticResident(player, npc);
		var p = questPlayer.Get(key);
		ResidentQuestDefinition d = p.Active ? p.Definition(identity) : default;
		if (!p.Active || p.ActiveVariant != stage || p.Completed != stage) { f.Add("accept"); return; }
		switch (kind)
		{
			case "Deliver":
				GiveItem(player, step.ItemType, d.Goal);
				Tick(player, 1);
				if (p.Progress != d.Goal) f.Add("progress");
				break;
			case "Expedition":
			{
				// Origin rule (NpcQuestSystem.AcquisitionMatches): crafted copies never satisfy a
				// non-crafted stage unless the item stacks; a copy created like chest loot or a natural
				// find (no spawn source, "unspecified") satisfies a world-found stage without any tag.
				bool stackable = ContentSamples.ItemsByType[step.ItemType].maxStack > 1;
				GiveItem(player, step.ItemType, d.Goal);
				foreach (Item item in player.inventory.Where(i => i.type == step.ItemType)) DamageAffinityGlobalItem.MarkRecipePrototype(item);
				if (NpcQuestSystem.IsReady(player, d, p) != (stackable || step.SourceTag == DamageTags.Crafted)) f.Add($"crafted-rule(stackable={stackable})");
				foreach (Item item in player.inventory) item.TurnToAir();
				GiveItem(player, step.ItemType, d.Goal);
				bool found = NpcQuestSystem.IsReady(player, d, p);
				if (found != (stackable || step.SourceTag == DamageTags.Worldgen)) f.Add($"found-rule(stackable={stackable})");
				if (!found)
				{
					// Fishing/crafted stages of non-stackable items: the recorded origin itself counts.
					PropertyInfo tag = typeof(DamageAffinityGlobalItem).GetProperty(nameof(DamageAffinityGlobalItem.AcquisitionTag));
					foreach (Item item in player.inventory.Where(i => i.type == step.ItemType)) tag.SetValue(item.GetGlobalItem<DamageAffinityGlobalItem>(), step.SourceTag);
					if (!NpcQuestSystem.IsReady(player, d, p)) f.Add("tagged-not-counted");
				}
				break;
			}
			case "Hunt":
				PlaceTarget(target, step.TargetNpcType, player);
				Kill(player, target, d.Goal);
				if (p.Progress != d.Goal) f.Add("progress");
				break;
			case "Visit":
				p.Progress = d.Goal; NpcQuestWorldLedger.Record(player, key, p); // biome presence is world-layout dependent
				break;
			case "Observe":
				PlaceTarget(target, step.TargetNpcType, player);
				Tick(player, 20);
				if (p.Progress <= 0) f.Add("no-progress");
				Kill(player, target, 1);
				if (p.Progress != 0) f.Add("kill-did-not-reset");
				PlaceTarget(target, step.TargetNpcType, player);
				Tick(player, d.Goal / 15 + 2);
				if (p.Progress != d.Goal) f.Add("progress");
				break;
			case "GatherEnv":
				Tick(player, 1);
				if (p.Progress != 0 || p.Baseline < 40) f.Add($"baseline(progress={p.Progress},baseline={p.Baseline})");
				PlaceTiles(gatherCenter, 1, d.Goal, step.TileType, restore);
				Tick(player, 1);
				if (p.Progress != d.Goal) f.Add("progress");
				break;
			case "Trial":
				PlaceTarget(target, step.TargetNpcType, player);
				Kill(player, target, Math.Min(3, d.Goal - 1));
				if (p.Progress <= 0) f.Add("no-progress");
				player.AddBuff(step.BuffType, 600, quiet: true);
				Tick(player, 1);
				if (p.Progress != 0) f.Add("debuff-did-not-reset");
				Kill(player, target, 2);
				if (p.Progress != 0) f.Add("counted-under-debuff");
				player.ClearBuff(step.BuffType);
				Kill(player, target, d.Goal);
				if (p.Progress != d.Goal) f.Add("progress");
				break;
			case "DialogueChoice":
			{
				// The last option (index >= 1) is the case that used to overflow Progress past Goal.
				NpcQuestSystem.HandleChoice(player, npc.whoAmI, step.Choices.Length - 1);
				if (p.Progress != 1) f.Add($"choice-progress({p.Progress})");
				// Same rule the client applies to every snapshot record (QuestSnapshotReceive).
				if (p.Progress > p.ActiveGoal) f.Add($"snapshot-contract({p.Progress}>{p.ActiveGoal})");
				if (!NpcQuestWorldLedger.TryGetChoice(key, stage, out string pending) || pending != step.Choices[^1].Id) f.Add("choice-pending");
				// An answered stage survives a world save/load with the same option.
				var worldLedger = ModContent.GetInstance<NpcQuestWorldLedger>();
				TagCompound roundTrip = new();
				worldLedger.SaveWorldData(roundTrip);
				worldLedger.LoadWorldData(roundTrip);
				p = questPlayer.Get(key);
				if (!p.Active || p.Progress != 1 || !NpcQuestWorldLedger.TryGetChoice(key, stage, out string reloaded) || reloaded != step.Choices[^1].Id) f.Add("choice-save-roundtrip");
				break;
			}
			case "Delayed":
				if (p.StartClock != NpcQuestWorldLedger.Clock) f.Add("start-clock");
				NpcQuestWorldLedger.AdvanceClockForProbe(d.Goal / 2);
				if (p.Progress <= 0 || p.Progress >= d.Goal) f.Add("half-day");
				NpcQuestWorldLedger.AdvanceClockForProbe(d.Goal);
				if (p.Progress != d.Goal) f.Add("world-clock");
				break;
		}
		if (!NpcQuestSystem.IsReady(player, d, p)) { f.Add("not-ready"); return; }
		var before = Holdings(player, step.RewardType);
		bool[] itemsBefore = CaptureActiveItems();
		InteractWithSyntheticResident(player, npc);
		var after = Holdings(player, step.RewardType);
		var drops = NewWorldItems(itemsBefore);
		long coins = after.Coins - before.Coins + CoinValue(drops);
		int items = after.Items - before.Items + drops.Where(i => i.type == step.RewardType).Sum(i => i.stack);
		foreach (Item item in drops) item.TurnToAir();
		var done = questPlayer.Get(key);
		if (done.Active || done.Completed != stage + 1) f.Add("complete");
		if (coins != step.Coins) f.Add($"coins({coins}/{step.Coins})");
		if (kind is not ("Deliver" or "Expedition") && items != step.RewardCount) f.Add($"item({items}/{step.RewardCount})");
		if (kind == "DialogueChoice" && (!NpcQuestWorldLedger.TryGetChoice(key, stage, out string chosen) || chosen != step.Choices[^1].Id)) f.Add("choice-ledger");
		if (kind is "Deliver" or "Expedition" && player.inventory.Any(i => i.type == step.ItemType && i.stack > 0) && step.RewardType != step.ItemType) f.Add("supplies-not-consumed");
	}

	private static List<string> RunAbandon(Player player, NPC npc, NPC target, NpcQuestPlayer questPlayer)
	{
		var f = new List<string>();
		if (!TryPickStage("Hunt", out var identity, out int stage)) { f.Add("no-stage"); return f; }
		var step = PersonalResidentQuestCatalog.Step(identity.NpcKey, stage);
		StageResident(identity, stage, player, npc);
		InteractWithSyntheticResident(player, npc);
		var p = questPlayer.Get(identity.NpcKey);
		PlaceTarget(target, step.TargetNpcType, player);
		Kill(player, target, 2);
		if (!p.Active || p.Progress != 2) f.Add("setup");
		NpcQuestSystem.HandleAbandon(player, identity.NpcKey);
		if (p.Active || p.Completed != stage) f.Add("abandon-state");
		InteractWithSyntheticResident(player, npc);
		if (!p.Active || p.Progress != 0 || p.Completed != stage) f.Add("restart");
		p.ClearActive();
		return f;
	}

	private static List<string> RunPauseHandover(Player a, Player b, NPC npc, NPC target)
	{
		var f = new List<string>();
		if (!TryPickStage("Hunt", out var identity, out int stage)) { f.Add("no-stage"); return f; }
		var step = PersonalResidentQuestCatalog.Step(identity.NpcKey, stage);
		b.active = false; b.dead = false; b.whoAmI = SyntheticPlayerIndex - 1; b.name = "ProbeB";
		EnsureProbeModPlayers(b);
		foreach (Item item in b.inventory) item.TurnToAir();
		StageResident(identity, stage, a, npc, b);
		b.active = true;
		var questA = a.GetModPlayer<NpcQuestPlayer>(); var questB = b.GetModPlayer<NpcQuestPlayer>();
		questB.SetNetworkIdentity(ProbeIdentityB);
		NpcQuestWorldLedger.BindPlayer(b, allowLocalImport: false);
		InteractWithSyntheticResident(a, npc);
		var p = NpcQuestWorldLedger.GetShared(identity.NpcKey);
		PlaceTarget(target, step.TargetNpcType, a, b);
		Kill(a, target, 2);
		if (!p.Active || !p.IsAssignedTo(ProbeIdentity) || p.Progress != 2) f.Add("A-accept");
		NpcQuestSystem.HandleInteraction(b, npc.whoAmI, false);
		if (!p.IsAssignedTo(ProbeIdentity)) f.Add("B-stole-active");
		NpcQuestWorldLedger.ReleaseClaims(ProbeIdentity);
		if (!p.Active || !p.Paused || p.Progress != 2) f.Add("pause");
		NpcQuestWorldLedger.BindPlayer(a, allowLocalImport: false);
		if (p.Paused || !p.IsAssignedTo(ProbeIdentity)) f.Add("resume-on-return");
		NpcQuestWorldLedger.ReleaseClaims(ProbeIdentity);
		NpcQuestSystem.HandleInteraction(b, npc.whoAmI, false);
		if (p.Paused || !p.IsAssignedTo(ProbeIdentityB) || p.Progress != 2) f.Add("B-takeover");
		Kill(b, target, p.ActiveGoal - p.Progress);
		var beforeA = Holdings(a, step.RewardType); var beforeB = Holdings(b, step.RewardType);
		bool[] itemsBefore = CaptureActiveItems();
		NpcQuestSystem.HandleInteraction(b, npc.whoAmI, false);
		var drops = NewWorldItems(itemsBefore);
		long coinsB = Holdings(b, step.RewardType).Coins - beforeB.Coins + CoinValue(drops);
		long coinsA = Holdings(a, step.RewardType).Coins - beforeA.Coins;
		foreach (Item item in drops) item.TurnToAir();
		if (p.Active || p.Completed != stage + 1) f.Add("B-complete");
		if (coinsB != step.Coins || coinsA != 0) f.Add($"reward(A={coinsA},B={coinsB})");
		b.active = false;
		return f;
	}

	private static List<string> RunPersistence(NpcQuestWorldLedger ledger, Player player, NPC npc, NpcQuestPlayer questPlayer, List<(int, int, bool, ushort, short, short)> restore)
	{
		var f = new List<string>();
		if (!TryPickStage("Delayed", out var delayed, out int delayedStage) || !TryPickStage("GatherEnv", out var gather, out int gatherStage)) { f.Add("no-stage"); return f; }
		// A Dialogue-Choice stage already completed in a release-line world (no choice ledger).
		bool chooserUsable = TryPickStage("DialogueChoice", out var chooser, out int chooserStage) && chooser.NpcKey != delayed.NpcKey && chooser.NpcKey != gather.NpcKey;
		if (chooserUsable)
		{
			var done = NpcQuestWorldLedger.GetShared(chooser.NpcKey);
			done.ClearActive();
			done.Completed = Math.Max(done.Completed, chooserStage + 1);
		}
		StageResident(delayed, delayedStage, player, npc);
		InteractWithSyntheticResident(player, npc);
		NpcQuestWorldLedger.AdvanceClockForProbe(PersonalResidentQuestCatalog.TicksPerDay);
		StageResident(gather, gatherStage, player, npc);
		PlaceTiles(NpcQuestSystem.GatherCenter(player, gather.NpcKey), 3, 5, PersonalResidentQuestCatalog.Step(gather.NpcKey, gatherStage).TileType, restore);
		InteractWithSyntheticResident(player, npc);
		var expected = NpcQuestWorldLedger.SharedSnapshot().ToDictionary(r => r.NpcKey, r => r.Progress, StringComparer.Ordinal);
		var choices = NpcQuestWorldLedger.Choices.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
		long clock = NpcQuestWorldLedger.Clock;
		TagCompound saved = new();
		ledger.SaveWorldData(saved);
		// The release line drops ledgers newer than version 2: the tag must stay readable there.
		if (saved.GetInt("SharedResidentQuestVersion") != NpcQuestWorldLedger.SharedVersion || NpcQuestWorldLedger.SharedVersion > 2) f.Add("version");
		ledger.LoadWorldData(saved);
		foreach (var (key, before) in expected)
		{
			var now = NpcQuestWorldLedger.GetShared(key);
			if (now.Completed != before.Completed || now.Active != before.Active || now.Progress != before.Progress || now.StartClock != before.StartClock || now.Baseline != before.Baseline || now.Paused != before.Paused)
				f.Add("v3:" + key);
		}
		if (NpcQuestWorldLedger.Clock != clock || NpcQuestWorldLedger.Choices.Count != choices.Count || choices.Any(c => !NpcQuestWorldLedger.Choices.TryGetValue(c.Key, out string v) || v != c.Value)) f.Add("v3:world");
		// A v2 world from the current release: no clock, no choices, no new record fields.
		TagCompound v2 = new();
		foreach (var pair in saved)
			if (pair.Key is not ("SharedResidentQuestClock" or "SharedResidentQuestChoices")) v2[pair.Key] = pair.Value;
		v2["SharedResidentQuestVersion"] = 2;
		v2["SharedResidentQuests"] = saved.GetList<TagCompound>("SharedResidentQuests").Select(t =>
		{
			var copy = new TagCompound();
			foreach (var pair in t) if (pair.Key is not ("Paused" or "StartClock" or "Baseline")) copy[pair.Key] = pair.Value;
			return copy;
		}).ToList();
		try
		{
			ledger.LoadWorldData(v2);
			foreach (var (key, before) in expected)
			{
				var now = NpcQuestWorldLedger.GetShared(key);
				if (now.Completed != before.Completed || now.Active != before.Active) f.Add("v2:" + key);
				if (now.Active && now.ActiveKind == ResidentQuestKind.Delayed && now.StartClock != NpcQuestWorldLedger.Clock - now.Progress) f.Add("v2-delayed:" + key);
			}
			if (chooserUsable && (!NpcQuestWorldLedger.TryGetChoice(chooser.NpcKey, chooserStage, out string legacy) || !NpcQuestWorldLedger.IsLegacyChoice(legacy))) f.Add("v2-legacy-choice");
		}
		catch (Exception ex) { f.Add("v2-exception:" + ex.GetType().Name); }
		return f;
	}
}
