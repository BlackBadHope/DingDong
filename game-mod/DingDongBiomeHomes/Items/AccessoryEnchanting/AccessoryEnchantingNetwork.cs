using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static partial class AccessoryEnchantingNetwork
{
	internal const byte SlotSyncPacket = 69;
	internal const byte ActionRequestPacket = 70;
	internal const byte ActionResultPacket = 71;
	internal const byte SlotSwapRequestPacket = 72;
}
