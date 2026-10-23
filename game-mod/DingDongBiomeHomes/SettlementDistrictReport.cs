using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed class SettlementDistrictReport
{
	public int Index { get; }
	public HomeBiome Biome { get; }
	public Rectangle Envelope { get; }
	public int Structures { get; }
	public int Residents { get; }

	public SettlementDistrictReport(int index, HomeBiome biome, Rectangle envelope, int structures, int residents)
	{
		Index = index;
		Biome = biome;
		Envelope = envelope;
		Structures = structures;
		Residents = residents;
	}

	internal TagCompound Save() => new()
	{
		["Index"] = Index,
		["Biome"] = (int)Biome,
		["X"] = Envelope.X,
		["Y"] = Envelope.Y,
		["Width"] = Envelope.Width,
		["Height"] = Envelope.Height,
		["Structures"] = Structures,
		["Residents"] = Residents
	};
	internal static bool TryLoad(TagCompound tag, out SettlementDistrictReport report)
	{
		int biomeValue = tag.GetInt("Biome");
		if (!Enum.IsDefined(typeof(HomeBiome), biomeValue))
		{
			report = null;
			return false;
		}

		report = new SettlementDistrictReport(tag.GetInt("Index"), (HomeBiome)biomeValue, new Rectangle(tag.GetInt("X"), tag.GetInt("Y"), tag.GetInt("Width"), tag.GetInt("Height")), tag.GetInt("Structures"), tag.GetInt("Residents"));
		return true;
	}

	internal void NetSend(BinaryWriter writer)
	{
		writer.Write(Index);
		writer.Write((byte)Biome);
		writer.Write(Envelope.X);
		writer.Write(Envelope.Y);
		writer.Write(Envelope.Width);
		writer.Write(Envelope.Height);
		writer.Write(Structures);
		writer.Write(Residents);
	}

	internal static SettlementDistrictReport NetReceive(BinaryReader reader) => new(reader.ReadInt32(), (HomeBiome)reader.ReadByte(), new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()), reader.ReadInt32(), reader.ReadInt32());
}
