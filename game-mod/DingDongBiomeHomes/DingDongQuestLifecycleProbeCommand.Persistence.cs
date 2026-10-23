using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class DingDongQuestLifecycleProbeCommand
{
	internal static string VerifyPersisted(Mod mod, string worldFile)
	{
		List<string> failures = new();
		Player player = Main.player[SyntheticPlayerIndex];
		player.active = false;
		player.dead = false;
		player.whoAmI = SyntheticPlayerIndex;
		EnsureProbeModPlayers(player);
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		questPlayer.SetNetworkIdentity(ProbeIdentity);
		NpcQuestWorldLedger.BindPlayer(player, allowLocalImport: false);
		int records = 0;
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			int expectedCompleted = string.Equals(identity.NpcKey, "Terraria/Guide", StringComparison.Ordinal) ? 6 : 1;
			bool valid = NpcQuestWorldLedger.TryRead(ProbeIdentity, identity.NpcKey, out ResidentQuestProgress ledgerProgress) && !ledgerProgress.Active && ledgerProgress.Completed == expectedCompleted;
			Expect(failures, $"persisted:{identity.NpcKey}", valid);
			if (valid)
				records++;
		}

		bool playerReadback = questPlayer.CurrentSnapshot().Count == NpcTownManifest.AuthoredCount && questPlayer.CurrentCompletedTotal() == NpcTownManifest.AuthoredCount + 5;
		Expect(failures, "player-readback", playerReadback);
		string verdict = failures.Count == 0 ? "PASS" : "FAIL";
		string summary = $"DINGDONG_QUEST_PERSISTENCE_RELOAD|verdict={verdict}|world={OneLine(worldFile)}|ledgerRecords={records}/{NpcTownManifest.AuthoredCount}|completedTotal={questPlayer.CurrentCompletedTotal()}/{NpcTownManifest.AuthoredCount + 5}|playerReadback={playerReadback}|failures={OneLine(string.Join(',', failures))}|userSavesTouched=False";
		mod.Logger.Info(summary);
		return summary;
	}

	private static void Expect(List<string> failures, string name, bool condition)
	{
		if (!condition)
			failures.Add(name);
	}
}
