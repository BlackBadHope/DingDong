using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace DingDongBiomeHomes;

// "Летопись" (owner item 11): chapters on the left (Мир, Силы, Главная линия, Финал, Узлы),
// the chosen page on the right. Texts come from the lore contract (Quests/Lore/codex.json and
// the node files); unlocks are evaluated on the client from synchronized quest state, saved
// choices and vanilla boss flags, so every player of a world sees the same open entries.
// "Read" marks are per character (BookTrackerPlayer).
internal sealed partial class DingDongHubUiState
{
	private const string MainLineId = "mainline";
	private const string NodePrefix = "places.";

	private static int ChronicleSignature()
	{
		BookLoreWorld world = BookLore.World(Main.LocalPlayer);
		int unlocked = 0;
		foreach (BookLoreEntry entry in BookLore.Entries)
			if (BookLore.Unlocked(entry.Unlock, world))
				unlocked++;
		return HashCode.Combine(unlocked, world.AllStages, world.MainStages, world.Choices.Count, world.Leaning);
	}

	private void BuildChronicle(BookSpread book)
	{
		BookLoreWorld world = BookLore.World(Main.LocalPlayer);
		BookTrackerPlayer reader = Main.LocalPlayer.GetModPlayer<BookTrackerPlayer>();
		IReadOnlyList<BookLoreEntry> entries = BookLore.Entries;
		IReadOnlyList<BookLoreNode> nodes = BookLore.Nodes;
		lastChronicleSignature = ChronicleSignature();
		selectedChronicle ??= MainLineId;
		BookLoreEntry selectedEntry = null;
		BookLoreNode selectedNode = null;
		foreach (BookLoreEntry entry in entries)
			if (entry.Id == selectedChronicle)
				selectedEntry = entry;
		foreach (BookLoreNode node in nodes)
			if (NodePrefix + node.Id == selectedChronicle)
				selectedNode = node;
		if (selectedEntry == null && selectedNode == null)
			selectedChronicle = MainLineId;
		// Opening a page marks it read before the index is drawn, so its "new" badge goes away at once.
		if (selectedEntry != null && BookLore.Unlocked(selectedEntry.Unlock, world))
			reader.MarkRead(selectedEntry.Id);
		if (selectedNode != null && BookLore.NodeUnlocked(selectedNode, world))
			reader.MarkRead(NodePrefix + selectedNode.Id);

		int open = 0, total = entries.Count + nodes.Count;
		foreach (BookLoreEntry entry in entries)
			if (BookLore.Unlocked(entry.Unlock, world))
				open++;
		foreach (BookLoreNode node in nodes)
			if (BookLore.NodeUnlocked(node, world))
				open++;
		book.AddIndexCaption(BookUiText.Format("Chronicle.Unlocked", open, total));

		foreach (string chapter in BookLore.ChapterOrder)
		{
			bool any = chapter == "arcs";
			foreach (BookLoreEntry entry in entries)
				any |= entry.Chapter == chapter;
			if (chapter == "places")
				any |= nodes.Count > 0;
			if (!any)
				continue;
			book.AddIndexCaption(BookLore.ChapterTitle(chapter));
			if (chapter == "arcs")
				AddChronicleRow(book, MainLineId, BookUiText.Get("Chronicle.MainLine.Title"), ItemID.FallenStar, true, false);
			foreach (BookLoreEntry entry in entries)
			{
				if (entry.Chapter != chapter)
					continue;
				bool unlocked = BookLore.Unlocked(entry.Unlock, world);
				AddChronicleRow(book, entry.Id, unlocked ? entry.Title.Value : BookUiText.Get("Chronicle.Locked"), unlocked ? entry.IconType : ItemID.None, unlocked, unlocked && !reader.IsRead(entry.Id));
			}
			if (chapter == "places")
				foreach (BookLoreNode node in nodes)
				{
					bool unlocked = BookLore.NodeUnlocked(node, world);
					string title = unlocked && !node.Title.IsEmpty ? node.Title.Value : BookUiText.Get("Chronicle.Locked");
					AddChronicleRow(book, NodePrefix + node.Id, title, unlocked ? ItemID.WorldGlobe : ItemID.None, unlocked, unlocked && !reader.IsRead(NodePrefix + node.Id));
				}
		}
		if (entries.Count == 0 && nodes.Count == 0)
			book.Index.Add(new BookText(BookUiText.Get("Chronicle.Empty"), .8f, InkSoft));

		if (selectedEntry != null)
			BuildCodexPage(book, selectedEntry, world);
		else if (selectedNode != null)
			BuildNodePage(book, selectedNode, world, reader);
		else
			BuildMainLinePage(book, world);
	}

	private void AddChronicleRow(BookSpread book, string id, string title, int icon, bool unlocked, bool isNew)
	{
		var row = new BookIndexRow(title, null, icon, NPCID.None, id == selectedChronicle, !unlocked, isNew ? BookUiText.Get("Chronicle.New") : null, id == MainLineId);
		row.OnLeftClick += (_, _) => { SoundEngine.PlaySound(SoundID.MenuTick); SelectChronicle(id); };
		book.Index.Add(row);
	}

	private static void BuildCodexPage(BookSpread book, BookLoreEntry entry, BookLoreWorld world)
	{
		if (!BookLore.Unlocked(entry.Unlock, world))
		{
			book.AddHeading(BookUiText.Get("Chronicle.Locked"), BookLore.ChapterTitle(entry.Chapter));
			var locked = new BookSection();
			locked.Add(new BookText(entry.LockedHint.IsEmpty ? BookUiText.Get("Chronicle.LockedDefault") : BookLore.Text(entry.LockedHint), .84f, ResidentBookJournal.Muted));
			book.Page.Add(locked);
			return;
		}
		book.AddHeading(entry.Title.Value, BookLore.ChapterTitle(entry.Chapter));
		var page = new BookSection();
		page.Add(new BookChapterTitle(entry.IconType, NPCID.None, entry.Title.Value, BookLore.ChapterTitle(entry.Chapter)));
		page.Add(new BookText(BookLore.Text(entry.Body), .82f, ResidentBookJournal.Ink));
		book.Page.Add(page);
	}

