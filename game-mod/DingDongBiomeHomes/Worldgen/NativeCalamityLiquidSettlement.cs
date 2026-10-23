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
internal sealed class NativeCalamityLiquidSettlement : ModSystem
{
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
		if (budget.WithoutProgress < 512 && Stopwatch.GetElapsedTime(budget.LoopStarted).TotalSeconds < 30) return active;
		budget.Deferred = true;
		return 0; // A loop condition only. Never overwrite numLiquid or its buffer.
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
				$"DINGDONG_CALAMITY_LIQUID_SETTLEMENT|deferred={budget.Deferred}|checks={budget.Checks}|seconds={Stopwatch.GetElapsedTime(budget.Started).TotalSeconds:F1}|active={Liquid.numLiquid}|buffer={LiquidBuffer.numLiquidBuffer}|pendingWaterDiscarded=False");
		}
	}
	public override void Unload() { hook?.Dispose(); hook = null; ilHook?.Dispose(); ilHook = null; current = null; }
}
