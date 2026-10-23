using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static partial class NpcQuestNetwork
{
	internal static bool HasConnectedClients() => Main.netMode == NetmodeID.Server && Enumerable.Range(0, Main.maxPlayers).Any(CanSendToClient);
	internal static bool IdentityAvailableForPlayer(int playerIndex, string persistentId)
	{
		if (playerIndex < 0 || playerIndex >= Main.maxPlayers || !Guid.TryParseExact(persistentId, "N", out _))
			return false;
		for (int index = 0; index < Main.maxPlayers; index++)
		{
			if (index == playerIndex)
				continue;
			Player other = Main.player[index];
			if (other?.active == true)
			{
				NpcQuestPlayer otherQuest = other.GetModPlayer<NpcQuestPlayer>();
				if (otherQuest.IdentityConfirmed && string.Equals(otherQuest.PersistentId, persistentId, StringComparison.Ordinal))
					return false;
			}
		}

		return true;
	}

	private static bool ValidPlayer(int whoAmI) => whoAmI >= 0 && whoAmI < Main.maxPlayers && Main.player[whoAmI].active;
}
