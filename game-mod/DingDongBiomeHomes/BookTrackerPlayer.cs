using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

/// <summary>
/// Per-character book preferences: up to three tracked residents per world (HUD tracker)
/// and the chronicle entries this character has already read ("new" marks). Pure client
/// presentation state; it never affects quest progress and is not synchronized.
/// </summary>
public sealed class BookTrackerPlayer : ModPlayer
{
	internal const int MaxTracked = 3;
	private const char Separator = '\u001f';
	private readonly List<string> tracked = new(); // "worldKey<US>npcKey"
	private readonly HashSet<string> readEntries = new(StringComparer.Ordinal);
	private readonly List<string> currentTracked = new(MaxTracked);
	private string currentWorld;
	internal uint Revision { get; private set; }

	private Guid keyGuid;
	private int keyWorldId = int.MinValue;
	private string worldKey;

	/// <summary>Current world key; the string is rebuilt only when the world changes.</summary>
	private string WorldKey()
	{
		Guid guid = Main.ActiveWorldFileData?.UniqueId ?? Guid.Empty;
		if (worldKey == null || guid != keyGuid || Main.worldID != keyWorldId)
		{
			keyGuid = guid;
			keyWorldId = Main.worldID;
			worldKey = guid != Guid.Empty ? guid.ToString("N") : Main.worldID.ToString();
		}
		return worldKey;
	}

	/// <summary>Tracked residents of the current world, in tracking order (cached; no allocation per call).</summary>
	internal IReadOnlyList<string> Tracked
	{
		get
		{
			string world = WorldKey();
			if (!ReferenceEquals(currentWorld, world))
			{
				currentWorld = world;
				RebuildCurrent();
			}
			return currentTracked;
		}
	}

	private void RebuildCurrent()
	{
		currentTracked.Clear();
		string prefix = currentWorld + Separator;
		foreach (string entry in tracked)
			if (entry.StartsWith(prefix, StringComparison.Ordinal) && currentTracked.Count < MaxTracked)
				currentTracked.Add(entry[prefix.Length..]);
	}

	internal bool IsTracked(string npcKey)
	{
		IReadOnlyList<string> list = Tracked;
		for (int i = 0; i < list.Count; i++)
			if (list[i] == npcKey)
				return true;
		return false;
	}

	/// <summary>Toggles tracking; returns false when the tracker is already full.</summary>
	internal bool ToggleTracked(string npcKey)
	{
		string entry = WorldKey() + Separator + npcKey;
		if (tracked.Remove(entry))
		{
			currentWorld = null;
			Revision++;
			return true;
		}
		if (Tracked.Count >= MaxTracked)
			return false;
		tracked.Add(entry);
		currentWorld = null;
		Revision++;
		return true;
	}

	/// <summary>Engine hook: a resident whose stage was just accepted is tracked when there is room.</summary>
	internal void TrackIfRoom(string npcKey)
	{
		if (!string.IsNullOrEmpty(npcKey) && !IsTracked(npcKey) && Tracked.Count < MaxTracked)
			ToggleTracked(npcKey);
	}

	internal void Untrack(string npcKey)
	{
		if (tracked.Remove(WorldKey() + Separator + npcKey))
		{
			currentWorld = null;
			Revision++;
		}
	}

	internal bool IsRead(string entryId) => readEntries.Contains(entryId);

	internal void MarkRead(string entryId)
	{
		if (!string.IsNullOrEmpty(entryId) && readEntries.Add(entryId))
			Revision++;
	}

	public override void SaveData(TagCompound tag)
	{
		if (tracked.Count > 0)
			tag["BookTracked"] = new List<string>(tracked);
		if (readEntries.Count > 0)
			tag["BookCodexRead"] = new List<string>(readEntries);
	}

	public override void LoadData(TagCompound tag)
	{
		tracked.Clear();
		readEntries.Clear();
		if (tag.ContainsKey("BookTracked"))
			foreach (string entry in tag.GetList<string>("BookTracked"))
				if (!string.IsNullOrEmpty(entry) && entry.IndexOf(Separator) > 0 && !tracked.Contains(entry))
					tracked.Add(entry);
		if (tag.ContainsKey("BookCodexRead"))
			foreach (string entry in tag.GetList<string>("BookCodexRead"))
				if (!string.IsNullOrEmpty(entry))
					readEntries.Add(entry);
		currentWorld = null;
		Revision++;
	}
}
