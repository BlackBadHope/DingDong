using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DingDongBiomeHomes;

// Protocol 5 tail of the quest snapshot: the world quest clock and every recorded
// Dialogue-Choice outcome, so the book, the codex and the finales read the same
// world answers on every client.
internal static partial class NpcQuestNetwork
{
	private static void WriteWorldState(BinaryWriter writer)
	{
		writer.Write(NpcQuestWorldLedger.Clock);
		var choices = NpcQuestWorldLedger.Choices.OrderBy(p => p.Key, StringComparer.Ordinal).Take(NpcQuestWorldLedger.MaximumChoices).ToArray();
		writer.Write((ushort)choices.Length);
		foreach (var pair in choices) { writer.Write(pair.Key); writer.Write(pair.Value); }
	}

	private static void ReadWorldState(BinaryReader reader, out long clock, out List<KeyValuePair<string, string>> choices)
	{
		clock = reader.ReadInt64();
		int count = reader.ReadUInt16();
		if (clock < 0 || count > NpcQuestWorldLedger.MaximumChoices) throw new InvalidDataException("Invalid quest world state.");
		choices = new(count);
		for (int i = 0; i < count; i++)
		{
			string key = reader.ReadString(), id = reader.ReadString();
			int split = key.LastIndexOf(':');
			if (split <= 0 || key.Length > MaximumQuestStringLength || id.Length == 0 || id.Length > 64 || !NpcTownManifest.TryGet(key[..split], out _) || !int.TryParse(key[(split + 1)..], out int stage) || stage < 0)
				throw new InvalidDataException("Invalid quest choice entry.");
			choices.Add(new(key, id));
		}
	}

	private static string ReadResidentKey(BinaryReader reader)
	{
		string key = reader.ReadString();
		if (key.Length == 0 || key.Length > MaximumQuestStringLength || !NpcTownManifest.TryGet(key, out _))
			throw new InvalidDataException("Invalid resident key.");
		return key;
	}

	private static bool WorldStatePacketContractPass()
	{
		using MemoryStream stream = new();
		using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
			WriteWorldState(writer);
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		ReadWorldState(reader, out long clock, out var choices);
		return clock == NpcQuestWorldLedger.Clock && choices.Count == NpcQuestWorldLedger.Choices.Count && stream.Position == stream.Length;
	}
}
