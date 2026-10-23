using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class NpcResidenceWorldSystem
{
	private static ResidentEscortState LoadState(TagCompound tag, int version) => new()
	{
		NpcKey = tag.GetString("NpcKey"),
		Stage = version == 1 && tag.GetInt("Stage") == 5 ? ResidentEscortStage.Arrived : (ResidentEscortStage)tag.GetInt("Stage"),
		OwnerId = tag.GetString("OwnerId"),
		OwnerPlayerIndex = -1,
		FlagPosition = new Point16(tag.GetInt("FlagX"), tag.GetInt("FlagY")),
		OriginHome = new Point(tag.GetInt("OriginX"), tag.GetInt("OriginY")),
		SpawnAnchor = version >= 2 ? new Point(tag.GetInt("SpawnX"), tag.GetInt("SpawnY")) : new Point(Main.spawnTileX, Main.spawnTileY),
		DestinationHome = new Point(tag.GetInt("DestinationX"), tag.GetInt("DestinationY")),
		FailureReason = tag.GetString("FailureReason"),
		Revision = Math.Max(0, tag.GetLong("Revision"))
	};
	private static void WriteState(BinaryWriter writer, ResidentEscortState state)
	{
		writer.Write(state.NpcKey);
		writer.Write((byte)state.Stage);
		writer.Write(state.OwnerId);
		writer.Write((short)state.OwnerPlayerIndex);
		writer.Write(state.FlagPosition.X);
		writer.Write(state.FlagPosition.Y);
		writer.Write(state.OriginHome.X);
		writer.Write(state.OriginHome.Y);
		writer.Write(state.SpawnAnchor.X);
		writer.Write(state.SpawnAnchor.Y);
		writer.Write(state.DestinationHome.X);
		writer.Write(state.DestinationHome.Y);
		writer.Write(state.FailureReason);
		writer.Write(state.Revision);
	}

	private static ResidentEscortState ReadState(BinaryReader reader)
	{
		if (!HasMinimumStateEnvelope(reader))
			throw new EndOfStreamException("Escort state envelope is truncated.");
		string npcKey = reader.ReadString();
		ResidentEscortStage stage = (ResidentEscortStage)reader.ReadByte();
		string ownerId = reader.ReadString();
		int ownerPlayerIndex = reader.ReadInt16();
		Point16 flagPosition = new(reader.ReadInt16(), reader.ReadInt16());
		Point originHome = new(reader.ReadInt32(), reader.ReadInt32());
		Point spawnAnchor = new(reader.ReadInt32(), reader.ReadInt32());
		Point destinationHome = new(reader.ReadInt32(), reader.ReadInt32());
		string failureReason = reader.ReadString();
		long revision = reader.ReadInt64();
		if (!Enum.IsDefined(typeof(ResidentEscortStage), stage))
			throw new InvalidDataException("Invalid escort stage.");
		if (npcKey.Length == 0 || npcKey.Length > MaximumEscortStringLength)
			throw new InvalidDataException("Invalid escort resident key.");
		if (ownerId.Length > MaximumEscortStringLength || (ownerId.Length != 0 && !Guid.TryParseExact(ownerId, "N", out _)))
		{
			throw new InvalidDataException("Invalid escort owner identity.");
		}

		if (failureReason.Length > MaximumEscortStringLength)
			throw new InvalidDataException("Invalid escort failure reason.");
		if (ownerPlayerIndex < -1 || ownerPlayerIndex >= Main.maxPlayers)
			throw new InvalidDataException("Invalid escort owner player index.");
		return new ResidentEscortState
		{
			NpcKey = npcKey,
			Stage = stage,
			OwnerId = ownerId,
			OwnerPlayerIndex = ownerPlayerIndex,
			FlagPosition = flagPosition,
			OriginHome = originHome,
			SpawnAnchor = spawnAnchor,
			DestinationHome = destinationHome,
			FailureReason = failureReason,
			Revision = Math.Max(0, revision)
		};
	}
}
