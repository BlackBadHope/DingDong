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
	private static AccessoryStationSlot ReadSlot(BinaryReader reader)
	{
		AccessoryStationSlot slot = (AccessoryStationSlot)reader.ReadByte();
		return Enum.IsDefined(slot) ? slot : throw new InvalidDataException("Unknown accessory station slot.");
	}
}
