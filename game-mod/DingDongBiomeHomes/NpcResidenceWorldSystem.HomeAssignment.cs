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
	private static bool TryEnsureOrigin(ResidentEscortState state)
	{
		if (state.OriginHome.X > 0 && state.OriginHome.Y > 0)
			return true;
		if (!NpcResidentRegistry.TryGetType(state.NpcKey, out int npcType) || !SettlementWorldState.TryGetResidence(state.NpcKey, out SettlementResidenceReport report))
		{
			return false;
		}

		OriginScans.Enqueue(state.NpcKey, state,
			new OriginScanRegion(report.Area.Left, report.Area.Top, report.Area.Width, report.Area.Height),
			npcType, Main.GameUpdateCount);
		return false;
	}

	private static bool IsRoomVacant(Rectangle room, int exceptNpcType, string exceptNpcKey)
	{
		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (!npc.townNPC || npc.homeless || npc.type == exceptNpcType)
				continue;
			if (room.Contains(npc.homeTileX, npc.homeTileY))
				return false;
		}

		foreach (ResidentEscortState state in States.Values)
		{
			if (state.Stage is not (ResidentEscortStage.Arrived or ResidentEscortStage.Following or ResidentEscortStage.Waiting) ||
				!HasAssignedDestination(state) || string.Equals(state.NpcKey, exceptNpcKey, StringComparison.Ordinal))
				continue;
			if (room.Contains(state.DestinationHome))
				return false;
		}

		return true;
	}

	private static void ApplyHome(NPC npc, Point home)
	{
		npc.homeless = false;
		npc.homeTileX = home.X;
		npc.homeTileY = home.Y;
	}

	private static void Touch(ResidentEscortState state) => state.Revision = ++worldRevision;
	private static bool HasMinimumStateEnvelope(BinaryReader reader)
	{
		Stream stream = reader.BaseStream;
		return !stream.CanSeek || stream.Length - stream.Position >= MinimumEscortStateEnvelopeBytes;
	}
}
