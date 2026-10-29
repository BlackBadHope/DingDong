using System;
using System.Collections.Generic;
using System.Text.Json;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>Localized pair from the lore contract "dingdong-lore-v1" ({"ru": ..., "en": ...}).</summary>
internal readonly struct BookLoreText
{
	private readonly string ru;
	private readonly string en;
	internal BookLoreText(string ru, string en)
	{
		this.ru = ru ?? string.Empty;
		this.en = en ?? string.Empty;
	}

	internal bool IsEmpty => string.IsNullOrEmpty(ru) && string.IsNullOrEmpty(en);

	/// <summary>Text in the client's language (Russian for ru, English otherwise); the other one if missing.</summary>
	internal string Value
	{
		get
		{
			string a = BookLore.Russian ? ru : en, b = BookLore.Russian ? en : ru;
			return !string.IsNullOrEmpty(a) ? a : b ?? string.Empty;
		}
	}
}

internal sealed class BookLoreChoice
{
	internal BookLoreText Outcome;
	internal string Lean = "none";
}

internal sealed class BookLoreStage
{
	internal int Index;
	internal string TitleRu = string.Empty, Kind = string.Empty;
	internal BookLoreText Description, Offer, Complete, Waiting;
	internal Dictionary<string, BookLoreChoice> Choices;
	internal BookLoreChoice Choice(string id) => id != null && Choices != null && Choices.TryGetValue(id, out BookLoreChoice c) ? c : null;
}

internal sealed class BookLoreResident
{
	internal string NpcKey = string.Empty, Force = string.Empty, Node = string.Empty;
	internal BookLoreText Epithet, Quote, Intro, FindHint, Epilogue;
	internal BookLoreStage[] Stages = Array.Empty<BookLoreStage>();
	internal BookLoreStage Stage(int index)
	{
		foreach (BookLoreStage stage in Stages)
			if (stage.Index == index)
				return stage;
		return null;
	}
}

internal sealed class BookLoreNode
{
	internal string Id = string.Empty;
	internal BookLoreText Title, Body;
	internal readonly List<string> Residents = new();
}

internal sealed class BookLoreChapter
{
	internal string Id = string.Empty;
	internal int Order;
	internal BookLoreText Title;
}

internal enum BookLoreCondKind { Never, Always, MainStages, Stages, ForceStages, Resident, Choice, Lean, Flag, All, Any }

internal sealed class BookLoreCond
{
	internal static readonly BookLoreCond Never = new() { Kind = BookLoreCondKind.Never };
	internal BookLoreCondKind Kind;
	internal int Count, Stage;
	internal string Force = string.Empty, NpcKey = string.Empty, Id = string.Empty, Value = string.Empty;
	internal BookLoreCond[] Children = Array.Empty<BookLoreCond>();
}

internal sealed class BookLoreEntry
{
	internal string Id = string.Empty, Chapter = string.Empty, Icon = string.Empty;
	internal int Order;
	internal BookLoreText Title, Body, LockedHint;
	internal BookLoreCond Unlock = BookLoreCond.Never;
	private int iconType = -1;
	internal int IconType => iconType >= 0 ? iconType : iconType = BookLore.ResolveItem(Icon);
}

/// <summary>World facts the codex conditions are evaluated against (client side, from synchronized state).</summary>
internal sealed class BookLoreWorld
{
	internal readonly Dictionary<string, int> Completed = new(StringComparer.Ordinal);
	internal readonly Dictionary<string, int> ForceStages = new(StringComparer.Ordinal);
	internal readonly Dictionary<string, string> Choices = new(StringComparer.Ordinal); // "npcKey:stage" -> choice id
	internal int MainStages, MainTotal, AllStages, Gardeners, Tiders, Archivists;
	internal string Leaning = "balance";
	internal int CompletedOf(string npcKey) => npcKey != null && Completed.TryGetValue(npcKey, out int value) ? value : 0;
}

