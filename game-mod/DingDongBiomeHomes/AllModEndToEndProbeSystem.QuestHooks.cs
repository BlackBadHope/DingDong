using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace DingDongBiomeHomes;

public sealed partial class AllModEndToEndProbeSystem
{
	private static QuestHookSnapshot RunQuestKillHooks()
	{
		Player player = Main.player[0];
		DingDongQuestLifecycleProbeCommand.EnsureProbeModPlayers(player);
		player.active = true;
		player.dead = false;
		player.whoAmI = 0;
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		questPlayer.SetNetworkIdentity(DingDongQuestLifecycleProbeCommand.ProbeIdentity);
		// Restore the lifecycle fixture's authoritative records after the kill
		// census, so the following real world save/reload checks the same quests.
		NpcQuestWorldLedger.BindPlayer(player, allowLocalImport: false);
		IReadOnlyList<(string NpcKey, ResidentQuestProgress Progress)> saved = questPlayer.CurrentSnapshot();
		if (!NpcTownManifest.TryGet("Terraria/Guide", out NpcTownIdentity guide) || !SettlementWorldState.TryGetResidence(guide.NpcKey, out SettlementResidenceReport residence))
			throw new InvalidOperationException("Guide residence unavailable for production OnKill coverage.");

		Dictionary<string, int> eligibleByMod = new(StringComparer.Ordinal);
		Dictionary<string, int> passedByMod = new(StringComparer.Ordinal);
		List<string> failures = new();
		int eligible = 0;
		int passed = 0;
		NpcQuestGlobalNPC hook = new();
		try
		{
			foreach (NPC sample in ContentSamples.NpcsByNetId.Values.Where(IsQuestKillEligible).OrderBy(QuestContentRegistry.ContentKey, StringComparer.Ordinal))
			{
				string provider = QuestContentRegistry.ContentMod(sample);
				if (provider == "Terraria")
					continue;
				eligible++;
				eligibleByMod[provider] = eligibleByMod.GetValueOrDefault(provider) + 1;
				if (ExerciseQuestKill(hook, questPlayer, player, guide, residence, sample, out string failure))
				{
					passed++;
					passedByMod[provider] = passedByMod.GetValueOrDefault(provider) + 1;
				}
				else
				{
					failures.Add($"{provider}/{sample.type}:{failure}");
				}
			}
		}
		finally
		{
			questPlayer.ReplaceCurrentSnapshot(saved);
			foreach ((string npcKey, ResidentQuestProgress progress) in saved)
				NpcQuestWorldLedger.Record(player, npcKey, progress);
			player.active = false;
			Main.npc[0].active = false;
		}

		bool verdict = eligible > 0 && passed == eligible && failures.Count == 0;
		DingDongBiomeHomes.Instance.Logger.Info($"DINGDONG_QUEST_KILL_HOOK_E2E|verdict={(verdict ? "PASS" : "FAIL")}|productionHook=NpcQuestGlobalNPC.OnKill|cases={passed}/{eligible}|providers={passedByMod.Count}/{eligibleByMod.Count}|failures={OneLine(string.Join(',', failures))}|syntheticPlayer=True|userSavesTouched=False");
		return new QuestHookSnapshot(verdict, eligible, passed, eligibleByMod, passedByMod);
	}
}
