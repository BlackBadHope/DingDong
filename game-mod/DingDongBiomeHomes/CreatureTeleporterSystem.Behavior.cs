using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class CreatureTeleporterSystem
{
	internal static void RecordPulse(int i, int j)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || !WorldGen.InWorld(i, j, 2))
			return;
		ulong tick = Main.GameUpdateCount;
		if (pulseTick != tick)
		{
			PulsePlatforms.Clear();
			pulseTick = tick;
			processedTick = ulong.MaxValue;
		}

		Tile tile = Framing.GetTileSafely(i, j);
		int column = tile.TileFrameX / 18 % 3;
		PulsePlatforms.Add(new Point16(i - column, j));
	}

	internal static bool IsEligible(NPC npc)
	{
		DingDongServerConfig config = ModContent.GetInstance<DingDongServerConfig>();
		// Discovery/escort cannot be skipped with the pack's creature teleporter.
		// Arrived residents have completed both physical legs and may use it.
		if (config.EnableNpcQuestsAndEscort && NpcResidentRegistry.TryGet(npc, out string npcKey, out _) &&
			NpcResidenceWorldSystem.GetState(npcKey).Stage != ResidentEscortStage.Arrived)
			return false;
		return IsEligibleContract(npc.active, npc.boss, npc.type >= NPCID.None && npc.type < NPCID.Sets.ShouldBeCountedAsBoss.Length && NPCID.Sets.ShouldBeCountedAsBoss[npc.type], npc.realLife, npc.aiStyle, npc.width, npc.height, config.MaximumTeleportedCreatureWidthTiles, config.MaximumTeleportedCreatureHeightTiles) && npc.type != NPCID.TargetDummy;
	}

	private static List<int> CaptureEligibleNpcs(Point16 platform)
	{
		Rectangle capture = new(platform.X * 16, platform.Y * 16 - 64, 48, 72);
		List<int> result = new();
		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (!capture.Intersects(npc.Hitbox) || !IsEligible(npc) || CooldownByNpc.TryGetValue(npc.whoAmI, out ulong previous) && Main.GameUpdateCount - previous < TeleportCooldownTicks)
			{
				continue;
			}

			result.Add(npc.whoAmI);
		}

		return result;
	}

	private static bool TryTeleport(NPC npc, Point16 destinationPlatform)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || !npc.active || !IsEligible(npc) || CooldownByNpc.TryGetValue(npc.whoAmI, out ulong previous) && Main.GameUpdateCount - previous < TeleportCooldownTicks || !TryFindDestination(npc, destinationPlatform, out Vector2 destination))
			return false;
		npc.Teleport(destination, TeleportationStyleID.TeleporterTile);
		npc.velocity = Vector2.Zero;
		npc.netUpdate = true;
		CooldownByNpc[npc.whoAmI] = Main.GameUpdateCount;
		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 1, npc.whoAmI, destination.X, destination.Y, TeleportationStyleID.TeleporterTile);
		}

		return true;
	}
}
