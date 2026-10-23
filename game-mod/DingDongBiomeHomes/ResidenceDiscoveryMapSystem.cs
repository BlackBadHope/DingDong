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
/// Filters only the authored town-NPC heads during Terraria's own map draw.
/// The original draw routine remains responsible for icon appearance, hover
/// text, minimap clipping and compatibility with other map layers.
/// </summary>
public sealed class ResidenceDiscoveryMapSystem : ModSystem
{
	private static readonly Dictionary<int, int> ReviewIdByNpcType = new();
	private static readonly Dictionary<int, int> NpcTypeByReviewId = new();
	private delegate void OrigDrawMap(Main self, GameTime gameTime);
	private delegate void HookDrawMap(OrigDrawMap orig, Main self, GameTime gameTime);
	public override void Load()
	{
		if (Main.dedServ)
			return;
		MethodInfo drawMap = typeof(Main).GetMethod("DrawMap", BindingFlags.NonPublic | BindingFlags.Instance, binder: null, new[] { typeof(GameTime) }, modifiers: null) ?? throw new MissingMethodException(typeof(Main).FullName, "DrawMap");
		MonoModHooks.Add(drawMap, (HookDrawMap)DrawMap);
	}

	public override void PostSetupContent()
	{
		ReviewIdByNpcType.Clear();
		NpcTypeByReviewId.Clear();
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			if (!TryResolveNpcType(identity.NpcKey, out int npcType))
				continue;
			int reviewId = identity.Variant + 1;
			ReviewIdByNpcType.TryAdd(npcType, reviewId);
			NpcTypeByReviewId.TryAdd(reviewId, npcType);
		}
	}

	public override void Unload()
	{
		ReviewIdByNpcType.Clear();
		NpcTypeByReviewId.Clear();
	}

	internal static string ResidentDisplayName(int reviewId, string fallbackNpcKey)
	{
		if (NpcTypeByReviewId.TryGetValue(reviewId, out int npcType))
			return Lang.GetNPCNameValue(npcType);
		int separator = fallbackNpcKey.LastIndexOf('/');
		return separator >= 0 && separator + 1 < fallbackNpcKey.Length ? fallbackNpcKey[(separator + 1)..] : fallbackNpcKey;
	}

	private static void DrawMap(OrigDrawMap orig, Main self, GameTime gameTime)
	{
		if (!SettlementWorldState.HasCompleteResidenceRegistry || Main.LocalPlayer is null || !Main.LocalPlayer.active)
		{
			orig(self, gameTime);
			return;
		}

		ResidenceDiscoveryPlayer knowledge = Main.LocalPlayer.GetModPlayer<ResidenceDiscoveryPlayer>();
		List<NPC> hidden = new();
		try
		{
			for (int index = 0; index < Main.maxNPCs; index++)
			{
				NPC npc = Main.npc[index];
				if (!npc.active || !npc.townNPC || !ReviewIdByNpcType.TryGetValue(npc.type, out int reviewId) || knowledge.HasDiscovered(reviewId))
				{
					continue;
				}

				npc.townNPC = false;
				hidden.Add(npc);
			}

			orig(self, gameTime);
		}
		finally
		{
			foreach (NPC npc in hidden)
				npc.townNPC = true;
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
