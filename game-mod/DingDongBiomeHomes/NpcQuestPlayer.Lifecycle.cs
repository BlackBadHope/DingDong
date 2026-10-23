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
	public override void Initialize()
	{
		if (PersistentId.Length == 0)
			PersistentId = Guid.NewGuid().ToString("N");
		identityConfirmed = Main.netMode == NetmodeID.SinglePlayer;
		nextIdentityRetryTick = 0;
		Revision = 0;
	}

	public override void OnEnterWorld()
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			identityConfirmed = true;
			NpcQuestWorldLedger.BindPlayer(Player, allowLocalImport: true);
			return;
		}

		if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
		{
			identityConfirmed = false;
			SendIdentityHello();
		}
	}

	public override void PlayerDisconnect()
	{
		if (Main.netMode == NetmodeID.Server)
		{
			NpcResidenceWorldSystem.HandlePlayerDisconnected(Player.whoAmI);
			identityConfirmed = false;
		}
	}

	public override void PostUpdate()
	{
		if (Main.gameMenu)
			return;
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			if (Player.whoAmI == Main.myPlayer && !identityConfirmed && Main.GameUpdateCount >= nextIdentityRetryTick)
				SendIdentityHello();
			return;
		}

		if (!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || (Main.GameUpdateCount + (ulong)Player.whoAmI) % 15UL != 0UL)
		{
			return;
		}

		NpcQuestSystem.UpdateLocationProgress(Player);
	}
}
