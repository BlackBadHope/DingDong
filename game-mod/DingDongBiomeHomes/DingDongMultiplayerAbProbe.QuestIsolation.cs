using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using DingDongBiomeHomes.Items.MobRelics;
using DingDongBiomeHomes.Items.SynergyArmor;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static partial class DingDongMultiplayerAbProbe
{
	private static int CompleteSecondPlayerQuestAndVerifyPersistence(Player playerA, Player playerB, NPC resident, NpcTownIdentity identity, NpcQuestWorldLedger ledger, List<string> failures, int passed, NpcQuestPlayer questA, NpcQuestPlayer questB, ResidentQuestProgress bAccepted, out ResidentQuestProgress repair, out bool normalized)
	{
		DingDongQuestLifecycleProbeCommand.PrepareObjective(playerB, bAccepted, bAccepted.Definition(identity));
		NpcQuestWorldLedger.Record(playerB, identity.NpcKey, bAccepted);
		bool[] beforeBReward = CaptureActiveItems();
		NpcQuestNetwork.HandleQuestInteractionForProbe(1, resident.whoAmI, requestEscort: false);
		Check(failures, "quest:B-complete", !bAccepted.Active && bAccepted.Completed == 1 && NewWorldItems(beforeBReward).Count > 0, ref passed);
		bool ledgersIsolated = NpcQuestWorldLedger.TryRead(PlayerAIdentity, identity.NpcKey, out ResidentQuestProgress ledgerA) && NpcQuestWorldLedger.TryRead(PlayerBIdentity, identity.NpcKey, out ResidentQuestProgress ledgerB) && ledgerA.Active && ledgerA.Completed == 1 && !ledgerB.Active && ledgerB.Completed == 1;
		Check(failures, "quest:ledger-owner-isolation", ledgersIsolated, ref passed);
		TagCompound twoPlayerSave = new();
		ledger.SaveWorldData(twoPlayerSave);
		questA.ReplaceCurrentSnapshot(Array.Empty<(string, ResidentQuestProgress)>());
		questB.ReplaceCurrentSnapshot(Array.Empty<(string, ResidentQuestProgress)>());
		ledger.LoadWorldData(twoPlayerSave);
		NpcQuestWorldLedger.BindPlayer(playerA, allowLocalImport: false);
		NpcQuestWorldLedger.BindPlayer(playerB, allowLocalImport: false);
		Check(failures, "quest:two-player-save-load", questA.Get(identity.NpcKey).Active && questA.Get(identity.NpcKey).Completed == 1 && !questB.Get(identity.NpcKey).Active && questB.Get(identity.NpcKey).Completed == 1, ref passed);
		repair = new()
		{
			Completed = 2
		};
		repair.Begin(new ResidentQuestDefinition(999_999, ResidentQuestKind.ModProgression, 0, ItemID.None, 1, 0, "milestone:MissingProvider/removed", "Removed target", 100), 100, 0);
		normalized = repair.NormalizeLoaded(identity, PlayerAIdentity);
		return passed;
	}
}
