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
	internal static bool NetworkContractPass(out string failure)
	{
		NpcTownIdentity identity = NpcTownManifest.All[0];
		ResidentEscortState expected = new()
		{
			NpcKey = identity.NpcKey,
			Stage = ResidentEscortStage.Following,
			OwnerId = "00112233445566778899aabbccddeeff",
			OwnerPlayerIndex = -1,
			FlagPosition = new Point16(17, 23),
			OriginHome = new Point(101, 202),
			SpawnAnchor = new Point(211, 212),
			DestinationHome = new Point(303, 404),
			FailureReason = "contract-roundtrip",
			Revision = 42
		};
		using MemoryStream stream = new();
		using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
		{
			WriteState(writer, expected);
			writer.Flush();
		}

		stream.Position = 0;
		ResidentEscortState actual;
		using (BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
			actual = ReadState(reader);
		bool pass = stream.Position == stream.Length && string.Equals(actual.NpcKey, expected.NpcKey, StringComparison.Ordinal) && actual.Stage == expected.Stage && string.Equals(actual.OwnerId, expected.OwnerId, StringComparison.Ordinal) && actual.OwnerPlayerIndex == expected.OwnerPlayerIndex && actual.FlagPosition.Equals(expected.FlagPosition) && actual.OriginHome == expected.OriginHome && actual.SpawnAnchor == expected.SpawnAnchor && actual.DestinationHome == expected.DestinationHome && string.Equals(actual.FailureReason, expected.FailureReason, StringComparison.Ordinal) && actual.Revision == expected.Revision;
		if (!pass)
		{
			failure = $"escort-state-roundtrip:{stream.Position}/{stream.Length}";
			return false;
		}

		using MemoryStream truncated = new(new byte[] { 5, (byte)'x' });
		using BinaryReader truncatedReader = new(truncated, System.Text.Encoding.UTF8, leaveOpen: true);
		if (HasMinimumStateEnvelope(truncatedReader))
		{
			failure = "escort-truncated-envelope-accepted";
			return false;
		}

		failure = string.Empty;
		return true;
	}
}
