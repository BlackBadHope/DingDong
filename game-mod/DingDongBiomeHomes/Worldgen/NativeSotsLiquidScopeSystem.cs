using System;
using System.Linq.Expressions;
using System.Reflection;
using DingDongBiomeHomes.Worldgen.Exact66Adapters;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class NativeSotsLiquidScopeSystem : ModSystem
{
	private delegate void OriginalSots(On_Liquid.orig_Update orig, Liquid liquid);
	private delegate void SotsHook(OriginalSots next, On_Liquid.orig_Update orig, Liquid liquid);
	private static Func<Rectangle> sanctuary;
	[ThreadStatic] private static bool sanctuaryWater;
	private Hook hook;
	internal static int Layer => sanctuaryWater ? Main.maxTilesY : NativeUnderworldSystem.Layer;
	public override void PostSetupContent()
	{
		if (!ModLoader.TryGetMod("SOTS", out _)) return;
		Mod provider = Exact66ProviderGate.Require("SOTS", new Version(0, 25, 1, 9), "14c19a4a-afb8-49fb-ab76-cd75dff954ac");
		const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		FieldInfo field = provider.Code.GetType("SOTS.WorldgenHelpers.SanctuaryWorldgenHelper", true).GetField("Rectangle", flags)
			?? throw new MissingFieldException("SOTS Sanctuary rectangle");
		if (field.FieldType != typeof(Rectangle)) throw new InvalidOperationException("SOTS Sanctuary bounds ABI changed.");
		sanctuary = Expression.Lambda<Func<Rectangle>>(Expression.Field(null, field)).Compile();
		MethodInfo source = provider.Code.GetType("SOTS.SOTSDetours", true).GetMethod("On_Liquid_Update", flags,
			null, new[] { typeof(On_Liquid.orig_Update), typeof(Liquid) }, null)
			?? throw new MissingMethodException("SOTS liquid hook");
		hook = new Hook(source, (SotsHook)Update);
		Mod.Logger.Info("DINGDONG_SOTS_LIQUID_HEIGHT|status=ready|x10GlobalHeightMutation=False|standardWorldsUnchanged=True");
		Probe(source, field);
	}
	private static void Update(OriginalSots next, On_Liquid.orig_Update orig, Liquid liquid)
	{
		if (!ContinentalWorldDimensions.IsContinental) { next(orig, liquid); return; }
		bool previous = sanctuaryWater;
		sanctuaryWater = sanctuary().Contains(liquid.x, liquid.y);
		try { orig(liquid); }
		finally { sanctuaryWater = previous; }
	}
	private void Probe(MethodInfo source, FieldInfo field)
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HEIGHT_COMPAT_PROBE") != "1") return;
		int width = Main.maxTilesX, height = Main.maxTilesY, checks = 0;
		object saved = field.GetValue(null);
		void Require(bool pass, string name) { if (!pass) throw new InvalidOperationException("SOTS liquid scope probe: " + name); checks++; }
		try
		{
			field.SetValue(null, new Rectangle(0, 0, 16, 16)); Main.maxTilesX = 28000; Main.maxTilesY = 7200;
			On_Liquid.orig_Update inside = _ => { Require(Main.maxTilesY == 7200, "global height changed"); Require(Layer == 7200, "Sanctuary evaporation exemption missing"); };
			source.Invoke(null, new object[] { inside, new Liquid { x = 1, y = 1 } });
			Require(!sanctuaryWater && Main.maxTilesY == 7200, "normal return leaked scope");
			On_Liquid.orig_Update fault = _ => throw new ApplicationException("isolated-liquid-probe");
			try { source.Invoke(null, new object[] { fault, new Liquid { x = 1, y = 1 } }); }
			catch (TargetInvocationException e) when (e.InnerException is ApplicationException { Message: "isolated-liquid-probe" }) { checks++; }
			Require(!sanctuaryWater && Main.maxTilesY == 7200, "exception leaked height or scope");
			Main.maxTilesX = 8400; Main.maxTilesY = 2400;
			On_Liquid.orig_Update standard = _ => Require(Main.maxTilesY == 2600, "standard-world provider behavior changed");
			source.Invoke(null, new object[] { standard, new Liquid { x = 1, y = 1 } });
			Require(Main.maxTilesY == 2400, "standard height not restored");
		}
		finally { Main.maxTilesX = width; Main.maxTilesY = height; field.SetValue(null, saved); sanctuaryWater = false; }
		Mod.Logger.Info($"DINGDONG_SOTS_LIQUID_PROBE|verdict=PASS|checks={checks}|actualForeignHookExecuted=True|exceptionPath=True|stateRestored=True|tileEdits=False");
	}
	public override void Unload() { hook?.Dispose(); hook = null; sanctuary = null; sanctuaryWater = false; }
}
