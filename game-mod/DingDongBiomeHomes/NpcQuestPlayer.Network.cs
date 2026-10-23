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
public sealed partial class NpcQuestPlayer
{
	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
	{
		if (Main.netMode == NetmodeID.Server && IdentityConfirmed && (toWho == Player.whoAmI || toWho == -1))
		{
			NpcQuestNetwork.SendQuestSnapshot(Player, Player.whoAmI);
			NpcResidenceWorldSystem.SendSnapshot(Player.whoAmI);
		}
	}

	private void SendIdentityHello()
	{
		NpcQuestNetwork.SendPlayerIdentity(PersistentId);
		nextIdentityRetryTick = Main.GameUpdateCount + IdentityRetryTicks;
	}
}
