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
	private void RunRelocationVerify(CommandCaller caller, string worldFile, string[] args)
	{
		const string prefix = "DINGDONG_CHEST_RELOCATION_VERIFY";
		if (!short.TryParse(args[1], out short sourceX) || !short.TryParse(args[2], out short sourceY) || !short.TryParse(args[3], out short destinationX) || !short.TryParse(args[4], out short destinationY))
		{
			ReplyFailure(caller, prefix, "invalid-coordinates");
			return;
		}

		string signature = NormalizeHexSignature(args[5]);
		if (signature.Length != 16)
		{
			ReplyFailure(caller, prefix, "invalid-signature-format");
			return;
		}

		Point16 source = new(sourceX, sourceY);
		Point16 destination = new(destinationX, destinationY);
		if (!PersistentChestRelocator.TryVerify(source, destination, signature, out string failure))
		{
			ReplyFailure(caller, prefix, failure);
			return;
		}

		int destinationIndex = Chest.FindChest(destination.X, destination.Y);
		string verified = $"{prefix}_END|verdict=PASS|world={worldFile}|source={source.X},{source.Y}|destination={destination.X},{destination.Y}|destinationIndex={destinationIndex}|signature={signature}|sourceAbsent=True|destinationPresent=True|approved={NpcAuthoredPresetManifest.Count}/61|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}";
		Mod.Logger.Info(verified);
		caller.Reply(verified, Color.LightGreen);
	}
}
