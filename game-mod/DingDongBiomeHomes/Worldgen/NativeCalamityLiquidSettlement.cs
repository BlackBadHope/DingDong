using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DingDongBiomeHomes.Worldgen.Exact66Adapters;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// Calamity's native budget is five times the queued cell count, in full
// UpdateLiquid calls, repeated ten times. On a continental sea this can spend
// billions of cell updates on the same non-converging frontier. Retain native
// QuickWater, WaterCheck, physics and queues; bound only the worldgen wait.
// Owner GO 30.10 tried bounding the vanilla "Settle Liquids" / "Settle Liquids Again" loops
// too (10 s): −52 s on x10, but on the owner seed the surface residences lost their aesthetic
// layout in 2 of 2 runs (0 of 5 without it). Kept as an opt-in (DINGDONG_LIQUID_BOUND=fast) only.
internal sealed class NativeCalamityLiquidSettlement : ModSystem
{
	// Default is the S27 bound (30 s, 512 checks, Calamity only); DINGDONG_LIQUID_BOUND=fast is the experiment.
	private static readonly bool Legacy = Environment.GetEnvironmentVariable("DINGDONG_LIQUID_BOUND") != "fast";
	internal static readonly double WaitSeconds = Legacy ? 30 : 10;
	internal static readonly int ChecksWithoutProgress = Legacy ? 512 : 256;
	private static readonly string[] VanillaPasses = { "Settle Liquids", "Settle Liquids Again" };
	private readonly System.Collections.Generic.List<ILHook> vanillaHooks = new();
	private delegate void Original(bool quick);
	private delegate void SettlementHook(Original next, bool quick);
	private ILHook ilHook;
	private Hook hook;
	[ThreadStatic] private static Budget current;
	private sealed class Budget
	{
		internal readonly long Started = Stopwatch.GetTimestamp();
		internal long LoopStarted;
		internal int BestDebt = int.MaxValue, WithoutProgress;
		internal int Checks;
		internal bool Deferred;
		internal int DeferredDebt = -1;
	}
	public override void PostSetupContent()
	{
		if (!ModLoader.TryGetMod("CalamityMod", out _)) return;
		Mod provider = Exact66ProviderGate.Require("CalamityMod", new Version(2, 2, 2), "0e63c698-ec72-446e-ac1f-8d01f0ee7ac7");
		MethodInfo source = provider.Code.GetType("CalamityMod.CalamityUtils", true)
			.GetMethod("SettleWater", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(bool) }, null)
			?? throw new MissingMethodException("Calamity SettleWater");
		ilHook = new ILHook(source, Patch);
		hook = new Hook(source, (SettlementHook)Settle);
		Mod.Logger.Info("DINGDONG_CALAMITY_LIQUID_SETTLEMENT|status=ready|scope=native-x10-worldgen|pendingWaterDiscarded=False");
	}
	private static void Patch(ILContext il)
	{
		var sites = il.Body.Instructions.Where(i => i.MatchLdsfld("Terraria.Liquid", "numLiquid") &&
			i.Next?.MatchLdcI4(0) == true && i.Next.Next.OpCode.FlowControl == FlowControl.Cond_Branch).ToArray();
		if (sites.Length != 1) throw new InvalidOperationException("Calamity liquid loop topology changed.");
		var c = new ILCursor(il); c.Goto(sites[0], MoveType.After);
		c.EmitDelegate<Func<int, int>>(Continue);
	}
	private static int Continue(int active)
	{
		Budget budget = current;
		if (budget == null || active <= 0) return active;
		budget.Checks++;
		// QuickWater may itself take longer than the wait budget on x10. Start
		// measuring at this loop, and measure this call's progress rather than
		// Liquid.stuckCount inherited from a previous terrain generation pass.
		if (budget.LoopStarted == 0) budget.LoopStarted = Stopwatch.GetTimestamp();
		int debt = active + LiquidBuffer.numLiquidBuffer;
		if (debt < budget.BestDebt - 16) { budget.BestDebt = debt; budget.WithoutProgress = 0; }
		else budget.WithoutProgress++;
		if (budget.WithoutProgress < ChecksWithoutProgress && Stopwatch.GetElapsedTime(budget.LoopStarted).TotalSeconds < WaitSeconds) return active;
		if (!budget.Deferred) budget.DeferredDebt = debt;
		budget.Deferred = true;
		return 0; // A loop condition only. Never overwrite numLiquid or its buffer.
	}
	public override void ModifyWorldGenTasks(System.Collections.Generic.List<Terraria.WorldBuilding.GenPass> tasks, ref double totalWeight)
	{
		if (Legacy || vanillaHooks.Count > 0) return; // BeginVanilla gates on x10 at run time.
		foreach (string name in VanillaPasses)
		{
			Terraria.WorldBuilding.GenPass pass = tasks.Find(t => t.Name == name);
			MethodInfo method = pass == null ? null : LegacyMethod(pass);
			if (method == null) { Mod.Logger.Warn($"DINGDONG_VANILLA_LIQUID_SETTLEMENT|pass={name}|status=not-found"); continue; }
			try
			{
				vanillaHooks.Add(new ILHook(method, il => PatchVanilla(il, name)));
				Mod.Logger.Info($"DINGDONG_VANILLA_LIQUID_SETTLEMENT|pass={name}|status=ready|waitSeconds={WaitSeconds}|checksWithoutProgress={ChecksWithoutProgress}");
			}
			catch (Exception error) { Mod.Logger.Warn($"DINGDONG_VANILLA_LIQUID_SETTLEMENT|pass={name}|status=disabled|reason={error.Message}"); }
		}
	}

	private static MethodInfo LegacyMethod(Terraria.WorldBuilding.GenPass pass)
	{
		for (Type t = pass.GetType(); t != null; t = t.BaseType)
			foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if (typeof(Delegate).IsAssignableFrom(field.FieldType) && field.GetValue(pass) is Delegate d) return d.Method;
		return null;
	}

	// The vanilla pass body: QuickWater, WaterCheck, then ten rounds of "UpdateLiquid while
	// numLiquid > 0 (at most 5x the queue)" each followed by WaterCheck. Only that loop
	// condition is bounded; every WaterCheck and the round structure stay native.
	private static void PatchVanilla(ILContext il, string name)
	{
		Patch(il);
		var c = new ILCursor(il);
		c.EmitLdstr(name);
		c.EmitDelegate<Action<string>>(BeginVanilla);
		while (c.TryGotoNext(MoveType.Before, x => x.MatchRet()))
		{
			c.MoveBeforeLabels();
			c.EmitDelegate<Action>(EndVanilla);
			c.Index++;
		}
	}

	[ThreadStatic] private static string vanillaPass;
	private static void BeginVanilla(string name)
	{
		if (!WorldGen.gen || !ContinentalWorldDimensions.IsContinental) return;
		vanillaPass = name; current = new Budget();
	}
	private static void EndVanilla()
	{
		Budget budget = current;
		if (vanillaPass == null || budget == null) return;
		global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
			$"DINGDONG_VANILLA_LIQUID_SETTLEMENT|pass={vanillaPass}|deferred={budget.Deferred}|deferredDebt={budget.DeferredDebt}|checks={budget.Checks}|seconds={Stopwatch.GetElapsedTime(budget.Started).TotalSeconds:F1}|active={Liquid.numLiquid}|buffer={LiquidBuffer.numLiquidBuffer}|pendingWaterDiscarded=False");
		vanillaPass = null; current = null;
	}

	private static void Settle(Original next, bool quick)
	{
		if (!WorldGen.gen || !ContinentalWorldDimensions.IsContinental) { next(quick); return; }
		Budget previous = current; var budget = new Budget(); bool previousQuick = Liquid.quickSettle;
		current = budget;
		try { next(quick); }
		finally
		{
			current = previous; Liquid.quickSettle = previousQuick;
			Liquid.worldGenTilesIgnoreWater(false);
			global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
				$"DINGDONG_CALAMITY_LIQUID_SETTLEMENT|deferred={budget.Deferred}|deferredDebt={budget.DeferredDebt}|checks={budget.Checks}|seconds={Stopwatch.GetElapsedTime(budget.Started).TotalSeconds:F1}|active={Liquid.numLiquid}|buffer={LiquidBuffer.numLiquidBuffer}|pendingWaterDiscarded=False");
		}
	}
	// DINGDONG_LIQUID_AUDIT=1: liquid census of the finished world, for comparing settle bounds.
	// "falling" = liquid with a non-solid, not-full cell below it, i.e. water still to move in play.
	public override void PostWorldGen()
	{
		if (Environment.GetEnvironmentVariable("DINGDONG_LIQUID_AUDIT") != "1") return;
		long water = 0, lava = 0, honey = 0, shimmer = 0, falling = 0, amount = 0;
		for (int x = 1; x < Main.maxTilesX - 1; x++)
			for (int y = 1; y < Main.maxTilesY - 1; y++)
			{
				Tile tile = Main.tile[x, y];
				if (tile.LiquidAmount == 0) continue;
				amount += tile.LiquidAmount;
				switch (tile.LiquidType) { case 0: water++; break; case 1: lava++; break; case 2: honey++; break; default: shimmer++; break; }
				Tile below = Main.tile[x, y + 1];
				if (!(below.HasTile && Main.tileSolid[below.TileType] && !Main.tileSolidTop[below.TileType]) && below.LiquidAmount < 255) falling++;
			}
		Mod.Logger.Info($"DINGDONG_LIQUID_AUDIT|bound={(Legacy ? "legacy" : "fast")}|water={water}|lava={lava}|honey={honey}|shimmer={shimmer}|amount={amount}|falling={falling}|active={Liquid.numLiquid}|buffer={LiquidBuffer.numLiquidBuffer}");
	}

	public override void Unload()
	{
		hook?.Dispose(); hook = null; ilHook?.Dispose(); ilHook = null; current = null;
		foreach (ILHook vanilla in vanillaHooks) vanilla.Dispose();
		vanillaHooks.Clear();
	}
}
