using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;

/// <summary>
/// Shows the current, player-specific residence quest objective on every map
	/// mode. The gold ring is a readable local search area; the pulsing red cross is
/// the actual objective center used by quest progress.
/// </summary>
public sealed class ResidenceQuestMapLayer : ModMapLayer
{
	private const int MinimumSearchWidthTiles = 96;
	private const int MinimumSearchHeightTiles = 64;
	private const int SearchGridTiles = 16;
	private const int MinimumRingRadiusTiles = 32;
	private const int MaximumRingRadiusTiles = 72;
	private static readonly Color SearchColor = new(255, 196, 74, 235);
	private static readonly Color TargetColor = new(255, 92, 92, 255);
	private static readonly SpriteFrame PixelFrame = new(1, 1, 0, 0);

	public override Position GetDefaultPosition() => new Before(IMapLayer.Pings);

	public override void Draw(ref MapOverlayDrawContext context, ref string text)
	{
		Player player = Main.LocalPlayer;
		if (Main.dedServ || Main.gameMenu || player is null || !player.active ||
			!SettlementWorldState.HasCompleteResidenceRegistry)
		{
			return;
		}

		// Several residents often point at the same authored home or city. Stacked
		// rings there became one unreadable blot, so nearby targets share a marker
		// and the hover text lists every objective behind it.
		var groups = new List<TargetGroup>();
		foreach ((string npcKey, ResidentQuestProgress progress) in
			player.GetModPlayer<NpcQuestPlayer>().CurrentActiveRecords())
		{
			if (!NpcQuestSystem.TryGetMapTarget(npcKey, progress, out Point target,
				out int radiusTiles, out string objective))
			{
				continue;
			}

			string label = Language.GetTextValue(
				"Mods.DingDongBiomeHomes.ResidenceDiscovery.QuestTarget",
				NpcQuestSystem.ResidentName(npcKey), objective);
			TargetGroup group = null;
			foreach (TargetGroup candidate in groups)
			{
				int dx = candidate.Target.X - target.X, dy = candidate.Target.Y - target.Y;
				if (dx * dx + dy * dy <= MergeDistanceTiles * MergeDistanceTiles) { group = candidate; break; }
			}
			if (group == null) groups.Add(group = new TargetGroup(target));
			group.RadiusTiles = Math.Max(group.RadiusTiles, radiusTiles);
			group.Labels.Add(label);
		}

		foreach (TargetGroup group in groups)
		{
			bool hovered = DrawDottedRing(ref context, group.Target, group.RadiusTiles, SearchColor);
			hovered |= DrawTargetCross(ref context, group.Target, group.RadiusTiles);
			if (hovered) text = string.Join("\n", group.Labels);
		}
	}

	private const int MergeDistanceTiles = 40;

	private sealed class TargetGroup
	{
		internal TargetGroup(Point target) { Target = target; }
		internal Point Target { get; }
		internal int RadiusTiles { get; set; }
		internal List<string> Labels { get; } = new();
	}

	internal static Rectangle ApproximateTargetArea(
		Point target,
		int radiusTiles,
		int worldWidthTiles,
		int worldHeightTiles)
	{
		int worldWidth = Math.Max(1, worldWidthTiles);
		int worldHeight = Math.Max(1, worldHeightTiles);
		int radius = Math.Max(1, radiusTiles);
		int halfWidth = Math.Max(MinimumSearchWidthTiles / 2, radius * 3);
		int halfHeight = Math.Max(MinimumSearchHeightTiles / 2, radius * 2);
		int left = FloorToGrid(target.X - halfWidth);
		int right = CeilingToGrid(target.X + halfWidth + 1);
		int top = FloorToGrid(target.Y - halfHeight);
		int bottom = CeilingToGrid(target.Y + halfHeight + 1);
		int width = Math.Min(worldWidth, Math.Max(1, right - left));
		int height = Math.Min(worldHeight, Math.Max(1, bottom - top));
		left = Math.Clamp(left, 0, worldWidth - width);
		top = Math.Clamp(top, 0, worldHeight - height);
		return new Rectangle(left, top, width, height);
	}

	private static int FloorToGrid(int value) =>
		(int)Math.Floor(value / (double)SearchGridTiles) * SearchGridTiles;

	private static int CeilingToGrid(int value) =>
		(int)Math.Ceiling(value / (double)SearchGridTiles) * SearchGridTiles;

	internal static int MapRingRadius(int objectiveRadiusTiles) =>
		Math.Clamp(Math.Max(1, objectiveRadiusTiles) * 2, MinimumRingRadiusTiles, MaximumRingRadiusTiles);

	private static bool DrawDottedRing(
		ref MapOverlayDrawContext context,
		Point center,
		int objectiveRadiusTiles,
		Color color)
	{
		// MagicPixel is stretched by the map transform and becomes a full-height
		// beam. MapPing is a real bounded icon in every map mode.
		float scale = 0.7f + MapRingRadius(objectiveRadiusTiles) / (float)MaximumRingRadiusTiles * 0.2f;
		return DrawPoint(ref context, TextureAssets.MapPing.Value, center.ToVector2(), color, scale, scale + 0.15f);
	}

	private static bool DrawTargetCross(
		ref MapOverlayDrawContext context,
		Point target,
		int radiusTiles)
	{
		float pulse = 0.28f + 0.06f * MathF.Sin((float)(Main.GameUpdateCount % 120) * MathHelper.TwoPi / 120f);
		return DrawPoint(ref context, TextureAssets.MapPing.Value, target.ToVector2(), TargetColor, pulse, pulse + 0.08f);
	}

	private static bool DrawPoint(
		ref MapOverlayDrawContext context,
		Texture2D texture,
		Vector2 tilePosition,
		Color color,
		float scale,
		float selectedScale) =>
		context.Draw(texture, tilePosition, color, PixelFrame, scale, selectedScale,
			Alignment.Center).IsMouseOver;
}
