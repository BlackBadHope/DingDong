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
	public override void OnWorldUnload() => ClearWorld();
	public override void SaveWorldData(TagCompound tag)
	{
		tag["ResidenceEscortVersion"] = SaveVersion;
		tag["ResidenceEscortRevision"] = worldRevision;
		tag["ResidenceEscortStates"] = States.Values.Where(state => NpcTownManifest.TryGet(state.NpcKey, out _)).OrderBy(state => state.NpcKey, StringComparer.Ordinal).Select(SaveState).ToList();
	}

	public override void LoadWorldData(TagCompound tag)
	{
		ClearWorld();
		int version = tag.GetInt("ResidenceEscortVersion");
		if (version is < 1 or > SaveVersion || !tag.ContainsKey("ResidenceEscortStates"))
			return;
		worldRevision = Math.Max(0, tag.GetLong("ResidenceEscortRevision"));
		foreach (TagCompound stateTag in tag.GetList<TagCompound>("ResidenceEscortStates"))
		{
			ResidentEscortState state = LoadState(stateTag, version);
			if (!NpcTownManifest.TryGet(state.NpcKey, out _) || !Enum.IsDefined(typeof(ResidentEscortStage), state.Stage) || States.ContainsKey(state.NpcKey))
			{
				continue;
			}

			States.Add(state.NpcKey, state);
			worldRevision = Math.Max(worldRevision, state.Revision);
		}
	}

	public override void NetSend(BinaryWriter writer) => WriteSnapshot(writer);
	public override void NetReceive(BinaryReader reader) => ReadSnapshot(reader);
	private static void WriteSnapshot(BinaryWriter writer)
	{
		writer.Write(worldRevision);
		writer.Write((ushort)Math.Min(States.Count, NpcTownManifest.AuthoredCount));
		foreach (ResidentEscortState state in States.Values.OrderBy(state => state.NpcKey, StringComparer.Ordinal).Take(NpcTownManifest.AuthoredCount))
			WriteState(writer, state);
	}

	private static void ReadSnapshot(BinaryReader reader)
	{
		long revision = reader.ReadInt64();
		int count = reader.ReadUInt16();
		if (count > NpcTownManifest.AuthoredCount)
			throw new InvalidDataException("Escort snapshot exceeds the canonical resident count.");
		Dictionary<string, ResidentEscortState> replacement = new(StringComparer.Ordinal);
		for (int index = 0; index < count; index++)
		{
			ResidentEscortState state = ReadState(reader);
			if (!NpcTownManifest.TryGet(state.NpcKey, out _) || !replacement.TryAdd(state.NpcKey, state))
				throw new InvalidDataException("Escort snapshot contains an invalid or duplicate resident.");
		}

		States.Clear();
		foreach ((string key, ResidentEscortState state)in replacement)
			States.Add(key, state);
		worldRevision = Math.Max(0, revision);
	}

	private static TagCompound SaveState(ResidentEscortState state) => new()
	{
		["NpcKey"] = state.NpcKey,
		["Stage"] = (int)state.Stage,
		["OwnerId"] = state.OwnerId,
		["OwnerPlayerIndex"] = -1,
		["FlagX"] = (int)state.FlagPosition.X,
		["FlagY"] = (int)state.FlagPosition.Y,
		["OriginX"] = state.OriginHome.X,
		["OriginY"] = state.OriginHome.Y,
		["SpawnX"] = state.SpawnAnchor.X,
		["SpawnY"] = state.SpawnAnchor.Y,
		["DestinationX"] = state.DestinationHome.X,
		["DestinationY"] = state.DestinationHome.Y,
		["FailureReason"] = state.FailureReason,
		["Revision"] = state.Revision
	};
}
