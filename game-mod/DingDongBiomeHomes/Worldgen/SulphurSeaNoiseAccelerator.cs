using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

// Worldgen speed, owner GO 30.10 (tier 1, identical world): in "Sulphur Sea" Calamity spends
// ~22 s in GenerateSpaghettiWaterCaves and ~14 s in DecideHardSandstoneLine on x10. Both walk the
// biome cell by cell, evaluate FractalBrownianMotion (a pure read of NoiseHelper's static noise)
// and then edit one cell. The noise test is evaluated here on all cores; the edits stay
// sequential in the original order with the original genRand calls, so the world is the same.
//
// DINGDONG_SS1_NOISE: "fast" replaces both methods, "shadow" lets Calamity run and compares its
// edit points and final genRand state with the plan, "off" (default until verified) does nothing.
internal sealed class SulphurSeaNoiseAccelerator : ModSystem
{
	private delegate void Original();
	private static string mode = "off";
	private static Func<int> yStart, biomeWidth, blockDepth;
	private static Func<int, int> actualX;
	private static Func<float, float, int, int, float, float, float> fbm;
	private static float[] spaghettiThresholds;
	private static ushort unsafeSandWall, sand, hardSandstone, hardSandstoneWall;
	private static bool typesResolved;
	[ThreadStatic] private static List<Point> recorded;
	private static readonly FieldInfo seedArray = typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly FieldInfo inext = typeof(UnifiedRandom).GetField("inext", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly FieldInfo inextp = typeof(UnifiedRandom).GetField("inextp", BindingFlags.Instance | BindingFlags.NonPublic);

	public override void Load()
	{
		mode = (Environment.GetEnvironmentVariable("DINGDONG_SS1_NOISE") ?? "off").Trim().ToLowerInvariant();
		if (mode != "fast" && mode != "shadow") { mode = "off"; return; }
		if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity)) { mode = "off"; return; }
		try
		{
			const BindingFlags any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			Type sea = calamity.Code.GetType("CalamityMod.World.SulphurousSea", true);
			yStart = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), sea.GetProperty("YStart", any).GetGetMethod(true));
			biomeWidth = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), sea.GetProperty("BiomeWidth", any).GetGetMethod(true));
			blockDepth = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), sea.GetProperty("BlockDepth", any).GetGetMethod(true));
			actualX = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), sea.GetMethod("GetActualX", any));
			fbm = (Func<float, float, int, int, float, float, float>)Delegate.CreateDelegate(typeof(Func<float, float, int, int, float, float, float>), sea.GetMethod("FractalBrownianMotion", any));
			spaghettiThresholds = (float[])sea.GetField("SpaghettiCaveCarveOutThresholds", any).GetValue(null);
			if (seedArray == null || inext == null || inextp == null) throw new MissingFieldException("UnifiedRandom state");
			MonoModHooks.Add(sea.GetMethod("GenerateSpaghettiWaterCaves", any), (Action<Original>)Spaghetti);
			MonoModHooks.Add(sea.GetMethod("DecideHardSandstoneLine", any), (Action<Original>)HardSandstone);
			if (mode == "shadow")
				MonoModHooks.Add(typeof(WorldUtils).GetMethod(nameof(WorldUtils.Gen), new[] { typeof(Point), typeof(GenShape), typeof(GenAction) }),
					(Func<Func<Point, GenShape, GenAction, bool>, Point, GenShape, GenAction, bool>)RecordGen);
			Mod.Logger.Info($"DINGDONG_SS1_NOISE|state=ARMED|mode={mode}|thresholds={spaghettiThresholds.Length}");
		}
		catch (Exception error)
		{
			mode = "off";
			Mod.Logger.Warn("DINGDONG_SS1_NOISE|state=DISABLED|reason=" + error.Message);
		}
	}

	public override void Unload()
	{
		yStart = biomeWidth = blockDepth = null; actualX = null; fbm = null; spaghettiThresholds = null; typesResolved = false;
	}

	private static bool ResolveTypes()
	{
		if (typesResolved) return true;
		if (!ModContent.TryFind("CalamityMod/UnsafeSulphurousSandWall", out ModWall unsafeWall) ||
			!ModContent.TryFind("CalamityMod/SulphurousSand", out ModTile sandTile) ||
			!ModContent.TryFind("CalamityMod/HardenedSulphurousSandstone", out ModTile hardTile) ||
			!ModContent.TryFind("CalamityMod/HardenedSulphurousSandstoneWall", out ModWall hardWall))
			return false;
		unsafeSandWall = unsafeWall.Type; sand = sandTile.Type; hardSandstone = hardTile.Type; hardSandstoneWall = hardWall.Type;
		return typesResolved = true;
	}

	private static bool RecordGen(Func<Point, GenShape, GenAction, bool> orig, Point origin, GenShape shape, GenAction action)
	{
		recorded?.Add(origin);
		return orig(origin, shape, action);
	}

	// Mirror of the per-cell carve test (IL 0026–0105): carve when |Lerp(n, sign(n), w)| < threshold.
	private static bool SpaghettiCarves(int i, int y, int seed, int width, int depth, int top, float threshold)
	{
		float n = fbm(i * 0.00193f, y * 0.00193f, seed, 5, 0.5f, 2f);
		float length = new Vector2(i / (float)width, (y - top) / (float)depth).Length();
		float w = Utils.GetLerpValue(0.82f, 0.96f, length * 0.8f, true);
		w += Utils.GetLerpValue(top + 12f, top, y, true) * 0.2f;
		w += Utils.GetLerpValue(width - 19f, width - 4f, i, true) * 0.6f;
		return Math.Abs(MathHelper.Lerp(n, Math.Sign(n), w)) < threshold;
	}

	// Mirror of IL 0022–0081: the hard sandstone line under column i; cells with y >= YStart + line qualify.
	private static bool BelowHardLine(int i, int y, int seed, int width, int depth, int top)
	{
		int line = (int)(fbm(i * 0.004f, y * 0.004f, seed, 7, 0.5f, 2f) * 30f) + (int)(depth * 0.42f);
		line -= (int)(Math.Pow(Utils.GetLerpValue(width * 0.1f, width * 0.8f, i, true), 1.7200000286102295) * 67.0);
		return y >= top + line;
	}

	private static bool[] Mask(int firstColumn, int width, int depth, Func<int, int, bool> test)
	{
		var mask = new bool[width * depth];
		System.Threading.Tasks.Parallel.For(firstColumn, width, i =>
		{
			for (int dy = 0; dy < depth; dy++) mask[i * depth + dy] = test(i, dy);
		});
		return mask;
	}

	private static void Spaghetti(Original orig)
	{
		if (mode == "off" || !WorldGen.gen || !ResolveTypes()) { orig(); return; }
		var clock = Stopwatch.StartNew();
		int width = biomeWidth(), depth = (int)(blockDepth() * 0.96f), top = yStart();
		UnifiedRandom random = mode == "fast" ? WorldGen.genRand : Clone(WorldGen.genRand);
		var plan = new List<Point>();
		for (int c = 0; c < spaghettiThresholds.Length; c++)
		{
			int seed = random.Next();
			float threshold = spaghettiThresholds[c];
			bool[] mask = Mask(2, width, depth, (i, dy) => SpaghettiCarves(i, top + dy, seed, width, depth, top, threshold));
			for (int i = 2; i < width; i++)
			{
				int x = actualX(i);
				for (int dy = 0; dy < depth; dy++)
				{
					if (!mask[i * depth + dy]) continue;
					var point = new Point(x, top + dy);
					byte liquid = (byte)(random.NextFloat() > 0.91f ? 0 : 255);
					plan.Add(point);
					if (mode == "fast")
						WorldUtils.Gen(point, new Shapes.Rectangle(1, 1), Actions.Chain(new Actions.ClearTile(true), new Actions.PlaceWall(unsafeSandWall, true), new Actions.SetLiquid(0, liquid), new Actions.Smooth(true)));
				}
			}
		}
		Report("spaghetti", plan, random, clock, orig);
	}

	private static void HardSandstone(Original orig)
	{
		if (mode == "off" || !WorldGen.gen || !ResolveTypes()) { orig(); return; }
		var clock = Stopwatch.StartNew();
		int width = biomeWidth(), depth = blockDepth(), top = yStart();
		UnifiedRandom random = mode == "fast" ? WorldGen.genRand : Clone(WorldGen.genRand);
		int seed = random.Next();
		bool[] mask = Mask(0, width, depth, (i, dy) => BelowHardLine(i, top + dy, seed, width, depth, top));
		var plan = new List<Point>();
		for (int i = 0; i < width; i++)
		{
			int x = actualX(i);
			for (int dy = 0; dy < depth; dy++)
			{
				if (!mask[i * depth + dy]) continue;
				var point = new Point(x, top + dy);
				Tile tile = Main.tile[point.X, point.Y];
				if (!tile.HasTile || tile.TileType != sand) continue;
				plan.Add(point);
				if (mode == "fast")
					WorldUtils.Gen(point, new Shapes.Rectangle(1, 1), Actions.Chain(new Actions.SetTile(hardSandstone, true, true), new Actions.PlaceWall(hardSandstoneWall, true), new Actions.SetLiquid(0, 255)));
			}
		}
		Report("hardsandstone", plan, random, clock, orig);
	}

	// Fast mode logs the edit count; shadow mode runs Calamity, records its edit points through
	// WorldUtils.Gen and compares them and the genRand state with the plan computed on a clone.
	private static void Report(string step, List<Point> plan, UnifiedRandom random, Stopwatch clock, Original orig)
	{
		Mod mod = ModContent.GetInstance<SulphurSeaNoiseAccelerator>().Mod;
		long planMs = clock.ElapsedMilliseconds;
		if (mode == "fast")
		{
			mod.Logger.Info($"DINGDONG_SS1_NOISE|mode=fast|step={step}|edits={plan.Count}|ms={planMs}");
			return;
		}
		clock.Restart();
		recorded = new List<Point>();
		try { orig(); }
		finally
		{
			List<Point> actual = recorded; recorded = null;
			long origMs = clock.ElapsedMilliseconds;
			int mismatches = Math.Abs(actual.Count - plan.Count), first = -1;
			for (int k = 0; k < Math.Min(actual.Count, plan.Count); k++)
				if (actual[k] != plan[k]) { mismatches++; if (first < 0) first = k; }
			bool rngSame = SameState(random, WorldGen.genRand);
			mod.Logger.Info($"DINGDONG_SS1_NOISE|mode=shadow|step={step}|planned={plan.Count}|actual={actual.Count}|mismatches={mismatches}|first={first}" +
				$"|rngSame={rngSame}|planMs={planMs}|originalMs={origMs}|verdict={(mismatches == 0 && rngSame ? "IDENTICAL" : "DIFFERENT")}");
		}
	}

	private static UnifiedRandom Clone(UnifiedRandom source)
	{
		var copy = new UnifiedRandom(0);
		seedArray.SetValue(copy, ((int[])seedArray.GetValue(source)).Clone());
		inext.SetValue(copy, inext.GetValue(source));
		inextp.SetValue(copy, inextp.GetValue(source));
		return copy;
	}

	private static bool SameState(UnifiedRandom a, UnifiedRandom b)
	{
		if (!Equals(inext.GetValue(a), inext.GetValue(b)) || !Equals(inextp.GetValue(a), inextp.GetValue(b))) return false;
		int[] x = (int[])seedArray.GetValue(a), y = (int[])seedArray.GetValue(b);
		for (int k = 0; k < x.Length; k++) if (x[k] != y[k]) return false;
		return true;
	}
}
