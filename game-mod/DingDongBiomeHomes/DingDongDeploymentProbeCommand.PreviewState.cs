using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Read-only dedicated-server contract gate for candidate placement metadata.
/// It never places a tile and cannot enable the approved worldgen manifest.
/// </summary>
public sealed partial class DingDongDeploymentProbeCommand
{
	private void RunPreviewStatus(CommandCaller caller, string worldFile)
	{
		const string prefix = "DINGDONG_DEPLOYMENT_PREVIEW_STATUS";
		if (!PreviewWorldState.PreviewPlaced)
		{
			ReplyFailure(caller, prefix, "preview-state-not-present");
			return;
		}

		if (!PreviewWorldState.LoadedFromSave)
		{
			ReplyFailure(caller, prefix, "preview-state-not-reloaded-from-save");
			return;
		}

		HousingProbeResult housing = AuthoredSettlementBuilder.ProbeHousing(PreviewWorldState.Area);
		if (housing.ValidRooms <= 0)
		{
			ReplyFailure(caller, prefix, $"persisted-preview-housing-failed:{housing.FirstFailure}");
			return;
		}

		string summary = $"{prefix}_END|verdict=PASS|world={worldFile}|npc={PreviewWorldState.NpcKey}|preset={PreviewWorldState.PresetId}|area={PreviewWorldState.Area.X},{PreviewWorldState.Area.Y},{PreviewWorldState.Area.Width},{PreviewWorldState.Area.Height}|savedValidRooms={PreviewWorldState.ValidRooms}|currentValidRooms={housing.ValidRooms}|loadedFromSave=True|playerVisualApproved=False|worldgenPlacementApproved=False|approved={NpcAuthoredPresetManifest.Count}/61|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}";
		Mod.Logger.Info(summary);
		caller.Reply(summary, Color.LightGreen);
	}

	private static AuthoredTileSnapshot[] SnapshotTiles(Rectangle area)
	{
		AuthoredTileSnapshot[] snapshot = new AuthoredTileSnapshot[checked(area.Width * area.Height)];
		for (int x = 0; x < area.Width; x++)
		{
			for (int y = 0; y < area.Height; y++)
			{
				snapshot[x * area.Height + y] = AuthoredTileSnapshot.Read(Main.tile[area.X + x, area.Y + y]);
			}
		}

		return snapshot;
	}

	private static void RestoreTiles(Rectangle area, AuthoredTileSnapshot[] snapshot)
	{
		for (int x = 0; x < area.Width; x++)
		{
			for (int y = 0; y < area.Height; y++)
				snapshot[x * area.Height + y].Restore(Main.tile[area.X + x, area.Y + y]);
		}
	}

	private void ReplyFailure(CommandCaller caller, string prefix, string failure)
	{
		string failed = $"{prefix}_END|verdict=FAIL|failure={failure}|approved={NpcAuthoredPresetManifest.Count}/61";
		Mod.Logger.Error(failed);
		caller.Reply(failed, Color.OrangeRed);
	}

	private static string NormalizeHexSignature(string value)
	{
		char[] normalized = new char[16];
		int count = 0;
		foreach (char character in value ?? string.Empty)
		{
			bool isHex = character is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';
			if (!isHex)
				continue;
			if (count >= normalized.Length)
				return string.Empty;
			normalized[count++] = char.ToUpperInvariant(character);
		}

		return count == normalized.Length ? new string (normalized) : string.Empty;
	}
}
