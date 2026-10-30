using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// Worldgen order search, 30.10: Calamity's "Sulphur Sea 2" pass takes 180..430 s on x10 and its
// cost follows our ocean geometry. With DINGDONG_FOREIGN_PROFILE=<type name parts> set, every
// method of the matching Calamity types gets an enter/exit timer (outermost call only, behaviour
// unchanged) and the totals are logged after worldgen. Without the variable nothing is hooked.
internal sealed class ForeignPassProfiler : ModSystem
{
	private static readonly List<ILHook> hooks = new();
	private static string[] names = Array.Empty<string>();
	private static long[] ticks = Array.Empty<long>(), calls = Array.Empty<long>(), started = Array.Empty<long>();
	private static int[] depth = Array.Empty<int>();

	public override void Load()
	{
		string filter = Environment.GetEnvironmentVariable("DINGDONG_FOREIGN_PROFILE");
		if (string.IsNullOrWhiteSpace(filter) || !ModLoader.TryGetMod("CalamityMod", out Mod calamity)) return;
		string[] parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var methods = new List<MethodInfo>();
		foreach (Type type in LoadableTypes(calamity.Code))
		{
			if (type.FullName == null || !parts.Any(p => type.FullName.Contains(p, StringComparison.Ordinal))) continue;
			foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if (!method.IsAbstract && !method.ContainsGenericParameters && method.GetMethodBody() != null) methods.Add(method);
		}
		names = new string[methods.Count];
		ticks = new long[methods.Count]; calls = new long[methods.Count]; started = new long[methods.Count]; depth = new int[methods.Count];
		MethodInfo enter = typeof(ForeignPassProfiler).GetMethod(nameof(Enter), BindingFlags.NonPublic | BindingFlags.Static);
		MethodInfo exit = typeof(ForeignPassProfiler).GetMethod(nameof(Exit), BindingFlags.NonPublic | BindingFlags.Static);
		int failed = 0;
		for (int i = 0; i < methods.Count; i++)
		{
			int id = i;
			names[i] = methods[i].DeclaringType?.Name + "." + methods[i].Name;
			try
			{
				hooks.Add(new ILHook(methods[i], il =>
				{
					var c = new ILCursor(il);
					c.EmitLdcI4(id); c.EmitCall(enter);
					while (c.TryGotoNext(MoveType.Before, x => x.MatchRet()))
					{
						c.MoveBeforeLabels();
						c.EmitLdcI4(id); c.EmitCall(exit);
						c.Index++;
					}
				}));
			}
			catch (Exception) { failed++; }
		}
		Mod.Logger.Info($"DINGDONG_FOREIGN_PROFILE|state=ARMED|filter={filter}|methods={hooks.Count}|failed={failed}");
	}

	private static IEnumerable<Type> LoadableTypes(Assembly assembly)
	{
		try { return assembly.GetTypes(); }
		catch (ReflectionTypeLoadException partial) { return partial.Types.Where(t => t != null); }
	}

	private static void Enter(int id)
	{
		calls[id]++;
		if (depth[id]++ == 0) started[id] = Stopwatch.GetTimestamp();
	}

	private static void Exit(int id)
	{
		if (depth[id] > 0 && --depth[id] == 0) ticks[id] += Stopwatch.GetTimestamp() - started[id];
	}

	/// <summary>Logs and resets the timers after each foreign pass that ran profiled code.</summary>
	internal static void AfterPass(string pass)
	{
		if (hooks.Count == 0 || !calls.Any(c => c > 0)) return;
		Report("pass=" + pass);
		Array.Clear(ticks); Array.Clear(calls);
	}

	public override void PostWorldGen() => Report("pass=after-worldgen");

	private static void Report(string scope)
	{
		if (hooks.Count == 0) return;
		foreach (int i in Enumerable.Range(0, names.Length).Where(i => calls[i] > 0).OrderByDescending(i => ticks[i]).Take(15))
			ModContent.GetInstance<ForeignPassProfiler>().Mod.Logger.Info($"DINGDONG_FOREIGN_PROFILE|{scope}|method={names[i]}|ms={ticks[i] * 1000 / Stopwatch.Frequency}|calls={calls[i]}");
	}

	public override void Unload()
	{
		foreach (ILHook hook in hooks) hook.Dispose();
		hooks.Clear();
	}
}
