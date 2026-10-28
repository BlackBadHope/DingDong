using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.IO;
using Terraria.Localization;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldHooks
{
	private static void GuardServerMemoryFailure(ILContext il)
	{
		// The native callback retries and can replace the primary world with its
		// backup after ANY failed load. Memory admission is not file corruption.
		var loads = il.Body.Instructions.Where(i => i.OpCode == OpCodes.Call &&
			i.Operand is MethodReference m && m.DeclaringType.FullName == typeof(WorldFile).FullName &&
			m.Name == nameof(WorldFile.LoadWorld)).ToArray();
		if (loads.Length != 4)
			throw new InvalidOperationException("Unsupported server world-load recovery layout.");
		foreach (var load in loads.Reverse())
		{
			var cursor = new ILCursor(il);
			cursor.Goto(load, MoveType.After);
			var resume = cursor.Next;
			cursor.EmitDelegate<Func<bool>>(StopServerMemoryRecovery);
			cursor.Emit(OpCodes.Brfalse, resume);
			cursor.Emit(OpCodes.Ret);
		}
	}

	private static bool StopServerMemoryRecovery()
	{
		if (!Main.dedServ || !WorldGen.loadFailed ||
			WorldFile.LastThrownLoadException is not ContinentalMemoryAdmissionException error)
			return false;
		string message = Language.ActiveCulture.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
			? "Недостаточно доступной памяти для запуска сервера большого мира. " +
			  "Загрузка остановлена до выделения памяти. Мир и резервная копия не заменялись. " +
			  "Закройте лишние приложения и повторите запуск. "
			: "Not enough available memory to start this continental-world server. " +
			  "Loading stopped before allocation. The world and its backup were not replaced. " +
			  "Close other applications and retry. ";
		Console.WriteLine(message + error.Message);
		DingDongBiomeHomes.Instance.Logger.Warn(
			"DINGDONG_SERVER_MEMORY|verdict=DENIED|backupRecovery=skipped|worldFileUnchanged=True|reason=" + error.Message);
		// RamOpt 28.10: fail fast instead of waiting on Console.ReadKey in
		// Main.DedServ_PostModLoad ("if (!autoShutdown) Console.ReadKey(); return;").
		// The world-load callback continues to its native failed-load branch, which now
		// returns at once; the process then ends with a non-zero exit code.
		Main.autoShutdown = true;
		Environment.ExitCode = ServerMemoryDeniedExitCode;
		return true;
	}

	// RamOpt 28.10: distinct exit code for scripts and hosting panels.
	internal const int ServerMemoryDeniedExitCode = 3;

	private static void ExitServerAfterMemoryDenial(string scope, ContinentalMemoryAdmissionException error,
		ContinentalWorldPreset preset = null)
	{
		bool ru = Language.ActiveCulture.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
		// Presets 28.10: name the preset; ram32 keeps the previous x10 text.
		string label = ContinentalWorldCapacity.PresetLabel(preset ?? ContinentalWorldDimensions.Current);
		string message = ru
			? $"Недостаточно памяти для создания мира {(label == "x10" ? "×10" : label)} на этом сервере. Создание остановлено до выделения памяти, файл мира не записывался. "
			: $"Not enough memory to create the {label} world on this server. Creation stopped before allocation; no world file was written. ";
		Console.WriteLine(message + error.Message + VanillaServerHint(ru, preset ?? ContinentalWorldDimensions.Current));
		DingDongBiomeHomes.Instance?.Logger.Warn(
			$"DINGDONG_SERVER_MEMORY|verdict=DENIED|scope={scope}|worldFileWritten=False|exitCode={ServerMemoryDeniedExitCode}" +
			"|consoleWait=skipped|reason=" + error.Message);
		Console.Out.Flush();
		Environment.Exit(ServerMemoryDeniedExitCode);
	}

	// RamOpt review fix 28.10: out of memory during x10 generation on a dedicated server.
	// Unlike the admission refusal this happens after allocation. The final save of the new
	// world runs only after GenerateWorld returns, so the world is never completed.
	private static void ExitServerAfterGenerationOutOfMemory(OutOfMemoryException error)
	{
		bool ru = Language.ActiveCulture.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
		// Presets 28.10: vanilla Large is no longer a server choice; suggest the next smaller preset.
		ContinentalWorldPreset current = ContinentalWorldDimensions.Current;
		string label = ContinentalWorldCapacity.PresetLabel(current);
		string smaller = ContinentalWorldDimensions.NextSmaller(current)?.Id;
		string message = ru
			? $"Память закончилась во время генерации мира {(label == "x10" ? "×10" : label)}. Генерация остановлена, мир не создан. " +
			  "Закройте другие приложения, увеличьте файл подкачки" +
			  (smaller != null ? $" или выберите меньший пресет (DINGDONG_SERVER_WORLD_SIZE={smaller})" : "") + " и повторите. "
			: $"Memory ran out during {label} world generation. Generation stopped; the world was not created. " +
			  "Close other applications, enlarge the page file" +
			  (smaller != null ? $" or choose the smaller preset (DINGDONG_SERVER_WORLD_SIZE={smaller})" : "") + " and retry. ";
		string memory;
		try
		{
			ContinentalMemorySnapshot snapshot = ContinentalMemoryStatus.Read();
			memory = $"|physical={snapshot.AvailablePhysicalBytes}|commit={snapshot.AvailableCommitBytes}|commitLimit={snapshot.CommitLimitBytes}";
		}
		catch (Exception)
		{
			memory = "|memory=unavailable";
		}
		Console.WriteLine(message + error.Message + VanillaServerHint(ru, current));
		DingDongBiomeHomes.Instance?.Logger.Warn(
			$"DINGDONG_SERVER_MEMORY|verdict=OUT_OF_MEMORY|scope=generation|worldCompleted=False|exitCode={ServerMemoryDeniedExitCode}" +
			memory + "|consoleWait=skipped|reason=" + error.Message);
		Console.Out.Flush();
		Environment.Exit(ServerMemoryDeniedExitCode);
	}

	// Presets 28.10 review fix: with no smaller released preset, tell the operator how to get a
	// vanilla world again ("large" = no promotion, accepted by old and new builds).
	private static string VanillaServerHint(bool ru, ContinentalWorldPreset preset) =>
		ContinentalWorldDimensions.NextSmaller(preset) != null ? "" : ru
			? $" Р’Р°РЅРёР»СЊРЅС‹Р№ РјРёСЂ Р±РµР· РєРѕРЅС‚РёРЅРµРЅС‚Р°Р»СЊРЅС‹С… С„СѓРЅРєС†РёР№ Ding Dong: {ContinentalCreationChoice.ServerSizeVariable}=large."
			: $" For a vanilla world without Ding Dong continental features set {ContinentalCreationChoice.ServerSizeVariable}=large.";
}
