using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
/// <summary>
/// Keeps authored residence discoveries private to a character and world.
/// Player files are shared by all worlds, so every saved key contains the
/// world's stable GUID as well as the canonical residence review id.
/// </summary>
public sealed class ResidenceDiscoveryPlayer : ModPlayer
{
	private const string SaveKey = "discoveredAuthoredResidences";
	private const int DiscoveryHorizontalMarginTiles = 28;
	private const int DiscoveryVerticalMarginTiles = 20;
	private readonly HashSet<string> discoveries = new(StringComparer.Ordinal);
	// PerfOpt 28.10: HasDiscovered runs for every active authored town NPC in every
	// map/minimap/overlay frame (ResidenceDiscoveryMapSystem.DrawMap). Answer it from an
	// int set for the current world instead of allocating a Guid string + key (184 B) per call.
	private static Guid cachedWorldGuid;
	private static string cachedWorldId = string.Empty;
	private readonly HashSet<int> worldDiscoveries = new();
	private string worldDiscoveriesFor;
	private int discoveriesVersion, worldDiscoveriesVersion = -1;
	public override void Initialize() { discoveries.Clear(); discoveriesVersion++; }
	public override void PostUpdate()
	{
		if (Main.dedServ || Player.whoAmI != Main.myPlayer || !Player.active || Player.dead || Main.gameMenu || Main.GameUpdateCount % 30 != 0 || !SettlementWorldState.HasCompleteResidenceRegistry || !TryCurrentWorldId(out string worldId))
		{
			return;
		}

		Point playerTile = new((int)(Player.Center.X / 16f), (int)(Player.Center.Y / 16f));
		foreach (SettlementResidenceReport residence in SettlementWorldState.Residences)
		{
			if (HasDiscovered(worldId, residence.ReviewId))
				continue;
			Rectangle discoveryArea = residence.Area;
			discoveryArea.Inflate(DiscoveryHorizontalMarginTiles, DiscoveryVerticalMarginTiles);
			if (!discoveryArea.Contains(playerTile))
				continue;
			discoveries.Add(DiscoveryKey(worldId, residence.ReviewId));
			discoveriesVersion++; // PerfOpt 28.10
			string residentName = ResidenceDiscoveryMapSystem.ResidentDisplayName(residence.ReviewId, residence.NpcKey);
			Main.NewText(Language.GetTextValue("Mods.DingDongBiomeHomes.ResidenceDiscovery.Discovered", residentName), new Color(126, 218, 185));
		}
	}

	public bool HasDiscovered(int reviewId) => TryCurrentWorldId(out string worldId) && HasDiscovered(worldId, reviewId);
	public override void SaveData(TagCompound tag)
	{
		if (discoveries.Count > 0)
			tag[SaveKey] = discoveries.OrderBy(value => value, StringComparer.Ordinal).ToList();
	}

	public override void LoadData(TagCompound tag)
	{
		discoveries.Clear();
		discoveriesVersion++; // PerfOpt 28.10
		if (!tag.ContainsKey(SaveKey))
			return;
		foreach (string value in tag.GetList<string>(SaveKey))
		{
			if (IsValidDiscoveryKey(value))
				discoveries.Add(value);
		}
	}

	private bool HasDiscovered(string worldId, int reviewId)
	{
		// PerfOpt 28.10: rebuild the per-world int view only when the world or the
		// persisted key set changed; discoveries stays the saved source of truth.
		if (!ReferenceEquals(worldId, worldDiscoveriesFor) || worldDiscoveriesVersion != discoveriesVersion)
		{
			worldDiscoveries.Clear();
			foreach (string key in discoveries)
			{
				if (key.Length > worldId.Length + 1 && key[worldId.Length] == ':' &&
					key.StartsWith(worldId, StringComparison.Ordinal) &&
					int.TryParse(key.AsSpan(worldId.Length + 1), out int id))
					worldDiscoveries.Add(id);
			}
			worldDiscoveriesFor = worldId;
			worldDiscoveriesVersion = discoveriesVersion;
		}
		return worldDiscoveries.Contains(reviewId);
	}
	private static bool TryCurrentWorldId(out string worldId)
	{
		Guid uniqueId = Main.ActiveWorldFileData?.UniqueId ?? Guid.Empty;
		if (uniqueId == Guid.Empty)
		{
			worldId = string.Empty;
			return false;
		}

		// PerfOpt 28.10: format the GUID once per world, not once per call.
		if (uniqueId != cachedWorldGuid)
		{
			cachedWorldGuid = uniqueId;
			cachedWorldId = uniqueId.ToString("N");
		}
		worldId = cachedWorldId;
		return true;
	}

	private static string DiscoveryKey(string worldId, int reviewId) => $"{worldId}:{reviewId}";
	private static bool IsValidDiscoveryKey(string value)
	{
		if (string.IsNullOrEmpty(value))
			return false;
		int separator = value.LastIndexOf(':');
		return separator == 32 && Guid.TryParseExact(value[..separator], "N", out _) && int.TryParse(value[(separator + 1)..], out int reviewId) && reviewId >= 1 && reviewId <= NpcTownManifest.AuthoredCount;
	}
}
