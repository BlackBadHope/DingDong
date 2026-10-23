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
	private static void RunQuestAb(Player playerA, Player playerB, NPC resident, NpcTownIdentity identity, NpcQuestWorldLedger ledger, List<string> failures, ref int passed)
	{
		NpcQuestPlayer questA = playerA.GetModPlayer<NpcQuestPlayer>();
		NpcQuestPlayer questB = playerB.GetModPlayer<NpcQuestPlayer>();
		NpcQuestWorldLedger.BindPlayer(playerA, allowLocalImport: false);
		NpcQuestWorldLedger.BindPlayer(playerB, allowLocalImport: false);
		ResidentQuestProgress aAccepted, bAccepted;
		bool[] beforeDuplicate;
		passed = AcceptFirstPlayerQuestAndVerifyIsolation(playerA, resident, identity, failures, passed, questA, questB, out aAccepted, out bAccepted, out beforeDuplicate);
		Check(failures, "quest:duplicate-no-double-reward", aAccepted.Active && aAccepted.Completed == 1 && NewWorldItems(beforeDuplicate).Count == 0, ref passed);
		ResidentQuestProgress aBeforeInvalid = aAccepted.Clone();
		NpcQuestNetwork.HandleQuestInteractionForProbe(0, Main.maxNPCs, requestEscort: false);
		Check(failures, "quest:invalid-index-rejected", Equivalent(aAccepted, aBeforeInvalid), ref passed);
		ResidentQuestProgress repair;
		bool normalized;
		passed = CompleteSecondPlayerQuestAndVerifyPersistence(playerA, playerB, resident, identity, ledger, failures, passed, questA, questB, bAccepted, out repair, out normalized);
		Check(failures, "quest:missing-target-rebound", normalized && repair.Active && repair.ActiveGoal > 0 && QuestContentRegistry.TargetExists(repair.Definition(identity)), ref passed);
		Check(failures, "quest:duplicate-identity-rejected", NpcQuestNetwork.IdentityAvailableForPlayer(0, PlayerAIdentity) && !NpcQuestNetwork.IdentityAvailableForPlayer(1, PlayerAIdentity) && NpcQuestNetwork.IdentityAvailableForPlayer(1, PlayerBIdentity), ref passed);
		Check(failures, "quest:no-active-softlock", new[] { questA, questB }.All(quest => quest.CurrentActiveRecords().All(record => record.Progress.ActiveGoal > 0 && record.Progress.Progress <= record.Progress.ActiveGoal && NpcTownManifest.TryGet(record.NpcKey, out NpcTownIdentity owner) && QuestContentRegistry.TargetExists(record.Progress.Definition(owner)))), ref passed);
	}
}
