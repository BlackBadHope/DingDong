using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using DingDongBiomeHomes.Items.SynergyArmor;

namespace DingDongBiomeHomes.Items.MobRelics;
public sealed partial class MobRelicPlayer
{
	private void SyncVitals()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
	}
}