/// <summary>
/// UI-side read model of the lore contract (Quests/Lore/&lt;node&gt;.json and codex.json).
/// Lenient by design: a missing file or field only leaves that text empty, the book then
/// falls back to its own captions. The strict contract check belongs to the lore track.
/// </summary>
internal static class BookLore
{
	internal static readonly string[] NodeFiles = { "town", "arts", "nature", "wanderers", "ocean", "alchemist", "calamity", "craftsmen", "fargo", "thorium" };
	internal static readonly string[] ChapterOrder = { "world", "forces", "arcs", "finale", "places" };
	private static readonly Dictionary<string, BookLoreResident> Residents = new(StringComparer.Ordinal);
	private static readonly List<BookLoreNode> NodeList = new();
	private static readonly List<BookLoreChapter> ChapterList = new();
	private static readonly List<BookLoreEntry> EntryList = new();
	private static bool loaded;
	private static int warnings;
	private static BookLoreWorld cachedWorld;
	private static uint cachedRevision = uint.MaxValue;
	private static ulong cachedTick;

	internal static bool Russian => Language.ActiveCulture.CultureInfo.TwoLetterISOLanguageName == "ru";
	internal static IReadOnlyList<BookLoreNode> Nodes { get { EnsureLoaded(); return NodeList; } }
	internal static IReadOnlyList<BookLoreChapter> Chapters { get { EnsureLoaded(); return ChapterList; } }
	internal static IReadOnlyList<BookLoreEntry> Entries { get { EnsureLoaded(); return EntryList; } }
	internal static int Warnings { get { EnsureLoaded(); return warnings; } }

	internal static void Reset()
	{
		Residents.Clear();
		NodeList.Clear();
		ChapterList.Clear();
		EntryList.Clear();
		loaded = false;
		warnings = 0;
		cachedWorld = null;
		cachedRevision = uint.MaxValue;
	}

	internal static void ForgetWorld()
	{
		cachedWorld = null;
		cachedRevision = uint.MaxValue;
	}

	internal static BookLoreResident Resident(string npcKey)
	{
		EnsureLoaded();
		return npcKey != null && Residents.TryGetValue(npcKey, out BookLoreResident value) ? value : null;
	}

	internal static BookLoreStage Stage(string npcKey, int index) => Resident(npcKey)?.Stage(index);

