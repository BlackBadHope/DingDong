using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler.Api;

internal static class WorldCompilerExtensionRegistry
{
	private sealed record Registration(string Provider, string Key,
		WorldCompilerExtensionPhase Phase, Action Callback);

	private static readonly object Sync = new();
	private static readonly Dictionary<string, Registration> Entries =
		new(StringComparer.Ordinal);
	private static readonly HashSet<WorldCompilerExtensionPhase> Executed = [];
	private static bool worldActive;

	internal static bool Register(string provider, string key, string phaseName,
		Action callback)
	{
		if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(key) ||
			callback == null || !Enum.TryParse(phaseName, true,
				out WorldCompilerExtensionPhase phase) || !Enum.IsDefined(phase))
			return false;
		string identity = provider.Trim() + "\0" + key.Trim();
		lock (Sync)
		{
			if (worldActive || Entries.ContainsKey(identity)) return false;
			Entries.Add(identity, new Registration(provider.Trim(), key.Trim(), phase,
				callback));
			return true;
		}
	}

	internal static void BeginWorld()
	{
		lock (Sync)
		{
			Executed.Clear();
			worldActive = true;
		}
	}

	internal static void EndWorld()
	{
		lock (Sync)
		{
			Executed.Clear();
			worldActive = false;
		}
	}

	internal static bool HasActions(WorldCompilerExtensionPhase phase)
	{
		lock (Sync)
			foreach (Registration entry in Entries.Values)
				if (entry.Phase == phase) return true;
		return false;
	}

	internal static int Execute(WorldCompilerExtensionPhase phase)
	{
		Registration[] actions;
		lock (Sync)
		{
			if (!worldActive)
				throw new InvalidOperationException("Worldgen extension registry is closed.");
			if (!Executed.Add(phase))
				throw new InvalidOperationException($"Worldgen phase repeated: {phase}.");
			var selected = new List<Registration>();
			foreach (Registration entry in Entries.Values)
				if (entry.Phase == phase) selected.Add(entry);
			selected.Sort(static (left, right) =>
			{
				int provider = string.CompareOrdinal(left.Provider, right.Provider);
				return provider != 0 ? provider : string.CompareOrdinal(left.Key, right.Key);
			});
			actions = selected.ToArray();
		}
		foreach (Registration action in actions)
			try
			{
				action.Callback();
			}
			catch (Exception exception)
			{
				throw new InvalidOperationException(
					$"Worldgen extension failed: {action.Provider}/{action.Key} " +
					$"at {phase}.", exception);
			}
		return actions.Length;
	}

	internal static string[] PhaseNames() =>
		Enum.GetNames<WorldCompilerExtensionPhase>();

	internal static void ResetAll()
	{
		lock (Sync)
		{
			Entries.Clear();
			Executed.Clear();
			worldActive = false;
		}
	}
}
