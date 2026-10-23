using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	internal static void ProbeRestoredContext(Mod mod, AuthoredPreset preset, AuthoredDeploymentProfile profile, Point origin)
	{
		if (preset.ResourcePath != AuthoredSourceContext.JungleResource) return;
		StructureData data = GetOrLoad(mod, preset.ResourcePath);
		StructureData original = GetOrLoad(mod, "Structures/PrivateHome/Greaveil/jungle-east-house.shstruct");
		StructureWritePlan plan = GetWritePlan(data, preset.ResourcePath, profile);
		if (plan.TerrainAnchorY != AuthoredSourceContext.JungleTerrainAnchorY)
			throw new InvalidOperationException("Restored source context moved the original terrain anchor.");
		int coreCells = 0, treeCells = 0, furnitureCells = 0;
		for (int x = 0; x < original.Width; x++)
		for (int y = 0; y < original.Height; y++)
		{
			int oldIndex = x * original.Height + y;
			int newIndex = (x + AuthoredSourceContext.ReferenceX) * data.Height + y + AuthoredSourceContext.ReferenceY;
			if (!Same(original.TileTypes, data.TileTypes, 2) || !Same(original.WallTypes, data.WallTypes, 2) ||
				!Same(original.Liquids, data.Liquids, 2) || !Same(original.BrightnessAndInvisibility, data.BrightnessAndInvisibility, 1) ||
				!Same(original.WireState, data.WireState, 8))
				throw new InvalidOperationException("Restored context changed an original core cell.");
			coreCells++;
			bool Same(byte[] a, byte[] b, int size) => a.AsSpan(oldIndex * size, size).SequenceEqual(b.AsSpan(newIndex * size, size));
		}
		for (int x = 0; x < data.Width; x++)
		for (int y = 0; y < data.Height; y++)
		{
			SourceCell source = ReadSourceCell(data, x * data.Height + y);
			if (!source.HasTile || source.TileType is not (383 or 384 or 79 or 101 or 88)) continue;
			Tile actual = Main.tile[origin.X + x, origin.Y + y];
			if (!actual.HasTile || actual.TileType != source.TileType)
				throw new InvalidOperationException($"Restored tree/capsule object missing after native framing at {x},{y}: {source.TileType}.");
			if (source.TileType is 383 or 384) treeCells++; else furnitureCells++;
		}
		RequireNativeContainer(preset, origin, requireEmpty: true);
		mod.Logger.Info($"DINGDONG_SOURCE_CONTEXT_PROBE|verdict=PASS|coreCells={coreCells}|planes=5|treeCells={treeCells}|furnitureCells={furnitureCells}|anchor={plan.TerrainAnchorY}|dresserRegistered=True|visualApproval=False");
	}
}
