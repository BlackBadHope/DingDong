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
		return true;
	}
}
