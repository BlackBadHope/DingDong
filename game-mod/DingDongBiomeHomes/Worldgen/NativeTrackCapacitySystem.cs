using System;
using DingDongBiomeHomes.Worldgen.Exact66Adapters;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// FindPath's inner tunnel loop may pass the outer loop's 4096-entry guard.
// X is monotonic along a track, so world width plus the native guard's padding
// bounds the scratch history. Requested min/max track lengths remain unchanged.
internal sealed class NativeTrackCapacitySystem : ModSystem
{
	private ILHook hook;
	public override void Load()
	{
		Type type = typeof(WorldGen).Assembly.GetType("Terraria.GameContent.Generation.TrackGenerator", true);
		hook = new ILHook(type.GetConstructor(Type.EmptyTypes)
			?? throw new MissingMethodException(type.FullName, ".ctor"), il =>
		{
			ILCursor cursor = new(il);
			if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdcI4(4096)) ||
				cursor.Next.OpCode != OpCodes.Newarr ||
				cursor.Next.Operand is not TypeReference element || element.Name != "TrackHistory")
				throw new InvalidOperationException("Native track history allocation changed.");
			cursor.EmitDelegate<Func<int, int>>(original => ContinentalAdapterMath.Active()
				? Math.Max(original, checked(Main.maxTilesX + 100)) : original);
		});
		Mod.Logger.Info("DINGDONG_NATIVE_TRACK_CAPACITY|installed=True|bound=width+100|requestedLengths=unchanged");
	}
	public override void Unload() { hook?.Dispose(); hook = null; }
}
