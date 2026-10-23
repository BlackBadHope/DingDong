using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// A slope is defined from four neighbors. Outside that domain there is no
// valid operation; never wrap coordinates, invent neighbors or catch exceptions.
internal sealed class NativeSlopeBoundarySystem : ModSystem
{
	private ILHook hook;
	public override void Load()
	{
		hook = new ILHook(typeof(Tile).GetMethod("SmoothSlope", new[] {
			typeof(int), typeof(int), typeof(bool), typeof(bool) }), il => {
			ILCursor cursor = new(il);
			var original = cursor.Next;
			cursor.Emit(OpCodes.Ldarg_0); cursor.Emit(OpCodes.Ldarg_1);
			cursor.EmitDelegate<Func<int, int, bool>>((x, y) =>
				x > 0 && y > 0 && x < Main.maxTilesX - 1 && y < Main.maxTilesY - 1);
			cursor.Emit(OpCodes.Brtrue, original);
			cursor.Emit(OpCodes.Ret);
		});
		Mod.Logger.Info("DINGDONG_NATIVE_SLOPE_BOUNDARY|installed=True|interior=original|outermostTiles=no-op");
	}
	internal static void Probe()
	{
		foreach (int x in new[] { -1, 0, Main.maxTilesX - 1, Main.maxTilesX })
			Tile.SmoothSlope(x, Main.maxTilesY / 2, true, false);
		foreach (int y in new[] { -1, 0, Main.maxTilesY - 1, Main.maxTilesY })
			Tile.SmoothSlope(Main.maxTilesX / 2, y, true, false);
		Console.WriteLine("DINGDONG_SLOPE_BOUNDARY_PROBE|calls=8|invalidNeighborhoods=no-op|worldMutated=False");
	}
	public override void Unload() { hook?.Dispose(); hook = null; }
}
