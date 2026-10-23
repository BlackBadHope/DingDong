using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static partial class NpcQuestSystem
{
	internal static Point SurveyReferencePoint(Rectangle area, int variant)
	{
		int left = area.Left + Math.Max(2, area.Width / 5);
		int right = area.Right - Math.Max(3, area.Width / 5);
		int upper = area.Top + Math.Max(3, area.Height / 4);
		int lower = area.Bottom - Math.Max(4, area.Height / 4);
		int centerX = area.Left + area.Width / 2;
		int centerY = area.Top + area.Height / 2;
		return Math.Clamp(variant, 0, ResidentQuestCatalog.SurveyCount - 1) switch
		{
			0 => new Point(centerX, centerY),
			1 => new Point(left, centerY),
			2 => new Point(right, centerY),
			3 => new Point(centerX, upper),
			4 => new Point(centerX, lower),
			5 => new Point(left, upper),
			6 => new Point(right, upper),
			_ => new Point(centerX, area.Bottom - 3)};
	}

	private static int PatrolPointIndex(int variant, int progress)
	{
		int start = Math.Clamp(variant, 0, ResidentQuestCatalog.PatrolCount - 1) * 3 % 8;
		int direction = variant < ResidentQuestCatalog.PatrolCount / 2 ? 1 : -1;
		return (start + direction * Math.Max(0, progress) + 80) % 8;
	}

	internal static Point PatrolReferencePoint(Rectangle area, int pointIndex)
	{
		int left = area.Left - 3;
		int right = area.Right + 2;
		int top = area.Top - 3;
		int bottom = area.Bottom + 2;
		int centerX = area.Left + area.Width / 2;
		int centerY = area.Top + area.Height / 2;
		return pointIndex switch
		{
			0 => new Point(centerX, top),
			1 => new Point(right, top),
			2 => new Point(right, centerY),
			3 => new Point(right, bottom),
			4 => new Point(centerX, bottom),
			5 => new Point(left, bottom),
			6 => new Point(left, centerY),
			_ => new Point(left, top)};
	}

	internal static bool UsesResidenceMap(ResidentQuestKind kind) =>
		kind is ResidentQuestKind.SurveyOrigin
			or ResidentQuestKind.PatrolOrigin
			or ResidentQuestKind.DefendOrigin;

	internal static bool TryGetMapTarget(
		string npcKey,
		ResidentQuestProgress progress,
		out Point target,
		out int radiusTiles,
		out string objective)
	{
		target = Point.Zero;
		radiusTiles = 0;
		objective = string.Empty;
		if (progress is null
			|| !progress.Active
			|| !NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity)
			|| !SettlementWorldState.TryGetResidence(npcKey, out SettlementResidenceReport residence))
		{
			return false;
		}

		ResidentQuestDefinition definition = progress.Definition(identity);
		if (IsReady(Main.LocalPlayer, definition, progress) && TryGetResidentMapPosition(npcKey, out target))
		{
			radiusTiles = 8;
			objective = Language.GetTextValue(
				"Mods.DingDongBiomeHomes.Quests.Journal.ReturnToResident",
				ResidentName(npcKey));
			return true;
		}

		if (!UsesResidenceMap(definition.Kind))
			return false;

		target = definition.Kind switch
		{
			ResidentQuestKind.SurveyOrigin => residence.QuestPoint(false, definition.Variant),
			ResidentQuestKind.PatrolOrigin => residence.QuestPoint(true,
				PatrolPointIndex(definition.Variant, Math.Min(progress.Progress, definition.Goal - 1))),
			_ => residence.QuestPoint(false, 0)
		};
		radiusTiles = Math.Max(6, definition.RadiusTiles);
		objective = ObjectiveText(Main.LocalPlayer, definition, progress);
		return true;
	}

	private static bool TryGetResidentMapPosition(string npcKey, out Point target)
	{
		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (NpcResidentRegistry.TryGet(npc, out string activeKey, out _) &&
				string.Equals(activeKey, npcKey, StringComparison.Ordinal))
			{
				target = npc.Center.ToTileCoordinates();
				return true;
			}
		}

		target = Point.Zero;
		return false;
	}

	internal static Rectangle DefenseArea(SettlementResidenceReport residence, int radius)
	{
		Rectangle area = residence.Area;
		area.Inflate(radius, radius);
		return Rectangle.Intersect(area, new Rectangle(0, 0, Main.maxTilesX, Main.maxTilesY));
	}

	internal static Rectangle QuestMapSearchArea(string npcKey, ResidentQuestProgress progress, Point target, int radius)
	{
		if (NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity) &&
			SettlementWorldState.TryGetResidence(npcKey, out SettlementResidenceReport residence))
		{
			ResidentQuestDefinition definition = progress.Definition(identity);
			if (definition.Kind == ResidentQuestKind.DefendOrigin) return DefenseArea(residence, definition.RadiusTiles);
		}
		return ResidenceQuestMapLayer.ApproximateTargetArea(target, radius, Main.maxTilesX, Main.maxTilesY);
	}

	private static bool IsNear(Player player, Point target, int radiusTiles)
	{
		Point playerTile = player.Center.ToTileCoordinates();
		int dx = playerTile.X - target.X;
		int dy = playerTile.Y - target.Y;
		return dx * dx + dy * dy <= radiusTiles * radiusTiles;
	}

	private static void RecordProgress(Player player, string npcKey, ResidentQuestProgress progress)
	{
		NpcQuestWorldLedger.Record(player, npcKey, progress);
		NpcQuestNetwork.SendQuestSnapshot(player, player.whoAmI);
	}

	internal static int CountInventory(Player player, int itemType)
	{
		int count = 0;
		foreach (Item item in player.inventory)
		{
			if (item.type == itemType && item.stack > 0)
				count += item.stack;
		}

		return count;
	}

	private static void ConsumeInventory(Player player, int itemType, int count)
	{
		int remaining = count;
		for (int slot = 0; slot < player.inventory.Length && remaining > 0; slot++)
		{
			Item item = player.inventory[slot];
			if (item.type != itemType || item.stack <= 0)
				continue;
			int removed = Math.Min(remaining, item.stack);
			item.stack -= removed;
			remaining -= removed;
			if (item.stack <= 0)
				item.TurnToAir();
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, slot, item.prefix);
		}
	}
}
