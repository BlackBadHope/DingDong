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
	private static void RunMovementAb(Player playerA, Player playerB, NPC resident, NpcTownIdentity identity, List<string> failures, ref int passed)
	{
		ResidentEscortState state = NpcResidenceWorldSystem.GetState(identity.NpcKey);
		state.Stage = ResidentEscortStage.Anchored;
		state.OwnerId = string.Empty;
		state.OwnerPlayerIndex = -1;
		state.FlagPosition = new Point16(-1, -1);
		state.OriginHome = SettlementWorldState.TryGetResidence(identity.NpcKey, out SettlementResidenceReport probeResidence)
			? probeResidence.Area.Center
			: new Point(-1, -1);
		state.SpawnAnchor = new Point(-1, -1);
		state.DestinationHome = new Point(-1, -1);
		ResidentQuestProgress trust = playerA.GetModPlayer<NpcQuestPlayer>().Get(identity.NpcKey);
		trust.Active = false;
		trust.Completed = Math.Max(trust.Completed, ResidentTrustPolicy.RequiredQuests(identity));
		NpcResidenceWorldSystem.TryBeginEscort(playerA, resident, identity.NpcKey);
		Point capturedSpawn = new(Main.spawnTileX, Main.spawnTileY);
		Check(failures, "movement:escort-captures-world-spawn", state.SpawnAnchor == capturedSpawn, ref passed);
		Check(failures, "movement:first-leg-following-owned", state.Stage == ResidentEscortStage.Following && state.OwnerId == PlayerAIdentity && state.OwnerPlayerIndex == playerA.whoAmI, ref passed);
		Check(failures, "movement:spawn-radius-boundary", NpcResidenceWorldSystem.WithinSettlementRadius(new Point16(capturedSpawn.X + 1000, capturedSpawn.Y), capturedSpawn), ref passed);
		Check(failures, "movement:spawn-radius-outside-rejected", !NpcResidenceWorldSystem.WithinSettlementRadius(new Point16(capturedSpawn.X + 1001, capturedSpawn.Y), capturedSpawn), ref passed);
		Vector2 spawnWorld = capturedSpawn.ToWorldCoordinates(8f, 8f);
		playerA.Center = spawnWorld;
		resident.Center = spawnWorld;
		Check(failures, "movement:first-leg-completes-at-spawn", NpcResidenceWorldSystem.TryCompleteEscort(resident, identity.NpcKey, playerA) && state.Stage == ResidentEscortStage.AwaitingHome, ref passed);
		Check(failures, "movement:other-player-cannot-advance", !NpcResidenceWorldSystem.TryCompleteEscort(resident, identity.NpcKey, playerB) && state.Stage == ResidentEscortStage.AwaitingHome, ref passed);
		Check(failures, "movement:no-home-before-selection", state.FlagPosition.X < 0 && state.DestinationHome.X < 0, ref passed);
		CreatureTeleporterSystem teleporter = new();
		teleporter.ClearWorld();
		NPC creature = new();
		DingDongQuestLifecycleProbeCommand.SetNpcDefaultsForEarlyProbe(creature, NPCID.BlueSlime, playerA, playerB);
		creature.whoAmI = 1;
		creature.active = true;
		creature.position = resident.position + new Vector2(192f, 0f);
		creature.velocity = new Vector2(2f, -1f);
		Main.npc[1] = creature;
		Vector2 baseline = creature.position;
		teleporter.PostUpdateWorld();
		Check(failures, "movement:no-pulse-no-move", creature.position == baseline, ref passed);
		Check(failures, "movement:pulse-cardinality", CreatureTeleporterSystem.ShouldProcessPulseContract(NetmodeID.Server, 10, 10, 9, 2) && !CreatureTeleporterSystem.ShouldProcessPulseContract(NetmodeID.Server, 10, 10, 9, 0) && !CreatureTeleporterSystem.ShouldProcessPulseContract(NetmodeID.Server, 10, 10, 9, 1) && !CreatureTeleporterSystem.ShouldProcessPulseContract(NetmodeID.Server, 10, 10, 9, 3), ref passed);
		Check(failures, "movement:pulse-tick-dedup", !CreatureTeleporterSystem.ShouldProcessPulseContract(NetmodeID.Server, 9, 10, 8, 2) && !CreatureTeleporterSystem.ShouldProcessPulseContract(NetmodeID.Server, 10, 10, 10, 2), ref passed);
		if (!TryFindOpenPlatform(creature, out Point16 destination))
		{
			Fail(failures, "movement:no-open-destination");
			return;
		}

		int serverMode = Main.netMode;
		Main.netMode = NetmodeID.MultiplayerClient;
		Vector2 clientBefore = creature.position;
		bool clientMoved = CreatureTeleporterSystem.TryTeleportForProbe(creature, destination);
		Check(failures, "movement:client-authority-rejected", !clientMoved && creature.position == clientBefore, ref passed);
		Main.netMode = serverMode;
		creature.netUpdate = false;
		bool explicitTeleport = CreatureTeleporterSystem.TryTeleportForProbe(creature, destination);
		Check(failures, "movement:explicit-server-teleport", explicitTeleport && Vector2.DistanceSquared(creature.position, clientBefore) > 16f * 16f && creature.velocity == Vector2.Zero && creature.netUpdate, ref passed);
		Vector2 afterTeleport = creature.position;
		Check(failures, "movement:cooldown-no-bounce", !CreatureTeleporterSystem.TryTeleportForProbe(creature, destination) && creature.position == afterTeleport, ref passed);
		creature.boss = true;
		Check(failures, "movement:boss-rejected", !CreatureTeleporterSystem.TryTeleportForProbe(creature, destination) && creature.position == afterTeleport, ref passed);
		creature.boss = false;
		state.Stage = ResidentEscortStage.Following;
		state.OwnerId = PlayerAIdentity;
		state.OwnerPlayerIndex = 1;
		Player resolved = NpcResidenceWorldSystem.FindOwner(state);
		Check(failures, "movement:owner-routed-by-identity", ReferenceEquals(resolved, playerA) && state.OwnerPlayerIndex == 0 && !ReferenceEquals(resolved, playerB), ref passed);
		NpcEscortMobilityGlobalNPC mobility = new();
		Vector2 escortBefore = resident.position;
		mobility.OnSpawn(resident, null);
		_ = mobility.PreAI(resident);
		Check(failures, "movement:escort-velocity-only", resident.position == escortBefore, ref passed);
		playerA.active = false;
		state.OwnerPlayerIndex = 0;
		Check(failures, "movement:offline-owner-no-hijack", NpcResidenceWorldSystem.FindOwner(state)is null, ref passed);
		playerA.active = true;
	}
}
