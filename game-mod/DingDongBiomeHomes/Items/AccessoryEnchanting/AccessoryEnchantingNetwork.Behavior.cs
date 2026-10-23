using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static partial class AccessoryEnchantingNetwork
{
	private static void Execute(Player player, AccessoryStationAction action, out bool success, out string failure)
	{
		AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
		success = action switch
		{
			AccessoryStationAction.Upgrade => AccessoryEnchantingService.TryUpgrade(player, station.Target, charge: true, out failure),
			AccessoryStationAction.Fuse => AccessoryEnchantingService.TryFuse(player, station.Target, station.Donor, charge: true, out failure),
			_ => throw new InvalidDataException("Unknown accessory station action.")};
	}

	internal static bool TalkingToGoblin(Player player)
	{
		if (player == null || !player.active || player.dead || player.talkNPC < 0 || player.talkNPC >= Main.maxNPCs)
			return false;
		NPC goblin = Main.npc[player.talkNPC];
		return goblin.active && goblin.type == NPCID.GoblinTinkerer && Vector2.DistanceSquared(player.Center, goblin.Center) <= 240f * 240f;
	}
	private static bool AssignedPlayer(int playerIndex) => playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex] != null;
	private static bool ValidPlayer(int playerIndex) => AssignedPlayer(playerIndex) && Main.player[playerIndex].active;
}
