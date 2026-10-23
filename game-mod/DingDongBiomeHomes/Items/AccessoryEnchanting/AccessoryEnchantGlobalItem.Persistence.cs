using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public sealed partial class AccessoryEnchantGlobalItem
{
	public override void SaveData(Item item, TagCompound tag)
	{
		if (Level > 0)
			tag[LevelKey] = (int)Level;
		if (Imprint != AccessoryEnchantmentId.None)
			tag[ImprintKey] = (int)Imprint;
		if (fused.Count > 0)
			tag[FusedKey] = fused.Select(value => (int)value).ToList();
	}

	public override void LoadData(Item item, TagCompound tag)
	{
		Level = (byte)Math.Clamp(tag.ContainsKey(LevelKey) ? tag.GetInt(LevelKey) : 0, 0, MaxLevel);
		AccessoryEnchantmentId loadedImprint = (AccessoryEnchantmentId)(tag.ContainsKey(ImprintKey) ? tag.GetInt(ImprintKey) : 0);
		Imprint = AccessoryEnchantmentCatalog.IsValid(loadedImprint) ? loadedImprint : AccessoryEnchantmentId.None;
		fused = tag.ContainsKey(FusedKey) ? tag.GetList<int>(FusedKey).Select(value => (AccessoryEnchantmentId)value).Where(AccessoryEnchantmentCatalog.IsValid).Distinct().Take(Level).ToList() : new List<AccessoryEnchantmentId>();
	}

	public override void NetSend(Item item, BinaryWriter writer)
	{
		writer.Write(Level);
		writer.Write((byte)Imprint);
		writer.Write((byte)fused.Count);
		foreach (AccessoryEnchantmentId value in fused)
			writer.Write((byte)value);
	}

	public override void NetReceive(Item item, BinaryReader reader)
	{
		Level = (byte)Math.Min(reader.ReadByte(), MaxLevel);
		AccessoryEnchantmentId receivedImprint = (AccessoryEnchantmentId)reader.ReadByte();
		Imprint = AccessoryEnchantmentCatalog.IsValid(receivedImprint) ? receivedImprint : AccessoryEnchantmentId.None;
		int declaredCount = reader.ReadByte();
		fused = new List<AccessoryEnchantmentId>(Math.Min(declaredCount, Level));
		for (int index = 0; index < declaredCount; index++)
		{
			AccessoryEnchantmentId value = (AccessoryEnchantmentId)reader.ReadByte();
			if (fused.Count < Level && AccessoryEnchantmentCatalog.IsValid(value) && !fused.Contains(value))
				fused.Add(value);
		}
	}
}
