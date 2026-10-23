using System;
using DingDongBiomeHomes.Worldgen.Compiler.Api;

namespace DingDongBiomeHomes;

internal static class DingDongModCallRouter
{
	internal static object Route(object[] args)
	{
		if (TryQuest(args, out object result)) return result;
		if (WorldCompilerModApi.TryHandle(args, out result)) return result;
		throw new ArgumentException(
			"Unsupported DingDong call. Query Worldgen.GetCapabilities for the " +
			"world-compiler API or use RegisterQuestMilestone.", nameof(args));
	}

	private static bool TryQuest(object[] args, out object result)
	{
		result = null;
		if (args?.Length != 5 || args[0] is not string operation ||
			!string.Equals(operation, "RegisterQuestMilestone",
				StringComparison.Ordinal) || args[1] is not string providerMod ||
			args[2] is not string milestoneKey || args[3] is not string displayName ||
			args[4] is not Func<bool> completionProbe) return false;
		result = QuestContentRegistry.RegisterMilestone(providerMod, milestoneKey,
			displayName, completionProbe);
		return true;
	}
}
