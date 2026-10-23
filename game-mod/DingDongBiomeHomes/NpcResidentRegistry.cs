using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal static class NpcResidentRegistry
{
	private static readonly Dictionary<int, string> KeysByType = new();
	private static readonly Dictionary<string, int> TypesByKey = new(StringComparer.Ordinal);
	private static bool initialized;

	internal static int ResolvedCount
	{
		get
		{
			EnsureInitialized();
			return TypesByKey.Count;
		}
	}

	internal static bool TryGet(NPC npc, out string npcKey, out NpcTownIdentity identity)
	{
		if (npc is null)
		{
			npcKey = string.Empty;
			identity = null;
			return false;
		}

		EnsureInitialized();
		if (KeysByType.TryGetValue(npc.type, out npcKey) && NpcTownManifest.TryGet(npcKey, out identity))
			return true;

		npcKey = string.Empty;
		identity = null;
		return false;
	}

	internal static bool TryGetType(string npcKey, out int npcType)
	{
		EnsureInitialized();
		return TypesByKey.TryGetValue(npcKey, out npcType);
	}

	internal static void Reset()
	{
		initialized = false;
		KeysByType.Clear();
		TypesByKey.Clear();
	}

	private static void EnsureInitialized()
	{
		if (initialized)
			return;
		initialized = true;

		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			if (!TryResolveNpcType(identity.NpcKey, out int npcType))
				continue;
			KeysByType[npcType] = identity.NpcKey;
			TypesByKey[identity.NpcKey] = npcType;
		}
	}

	private static bool TryResolveNpcType(string npcKey, out int npcType)
	{
		const string vanillaPrefix = "Terraria/";
		if (npcKey.StartsWith(vanillaPrefix, StringComparison.Ordinal))
			return NPCID.Search.TryGetId(npcKey[vanillaPrefix.Length..], out npcType);

		if (ModContent.TryFind(npcKey, out ModNPC modNpc))
		{
			npcType = modNpc.Type;
			return true;
		}

		npcType = NPCID.None;
		return false;
	}
}
