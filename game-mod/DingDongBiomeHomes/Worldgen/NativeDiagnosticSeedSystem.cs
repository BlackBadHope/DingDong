using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

// FTW replaces the input seed with a random number inside GenerateWorld.
// Diagnostic replay must override AFTER that, retaining all special-world flags.
internal sealed class NativeDiagnosticSeedSystem : ModSystem
{
	private ILHook hook;
	public override void Load()
	{
		string text = Environment.GetEnvironmentVariable("DINGDONG_DIAGNOSTIC_WORLD_SEED");
		if (string.IsNullOrEmpty(text)) return;
		if (Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			!int.TryParse(text, out int seed) || seed < 0)
			throw new InvalidOperationException("Fixed world seed requires a valid diagnostic probe.");
		hook = new ILHook(typeof(WorldGen).GetMethod("GenerateWorld", new[] { typeof(int), typeof(GenerationProgress) }), il => {
			ILCursor cursor = new(il);
			if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdstr("Creating world - Seed: {0}, Width: {1}, Height: {2}, Evil: {3}, IsExpert: {4}")))
				throw new InvalidOperationException("Native seed initialization boundary changed.");
			cursor.EmitDelegate<Func<int>>(() => {
				if (!Main.dedServ || !(Main.ActiveWorldFileData?.Name ?? "").StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal))
					throw new InvalidOperationException("Fixed diagnostic seed cannot affect a non-probe world.");
				Mod.Logger.Info($"DINGDONG_DIAGNOSTIC_SEED|seed={seed}|FTW={Main.getGoodWorld}|flagsPreserved=True");
				return seed;
			});
			cursor.Emit(OpCodes.Starg, il.Method.Parameters[0]);
		});
	}
	public override void Unload() { hook?.Dispose(); hook = null; }
}
