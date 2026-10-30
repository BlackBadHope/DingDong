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
		DumpRequestedIl();
		string filter = Environment.GetEnvironmentVariable("DINGDONG_FOREIGN_PROFILE");
		// DINGDONG_VANILLA_PROFILE=Terraria.Liquid.QuickWater;Terraria.WorldGen.WaterCheck times single Terraria methods the same way.
		string vanilla = Environment.GetEnvironmentVariable("DINGDONG_VANILLA_PROFILE");
		var methods = new List<MethodInfo>();
		if (!string.IsNullOrWhiteSpace(filter) && ModLoader.TryGetMod("CalamityMod", out Mod calamity))
		{
			string[] parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (Type type in LoadableTypes(calamity.Code))
			{
				if (type.FullName == null || !parts.Any(p => type.FullName.Contains(p, StringComparison.Ordinal))) continue;
				foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
					if (!method.IsAbstract && !method.ContainsGenericParameters && method.GetMethodBody() != null) methods.Add(method);
			}
		}
		if (!string.IsNullOrWhiteSpace(vanilla))
			foreach (string entry in vanilla.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				int dot = entry.LastIndexOf('.');
				Type type = dot > 0 ? typeof(Terraria.Main).Assembly.GetType(entry[..dot]) : null;
				if (type == null) continue;
				methods.AddRange(type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
					.Where(m => m.Name == entry[(dot + 1)..] && !m.ContainsGenericParameters && m.GetMethodBody() != null));
			}
		if (methods.Count == 0) return;
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
		Mod.Logger.Info($"DINGDONG_FOREIGN_PROFILE|state=ARMED|filter={filter}|vanilla={vanilla}|methods={hooks.Count}|failed={failed}");
	}

	// DINGDONG_FOREIGN_IL_DUMP=Type.Method;Type.Method logs the IL of those Calamity methods once
	// at load (read-only: the hook body changes nothing and is disposed right away).
	private void DumpRequestedIl()
	{
		string request = Environment.GetEnvironmentVariable("DINGDONG_FOREIGN_IL_DUMP");
		if (string.IsNullOrWhiteSpace(request) || !ModLoader.TryGetMod("CalamityMod", out Mod calamity)) return;
		Type[] types = LoadableTypes(calamity.Code).ToArray();
		foreach (string entry in request.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			int dot = entry.LastIndexOf('.');
			if (dot <= 0) continue;
			string typeName = entry[..dot], methodName = entry[(dot + 1)..];
			foreach (Type type in types.Where(t => t.Name == typeName))
			foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(m => m.Name == methodName && m.GetMethodBody() != null))
				DumpMethod(method);
		}
	}

	// DINGDONG_PASS_IL_DUMP=Wall Variety;Smooth World logs the IL behind those world generation passes
	// (the delegate a legacy pass wraps), read-only, so they can be compared with an accelerated copy.
	public override void ModifyWorldGenTasks(List<Terraria.WorldBuilding.GenPass> tasks, ref double totalWeight)
	{
		string request = Environment.GetEnvironmentVariable("DINGDONG_PASS_IL_DUMP");
		if (string.IsNullOrWhiteSpace(request)) return;
		foreach (string name in request.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			foreach (Terraria.WorldBuilding.GenPass pass in tasks.Where(t => t.Name == name))
			{
				MethodInfo method = null;
				for (Type t = pass.GetType(); t != null && method == null; t = t.BaseType)
					foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
						if (typeof(Delegate).IsAssignableFrom(field.FieldType) && field.GetValue(pass) is Delegate d) { method = d.Method; break; }
				Mod.Logger.Info($"DINGDONG_PASS_IL|pass={name}|type={pass.GetType().FullName}|method={method?.DeclaringType?.FullName}.{method?.Name}");
				if (method != null) DumpMethod(method);
			}
	}

	private void DumpMethod(MethodInfo method)
	{
		var lines = new List<string>();
		try
		{
			using (new ILHook(method, il =>
			{
				foreach (var local in il.Body.Variables) lines.Add($"local V_{local.Index}: {local.VariableType.FullName}");
				for (int i = 0; i < il.Instrs.Count; i++) lines.Add($"{i:D4}: {il.Instrs[i].OpCode.Name} {FormatOperand(il, il.Instrs[i].Operand)}");
			})) { }
		}
		catch (Exception error) { lines.Add("dump-failed: " + error.Message); }
		Mod.Logger.Info($"DINGDONG_FOREIGN_IL|method={method.DeclaringType?.FullName}.{method.Name}|count={lines.Count}");
		foreach (string line in lines) Mod.Logger.Info("DINGDONG_FOREIGN_IL|" + line);
	}

	private static string FormatOperand(ILContext il, object operand) => operand switch
	{
		null => string.Empty,
		ILLabel label => "-> " + il.IndexOf(label.Target).ToString("D4"),
		Mono.Cecil.Cil.Instruction target => "-> " + il.IndexOf(target).ToString("D4"),
		ILLabel[] labels => "switch " + string.Join(",", labels.Select(l => il.IndexOf(l.Target).ToString("D4"))),
		Mono.Cecil.Cil.Instruction[] targets => "switch " + string.Join(",", targets.Select(t => il.IndexOf(t).ToString("D4"))),
		string text => "\"" + text + "\"",
		_ => operand.ToString()
	};

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
