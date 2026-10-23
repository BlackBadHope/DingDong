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
	private static Player CreatePlayer(int index, Vector2 position, string identity)
	{
		Player player = new()
		{
			whoAmI = index,
			active = true,
			dead = false,
			position = position,
			statLife = 100,
			statLifeMax2 = 100,
			statMana = 20,
			statManaMax2 = 20
		};
		DingDongQuestLifecycleProbeCommand.EnsureProbeModPlayers(player);
		player.GetModPlayer<NpcQuestPlayer>().SetNetworkIdentity(identity);
		foreach (Item item in player.inventory)
			item.TurnToAir();
		return player;
	}
}
