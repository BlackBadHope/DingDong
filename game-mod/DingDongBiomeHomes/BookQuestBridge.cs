using System;

namespace DingDongBiomeHomes;

/// <summary>
/// Seam between the book UI and quest-engine features that are developed in a parallel
/// track (saved DialogueChoice results, Abandon request, paused claims after a disconnect).
/// The UI only reads synchronized state through these hooks; when a hook is not wired the
/// book falls back cleanly (no abandon button, choice taken from the active stage only,
/// no "paused" status). Wiring is one assignment per hook in the engine's Load, e.g.
/// <c>BookQuestBridge.RequestAbandon = NpcQuestNetwork.RequestAbandon;</c>.
/// </summary>
internal static class BookQuestBridge
{
	/// <summary>(npcKey, 0-based stage) → saved choice id, or null when no choice is stored.</summary>
	internal static Func<string, int, string> ChoiceOf;

	/// <summary>Asks the server to drop the local player's active stage of this resident (keeps Completed).</summary>
	internal static Action<string> RequestAbandon;

	/// <summary>True when the active stage of this resident is paused (its performer left) and free to continue.</summary>
	internal static Func<string, bool> IsPaused;

	internal static bool CanAbandon => RequestAbandon != null;

	internal static void Reset()
	{
		ChoiceOf = null;
		RequestAbandon = null;
		IsPaused = null;
	}

	/// <summary>Choice id for a stage from the engine's world ledger (picks are recorded when made).</summary>
	internal static string Choice(string npcKey, int stage, ResidentQuestProgress progress)
	{
		try
		{
			string saved = ChoiceOf?.Invoke(npcKey, stage);
			if (!string.IsNullOrEmpty(saved))
				return saved;
		}
		catch (Exception)
		{
			// A bridge failure must never break the book; treat it as "no saved choice".
		}
		// The pick lives only in the world ledger; Progress just marks the stage as answered.
		return null;
	}

	internal static bool Paused(string npcKey)
	{
		try
		{
			return IsPaused != null && IsPaused(npcKey);
		}
		catch (Exception)
		{
			return false;
		}
	}
}
