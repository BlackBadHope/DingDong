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
	internal static MultiplayerAbProbeResult Run(Mod mod)
	{
		List<string> failures = new();
		int questPassed = 0;
		int movementPassed = 0;
		int itemPassed = 0;
		Player previousPlayer0 = Main.player[0];
		Player previousPlayer1 = Main.player[1];
		NPC previousNpc0 = Main.npc[0];
		NPC previousNpc1 = Main.npc[1];
		int previousMyPlayer = Main.myPlayer;
		int previousNetMode = Main.netMode;
		bool[] worldItemsBefore = CaptureActiveItems();
		NpcQuestWorldLedger ledger = new();
		NpcResidenceWorldSystem residences = new();
		TagCompound savedLedger = new();
		TagCompound savedResidences = new();
		ledger.SaveWorldData(savedLedger);
		residences.SaveWorldData(savedResidences);
		try
		{
			ledger.ClearWorld();
			residences.ClearWorld();
			if (!SettlementWorldState.HasCompleteResidenceRegistry)
				DingDongQuestLifecycleProbeCommand.BuildSyntheticResidenceRegistry(failures);
			if (!NpcTownManifest.TryGet("Terraria/Guide", out NpcTownIdentity identity) || !NpcResidentRegistry.TryGetType(identity.NpcKey, out int npcType) || !SettlementWorldState.TryGetResidence(identity.NpcKey, out SettlementResidenceReport residence))
			{
				throw new InvalidOperationException("Guide multiplayer probe fixture is unavailable.");
			}

			Vector2 center = new(residence.Area.Center.X * 16f, residence.Area.Center.Y * 16f);
			Player playerA = CreatePlayer(0, center, PlayerAIdentity);
			Player playerB = CreatePlayer(1, center + new Vector2(8f, 0f), PlayerBIdentity);
			Main.player[0] = playerA;
			Main.player[1] = playerB;
			Main.myPlayer = 0;
			NPC resident = new();
			DingDongQuestLifecycleProbeCommand.SetNpcDefaultsForEarlyProbe(resident, npcType, playerA, playerB);
			resident.whoAmI = 0;
			resident.active = true;
			resident.position = center + new Vector2(32f, 0f);
			Main.npc[0] = resident;
			RunQuestAb(playerA, playerB, resident, identity, ledger, failures, ref questPassed);
			RunMovementAb(playerA, playerB, resident, identity, failures, ref movementPassed);
			RunItemIsolation(playerA, playerB, failures, ref itemPassed);
		}
		catch (Exception exception)
		{
			Fail(failures, $"exception:{exception.GetType().Name}:{exception.Message}");
		}
		finally
		{
			Main.netMode = previousNetMode;
			foreach (Item item in NewWorldItems(worldItemsBefore))
				item.TurnToAir();
			ledger.LoadWorldData(savedLedger);
			residences.LoadWorldData(savedResidences);
			Main.player[0] = previousPlayer0;
			Main.player[1] = previousPlayer1;
			Main.npc[0] = previousNpc0;
			Main.npc[1] = previousNpc1;
			Main.myPlayer = previousMyPlayer;
		}

		bool passed = questPassed == QuestExpected && movementPassed == MovementExpected && itemPassed == ItemExpected && failures.Count == 0;
		MultiplayerAbProbeResult result = new(passed, questPassed, QuestExpected, movementPassed, MovementExpected, itemPassed, ItemExpected, failures);
		if (passed)
			mod.Logger.Info(result.ToMarker());
		else
			mod.Logger.Error(result.ToMarker());
		return result;
	}
}