	/// <summary>Replaces the contract tokens; called while building UI, never while drawing.</summary>
	internal static string Fill(string text, string residentName)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;
		if (text.Contains("{player}", StringComparison.Ordinal))
			text = text.Replace("{player}", Main.LocalPlayer?.name ?? string.Empty, StringComparison.Ordinal);
		if (text.Contains("{resident}", StringComparison.Ordinal))
			text = text.Replace("{resident}", residentName ?? string.Empty, StringComparison.Ordinal);
		return text;
	}

	internal static string Text(BookLoreText text, string residentName = null) => Fill(text.Value, residentName);

	internal static string ChapterTitle(string id)
	{
		foreach (BookLoreChapter chapter in Chapters)
			if (chapter.Id == id && !chapter.Title.IsEmpty)
				return chapter.Title.Value;
		return BookUiText.Get("Chronicle.Chapters." + id);
	}

	internal static string ForceName(string force) => BookUiText.Get("Forces." + (string.IsNullOrEmpty(force) ? "unassigned" : force));

	internal static int ResolveItem(string key)
	{
		if (string.IsNullOrEmpty(key))
			return ItemID.Book;
		try
		{
			if (ItemID.Search.TryGetId(key, out int type) && type > 0)
				return type;
			if (ModContent.TryFind<ModItem>(key, out ModItem item))
				return item.Type;
		}
		catch (Exception)
		{
			// Unknown or malformed icon names fall back to a book glyph.
		}
		return ItemID.Book;
	}

	private static void EnsureLoaded()
	{
		if (loaded)
			return;
		loaded = true;
		foreach (string node in NodeFiles)
			LoadNode(node);
		LoadCodex();
		EntryList.Sort((a, b) => a.Chapter != b.Chapter ? Array.IndexOf(ChapterOrder, a.Chapter).CompareTo(Array.IndexOf(ChapterOrder, b.Chapter)) : a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Id, b.Id));
		ChapterList.Sort((a, b) => a.Order.CompareTo(b.Order));
		if (warnings > 0)
			DingDongBiomeHomes.Instance?.Logger.Warn($"DINGDONG_BOOK_LORE|warnings={warnings}|residents={Residents.Count}|codex={EntryList.Count}");
	}

	private static JsonDocument Open(string name)
	{
		string path = "Quests/Lore/" + name + ".json";
		Mod mod = DingDongBiomeHomes.Instance;
		try
		{
			if (mod == null || !mod.FileExists(path))
				return null;
			return JsonDocument.Parse(mod.GetFileBytes(path), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
		}
		catch (Exception ex)
		{
			warnings++;
			mod?.Logger.Warn("Book lore file is unreadable: " + path + " — " + ex.Message);
			return null;
		}
	}

	private static void LoadNode(string name)
	{
		using JsonDocument document = Open(name);
		if (document == null)
			return;
		JsonElement root = document.RootElement;
		var node = new BookLoreNode { Id = name };
		if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("nodePage", out JsonElement page))
		{
			node.Title = Text(page, "title");
			node.Body = Text(page, "body");
		}
		if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("residents", out JsonElement list) && list.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement row in list.EnumerateArray())
			{
				string key = Str(row, "npcKey");
				if (key.Length == 0 || Residents.ContainsKey(key))
				{
					warnings++;
					continue;
				}
				var resident = new BookLoreResident
				{
					NpcKey = key, Force = Str(row, "force"), Node = name,
					Epithet = Text(row, "epithet"), Quote = Text(row, "quote"), Intro = Text(row, "intro"),
					FindHint = Text(row, "findHint"), Epilogue = Text(row, "epilogue")
				};
				var stages = new List<BookLoreStage>();
				if (row.TryGetProperty("stages", out JsonElement steps) && steps.ValueKind == JsonValueKind.Array)
					foreach (JsonElement step in steps.EnumerateArray())
						stages.Add(ReadStage(step));
				resident.Stages = stages.ToArray();
				Residents.Add(key, resident);
				node.Residents.Add(key);
			}
		}
		NodeList.Add(node);
	}

	private static BookLoreStage ReadStage(JsonElement step)
	{
		var stage = new BookLoreStage
		{
			Index = Int(step, "index", -1), TitleRu = Str(step, "titleRu"), Kind = Str(step, "kind"),
			Description = Text(step, "description"), Offer = Text(step, "offer"), Complete = Text(step, "complete"), Waiting = Text(step, "waiting")
		};
		if (step.ValueKind == JsonValueKind.Object && step.TryGetProperty("choices", out JsonElement choices) && choices.ValueKind == JsonValueKind.Object)
		{
			stage.Choices = new Dictionary<string, BookLoreChoice>(StringComparer.Ordinal);
			foreach (JsonProperty choice in choices.EnumerateObject())
				stage.Choices[choice.Name] = new BookLoreChoice { Outcome = Text(choice.Value, "outcome"), Lean = Str(choice.Value, "lean") is { Length: > 0 } lean ? lean : "none" };
		}
		return stage;
	}

	private static void LoadCodex()
	{
		using JsonDocument document = Open("codex");
		if (document == null || document.RootElement.ValueKind != JsonValueKind.Object)
			return;
		JsonElement root = document.RootElement;
		if (root.TryGetProperty("chapters", out JsonElement chapters) && chapters.ValueKind == JsonValueKind.Array)
			foreach (JsonElement c in chapters.EnumerateArray())
				ChapterList.Add(new BookLoreChapter { Id = Str(c, "id"), Order = Int(c, "order", 0), Title = Text(c, "title") });
		if (root.TryGetProperty("entries", out JsonElement entries) && entries.ValueKind == JsonValueKind.Array)
		{
			var seen = new HashSet<string>(StringComparer.Ordinal);
			foreach (JsonElement e in entries.EnumerateArray())
			{
				var entry = new BookLoreEntry
				{
					Id = Str(e, "id"), Chapter = Str(e, "chapter"), Order = Int(e, "order", 0), Icon = Str(e, "icon"),
					Title = Text(e, "title"), Body = Text(e, "body"), LockedHint = Text(e, "lockedHint"),
					Unlock = e.TryGetProperty("unlock", out JsonElement unlock) ? Cond(unlock) : BookLoreCond.Never
				};
				if (entry.Id.Length == 0 || !seen.Add(entry.Id) || Array.IndexOf(ChapterOrder, entry.Chapter) < 0)
				{
					warnings++;
					continue;
				}
				EntryList.Add(entry);
			}
		}
	}

	private static BookLoreCond Cond(JsonElement e)
	{
		if (e.ValueKind != JsonValueKind.Object)
			return BookLoreCond.Never;
		foreach (JsonProperty p in e.EnumerateObject())
		{
			JsonElement v = p.Value;
			switch (p.Name)
			{
				case "always":
					return v.ValueKind == JsonValueKind.True ? new BookLoreCond { Kind = BookLoreCondKind.Always } : BookLoreCond.Never;
				case "mainStages":
					return new BookLoreCond { Kind = BookLoreCondKind.MainStages, Count = Number(v) };
				case "stages":
					return new BookLoreCond { Kind = BookLoreCondKind.Stages, Count = Number(v) };
				case "forceStages":
					return new BookLoreCond { Kind = BookLoreCondKind.ForceStages, Force = Str(v, "force"), Count = Int(v, "count", int.MaxValue) };
				case "resident":
					return new BookLoreCond { Kind = BookLoreCondKind.Resident, NpcKey = Str(v, "npcKey"), Stage = Int(v, "stage", int.MaxValue) };
				case "choice":
					return new BookLoreCond { Kind = BookLoreCondKind.Choice, NpcKey = Str(v, "npcKey"), Stage = Int(v, "stage", -1), Id = Str(v, "id") };
				case "lean":
					return new BookLoreCond { Kind = BookLoreCondKind.Lean, Value = v.ValueKind == JsonValueKind.String ? v.GetString() : string.Empty };
				case "flag":
					return new BookLoreCond { Kind = BookLoreCondKind.Flag, Value = v.ValueKind == JsonValueKind.String ? v.GetString() : string.Empty };
				case "all":
				case "any":
					var children = new List<BookLoreCond>();
					if (v.ValueKind == JsonValueKind.Array)
						foreach (JsonElement child in v.EnumerateArray())
							children.Add(Cond(child));
					return new BookLoreCond { Kind = p.Name == "all" ? BookLoreCondKind.All : BookLoreCondKind.Any, Children = children.ToArray() };
			}
		}
		warnings++;
		return BookLoreCond.Never;
	}

	private static string Str(JsonElement e, string name) =>
		e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : string.Empty;

	private static int Number(JsonElement v) => v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n) ? n : int.MaxValue;

	private static int Int(JsonElement e, string name, int fallback) =>
		e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n) ? n : fallback;

	private static BookLoreText Text(JsonElement e, string name)
	{
		if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out JsonElement v) || v.ValueKind != JsonValueKind.Object)
			return default;
		return new BookLoreText(Str(v, "ru"), Str(v, "en"));
	}

	// ---------------------------------------------------------------- codex state

	/// <summary>World facts for the local view; rebuilt when the quest revision changes (and at most once a second for choices).</summary>
	internal static BookLoreWorld World(Player player)
	{
		NpcQuestPlayer quests = player.GetModPlayer<NpcQuestPlayer>();
		if (cachedWorld != null && cachedRevision == quests.Revision && Main.GameUpdateCount - cachedTick < 60UL)
			return cachedWorld;
		cachedRevision = quests.Revision;
		cachedTick = Main.GameUpdateCount;
		var world = new BookLoreWorld();
		HashSet<string> main = Main.gameMenu ? new HashSet<string>(StringComparer.Ordinal) : ResidentStoryRoles.MainSet(Main.worldID);
		var progressByKey = new Dictionary<string, ResidentQuestProgress>(StringComparer.Ordinal);
		foreach ((string npcKey, ResidentQuestProgress progress) in quests.CurrentSnapshot())
			progressByKey[npcKey] = progress;
		foreach (NpcTownIdentity npc in NpcTownManifest.All)
		{
			string key = npc.NpcKey;
			int length = PersonalResidentQuestCatalog.Length(key);
			progressByKey.TryGetValue(key, out ResidentQuestProgress progress);
			int completed = Math.Clamp(progress?.Completed ?? 0, 0, length);
			world.Completed[key] = completed;
			world.AllStages += completed;
			string force = PersonalResidentQuestCatalog.Force(key);
			force = string.IsNullOrEmpty(force) ? "unassigned" : force;
			world.ForceStages[force] = (world.ForceStages.TryGetValue(force, out int sum) ? sum : 0) + completed;
			bool isMain = main.Contains(key);
			if (isMain)
			{
				world.MainStages += completed;
				world.MainTotal += length;
			}
			for (int stage = 0; stage < length; stage++)
			{
				string choice = BookQuestBridge.Choice(key, stage, progress);
				if (choice == null)
					continue;
				world.Choices[key + ":" + stage] = choice;
				// A pick sways the finale once its stage is turned in; legacy markers stay neutral.
				if (!isMain || stage >= completed || NpcQuestWorldLedger.IsLegacyChoice(choice))
					continue;
				switch (Stage(key, stage)?.Choice(choice)?.Lean)
				{
					case "gardeners": world.Gardeners++; break;
					case "tiders": world.Tiders++; break;
					case "archivists": world.Archivists++; break;
				}
			}
		}
		int top = Math.Max(world.Gardeners, Math.Max(world.Tiders, world.Archivists));
		int winners = (world.Gardeners == top ? 1 : 0) + (world.Tiders == top ? 1 : 0) + (world.Archivists == top ? 1 : 0);
		world.Leaning = top == 0 || winners > 1 ? "balance" : world.Gardeners == top ? "gardeners" : world.Tiders == top ? "tiders" : "archivists";
		cachedWorld = world;
		return world;
	}

	internal static bool Unlocked(BookLoreCond cond, BookLoreWorld world)
	{
		switch (cond.Kind)
		{
			case BookLoreCondKind.Always: return true;
			case BookLoreCondKind.MainStages: return world.MainStages >= LoreCodex.MainThreshold(cond.Count, world.MainTotal);
			case BookLoreCondKind.Stages: return world.AllStages >= cond.Count;
			case BookLoreCondKind.ForceStages: return world.ForceStages.TryGetValue(cond.Force, out int sum) && sum >= cond.Count;
			case BookLoreCondKind.Resident: return world.CompletedOf(cond.NpcKey) >= cond.Stage;
			case BookLoreCondKind.Choice: return world.CompletedOf(cond.NpcKey) > cond.Stage && world.Choices.TryGetValue(cond.NpcKey + ":" + cond.Stage, out string id) && (id == cond.Id || NpcQuestWorldLedger.IsLegacyChoice(id));
			case BookLoreCondKind.Lean: return world.MainTotal > 0 && world.MainStages >= world.MainTotal && world.Leaning == cond.Value;
			case BookLoreCondKind.Flag: return Flag(cond.Value);
			case BookLoreCondKind.All:
				foreach (BookLoreCond child in cond.Children)
					if (!Unlocked(child, world))
						return false;
				return cond.Children.Length > 0;
			case BookLoreCondKind.Any:
				foreach (BookLoreCond child in cond.Children)
					if (Unlocked(child, world))
						return true;
				return false;
			default: return false;
		}
	}

	private static bool Flag(string name) => name switch
	{
		"hardMode" => Main.hardMode,
		"downedBoss1" => NPC.downedBoss1,
		"downedBoss2" => NPC.downedBoss2,
		"downedBoss3" => NPC.downedBoss3,
		"downedQueenBee" => NPC.downedQueenBee,
		"downedMechBossAny" => NPC.downedMechBossAny,
		"downedPlantBoss" => NPC.downedPlantBoss,
		"downedGolemBoss" => NPC.downedGolemBoss,
		"downedAncientCultist" => NPC.downedAncientCultist,
		"downedMoonlord" => NPC.downedMoonlord,
		_ => false
	};

	/// <summary>A node page opens when any of its residents finished at least one stage.</summary>
	internal static bool NodeUnlocked(BookLoreNode node, BookLoreWorld world)
	{
		foreach (string key in node.Residents)
			if (world.CompletedOf(key) >= 1)
				return true;
		return false;
	}

	/// <summary>The last unlocked act of the main line (chapter "arcs"), or null.</summary>
	internal static BookLoreEntry CurrentAct(BookLoreWorld world)
	{
		BookLoreEntry act = null;
		foreach (BookLoreEntry entry in Entries)
			if (entry.Chapter == "arcs" && Unlocked(entry.Unlock, world))
				act = entry;
		return act;
	}
}

public sealed class BookLoreSystem : ModSystem
{
	/// <summary>Wires the book to the quest engine: saved choices, abandon and paused claims.</summary>
	public override void Load()
	{
		BookQuestBridge.ChoiceOf = static (npcKey, stage) => NpcQuestWorldLedger.TryGetChoice(npcKey, stage, out string id) && !string.IsNullOrEmpty(id) ? id : null;
		BookQuestBridge.RequestAbandon = NpcQuestNetwork.RequestQuestAbandon;
		BookQuestBridge.IsPaused = static npcKey => !Main.gameMenu && Main.LocalPlayer is { active: true } local
			&& local.GetModPlayer<NpcQuestPlayer>().TryGetCurrent(npcKey, out ResidentQuestProgress p) && p.Active && p.Paused;
	}

	public override void Unload()
	{
		BookLore.Reset();
		BookQuestBridge.Reset();
	}

	public override void OnWorldUnload() => BookLore.ForgetWorld();
}