	private static void BuildNodePage(BookSpread book, BookLoreNode node, BookLoreWorld world, BookTrackerPlayer reader)
	{
		if (!BookLore.NodeUnlocked(node, world))
		{
			book.AddHeading(BookUiText.Get("Chronicle.Locked"), BookLore.ChapterTitle("places"));
			book.Page.Add(new BookText(BookUiText.Get("Chronicle.NodeLocked"), .84f, ResidentBookJournal.Muted));
			return;
		}
		book.AddHeading(node.Title.Value, BookLore.ChapterTitle("places"));
		if (!node.Body.IsEmpty)
		{
			var body = new BookSection();
			body.Add(new BookText(BookLore.Text(node.Body), .82f, ResidentBookJournal.Ink));
			book.Page.Add(body);
		}
		book.Page.Add(new BookText(BookUiText.Get("Chronicle.NodeResidents"), .96f, ResidentBookJournal.Ink));
		foreach (string key in node.Residents)
		{
			if (!NpcTownManifest.TryGet(key, out _))
				continue;
			int completed = world.CompletedOf(key), length = PersonalResidentQuestCatalog.Length(key);
			string name = NpcQuestSystem.ResidentName(key);
			BookLoreResident lore = BookLore.Resident(key);
			// Pages open with the story: epilogue after 5/5, the introduction once a stage is done.
			string text = lore == null ? string.Empty
				: completed >= length && !lore.Epilogue.IsEmpty ? BookLore.Text(lore.Epilogue, name)
				: completed >= 1 ? BookLore.Text(lore.Intro, name) : string.Empty;
			if (text.Length == 0)
				text = BookUiText.Get("Chronicle.ResidentUnknown");
			NpcResidentRegistry.TryGetType(key, out int type);
			book.Page.Add(new BookSlotRow(new QuestTargetIcon(0, type, 38, true), BookUiText.Format("Chronicle.ResidentLine", name, completed, length), .86f, text));
		}
	}

	private static readonly (string Force, Color Fill)[] LeanColors =
	{
		("gardeners", new Color(89, 147, 49)),
		("tiders", new Color(52, 112, 168)),
		("archivists", new Color(146, 96, 46))
	};

	/// <summary>"Главная линия этого мира": the 20 drawn main residents, X/100 stages, the current act and the force lean.</summary>
	private static void BuildMainLinePage(BookSpread book, BookLoreWorld world)
	{
		book.AddHeading(BookUiText.Get("Chronicle.MainLine.Title"), BookUiText.Get("Chronicle.MainLine.Body"));
		var progress = new BookSection();
		progress.Add(new BookMeterRow(BookUiText.Get("Chronicle.MainLine.Progress"), world.MainStages / (float)Math.Max(1, world.MainTotal), new Color(196, 146, 48),
			BookUiText.Format("Count", world.MainStages, world.MainTotal)));
		BookLoreEntry act = BookLore.CurrentAct(world);
		progress.Add(new BookText(act != null ? BookUiText.Format("Chronicle.MainLine.Act", act.Title.Value) : BookUiText.Get("Chronicle.MainLine.NoAct"), .86f, ResidentBookJournal.Ink));
		book.Page.Add(progress);

		var lean = new BookSection();
		lean.Add(new BookText(BookUiText.Get("Chronicle.MainLine.Lean"), .96f, ResidentBookJournal.Ink));
		int total = world.Gardeners + world.Tiders + world.Archivists;
		foreach ((string force, Color fill) in LeanColors)
		{
			int value = force == "gardeners" ? world.Gardeners : force == "tiders" ? world.Tiders : world.Archivists;
			lean.Add(new BookMeterRow(BookLore.ForceName(force), value / (float)Math.Max(1, total), fill, value.ToString()));
		}
		lean.Add(new BookText(total == 0 ? BookUiText.Get("Chronicle.MainLine.LeanEmpty")
			: BookUiText.Format("Chronicle.MainLine.Leaning", BookLore.ForceName(world.Leaning)), .78f, ResidentBookJournal.Muted));
		book.Page.Add(lean);

		book.Page.Add(new BookText(BookUiText.Get("Chronicle.MainLine.Residents"), .96f, ResidentBookJournal.Ink));
		var keys = new List<string>(Main.gameMenu ? new HashSet<string>() : ResidentStoryRoles.MainSet(Main.worldID));
		keys.Sort((a, b) => string.Compare(NpcQuestSystem.ResidentName(a), NpcQuestSystem.ResidentName(b), StringComparison.CurrentCulture));
		foreach (string key in keys)
		{
			if (!NpcTownManifest.TryGet(key, out _))
				continue;
			int completed = world.CompletedOf(key), length = PersonalResidentQuestCatalog.Length(key);
			NpcResidentRegistry.TryGetType(key, out int type);
			string line = BookUiText.Format("RoleHover", NpcQuestSystem.ResidentName(key), BookLore.ForceName(PersonalResidentQuestCatalog.Force(key)));
			book.Page.Add(new BookSlotRow(new QuestTargetIcon(0, type, 38, true), line, .84f, null, completed / (float)Math.Max(1, length), BookUiText.Format("Count", completed, length)));
		}
	}
}
