using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;

namespace DingDongBiomeHomes;

internal enum ResidentQuestStatus : byte
{
	Locked,          // the resident still has to be found or rescued (encounter pending)
	Available,       // a stage can be accepted
	InProgress,      // the viewer performs the active stage
	ReadyToTurnIn,   // the viewer can turn the stage in now
	TakenByOther,    // another player performs it
	Paused,          // its performer left; anyone may resume with the progress kept
	Complete         // all stages done
}

internal enum QuestListFilter : byte { All, Main, Side, Mine, Completed }

internal enum ResidentStageState : byte { Done, Current, Upcoming }

/// <summary>One stage of a resident's story as the book's "whole story" section shows it.</summary>
internal readonly record struct ResidentStageView(int Index, string Title, ResidentQuestKind Kind, ResidentStageState State, string Description, string ChoiceText, string ChoiceOutcome);

/// <summary>
/// Read-only quest facts for the UI track (book, HUD, markers). Everything is derived
/// from the viewer's synchronized snapshot and the world choice ledger; no strings are
/// built in Draw: call these from Update or when the page is rebuilt.
/// </summary>
internal static class QuestStatusApi
{
	internal static ResidentQuestProgress Progress(Player viewer, string npcKey) =>
		viewer.GetModPlayer<NpcQuestPlayer>().TryGetCurrent(npcKey, out var p) ? p : new ResidentQuestProgress();

	internal static ResidentQuestStatus Status(Player viewer, string npcKey) => Status(viewer, npcKey, Progress(viewer, npcKey));

	internal static ResidentQuestStatus Status(Player viewer, string npcKey, ResidentQuestProgress p)
	{
		if (ResidentEncounterSystem.IsPending(npcKey)) return ResidentQuestStatus.Locked;
		if (PersonalResidentQuestCatalog.Complete(npcKey, p.Completed) && !p.Active) return ResidentQuestStatus.Complete;
		if (!p.Active) return ResidentQuestStatus.Available;
		if (p.Paused) return ResidentQuestStatus.Paused;
		string me = viewer.GetModPlayer<NpcQuestPlayer>().PersistentId;
		if (!p.IsAssignedTo(me)) return ResidentQuestStatus.TakenByOther;
		if (!NpcTownManifest.TryGet(npcKey, out var identity)) return ResidentQuestStatus.InProgress;
		return NpcQuestSystem.IsReady(viewer, p.Definition(identity), p) ? ResidentQuestStatus.ReadyToTurnIn : ResidentQuestStatus.InProgress;
	}

	/// <summary>Localized status caption; TakenByOther and Paused name the performer.</summary>
	internal static string StatusLabel(ResidentQuestStatus status, ResidentQuestProgress p) =>
		Language.GetTextValue("Mods.DingDongBiomeHomes.QuestEngine.Status." + status, p?.AssignedName ?? "");

	internal static string FilterLabel(QuestListFilter filter) => Language.GetTextValue("Mods.DingDongBiomeHomes.QuestEngine.Filters." + filter);

	internal static bool Matches(QuestListFilter filter, Player viewer, string npcKey, ResidentQuestProgress p) => filter switch
	{
		QuestListFilter.Main => ResidentStoryRoles.IsMain(npcKey),
		QuestListFilter.Side => !ResidentStoryRoles.IsMain(npcKey),
		QuestListFilter.Mine => p.Active && (p.IsAssignedTo(viewer.GetModPlayer<NpcQuestPlayer>().PersistentId) || p.IsPausedFor(viewer.GetModPlayer<NpcQuestPlayer>().PersistentId)),
		QuestListFilter.Completed => PersonalResidentQuestCatalog.Complete(npcKey, p.Completed),
		_ => true
	};

	internal static bool CanAbandon(Player viewer, ResidentQuestProgress p)
	{
		string me = viewer.GetModPlayer<NpcQuestPlayer>().PersistentId;
		return p.Active && (p.IsAssignedTo(me) || p.IsPausedFor(me));
	}

	/// <summary>Sends the abandon request (the book shows its own confirmation first).</summary>
	internal static void Abandon(string npcKey) => NpcQuestNetwork.RequestQuestAbandon(npcKey);

	/// <summary>Objective progress in display units (seconds, days, items, kills, tiles).</summary>
	internal static (int Current, int Goal) ObjectiveCount(Player viewer, string npcKey, ResidentQuestProgress p)
	{
		if (!NpcTownManifest.TryGet(npcKey, out var identity)) return (0, 1);
		var d = p.Active ? p.Definition(identity) : PersonalResidentQuestCatalog.Resolve(identity, Math.Min(p.Completed, PersonalResidentQuestCatalog.Length(npcKey) - 1));
		if (!p.Active) return (0, NpcQuestSystem.DisplayProgress(viewer, d, new ResidentQuestProgress()).Goal);
		return NpcQuestSystem.DisplayProgress(viewer, d, p);
	}

	internal static string ChosenId(string npcKey, int stage) => NpcQuestWorldLedger.TryGetChoice(npcKey, stage, out string id) ? id : "";

	/// <summary>All stages of the story: titles, authored descriptions of done/current stages and recorded choice outcomes.</summary>
	internal static IReadOnlyList<ResidentStageView> Stages(string npcKey, ResidentQuestProgress p)
	{
		var story = PersonalResidentQuestCatalog.Story(npcKey);
		if (story == null) return Array.Empty<ResidentStageView>();
		var list = new List<ResidentStageView>(story.Steps.Length);
		for (int i = 0; i < story.Steps.Length; i++)
		{
			var step = story.Steps[i];
			var state = i < p.Completed ? ResidentStageState.Done : i == p.Completed ? ResidentStageState.Current : ResidentStageState.Upcoming;
			string choiceText = "", outcome = "";
			if (step.Kind == "DialogueChoice")
			{
				string id = ChosenId(npcKey, i);
				foreach (var c in step.Choices) if (c.Id == id) { choiceText = c.Text; outcome = LoreCatalog.Outcome(npcKey, i, id); }
			}
			ResidentQuestKind kind = NpcTownManifest.TryGet(npcKey, out var identity) ? PersonalResidentQuestCatalog.Resolve(identity, i).Kind : ResidentQuestKind.VisitBiome;
			list.Add(new ResidentStageView(i, step.Title, kind, state, state == ResidentStageState.Upcoming ? "" : LoreCatalog.Description(npcKey, i), choiceText, outcome));
		}
		return list;
	}
}
