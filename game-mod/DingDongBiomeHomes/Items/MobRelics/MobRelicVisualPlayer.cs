using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal readonly record struct MobRelicPlayerVisual(int ItemType, MobRelicTier Tier);

internal sealed class MobRelicVisualPlayer : ModPlayer
{
	private readonly List<MobRelicPlayerVisual> visibleRelics = new();

	internal IReadOnlyList<MobRelicPlayerVisual> VisibleRelics => visibleRelics;

	public override void ResetEffects() => visibleRelics.Clear();

	internal void RegisterVisible(int itemType, MobRelicTier tier)
	{
		if (itemType <= 0 || visibleRelics.Exists(entry => entry.ItemType == itemType))
			return;
		visibleRelics.Add(new MobRelicPlayerVisual(itemType, tier));
	}
}

internal sealed class MobRelicPlayerDrawLayer : PlayerDrawLayer
{
	public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

	public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
		drawInfo.shadow == 0f
		&& drawInfo.drawPlayer.TryGetModPlayer(out MobRelicVisualPlayer visualPlayer)
		&& visualPlayer.VisibleRelics.Count > 0;

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		if (drawInfo.shadow != 0f
			|| !drawInfo.drawPlayer.TryGetModPlayer(out MobRelicVisualPlayer visualPlayer)
			|| visualPlayer.VisibleRelics.Count == 0)
		{
			return;
		}

		Player player = drawInfo.drawPlayer;
		IReadOnlyList<MobRelicPlayerVisual> relics = visualPlayer.VisibleRelics;
		float spin = player.miscCounter / 90f;
		Vector2 center = drawInfo.Center - Main.screenPosition + new Vector2(0f, -4f);
		for (int index = 0; index < relics.Count; index++)
		{
			MobRelicPlayerVisual relic = relics[index];
			if (relic.ItemType < 0 || relic.ItemType >= TextureAssets.Item.Length)
				continue;
			Asset<Texture2D> asset = TextureAssets.Item[relic.ItemType];
			if (asset == null || !asset.IsLoaded)
				continue;
			Texture2D texture = asset.Value;
			if (texture.Width <= 0 || texture.Height <= 0)
				continue;

			float angle = spin + MathHelper.TwoPi * index / Math.Max(1, relics.Count);
			float radius = 24f + 2f * (int)relic.Tier;
			Vector2 orbit = new(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * 0.48f);
			float scale = (16f + (int)relic.Tier) / Math.Max(texture.Width, texture.Height);
			Color drawColor = Color.Lerp(Color.White, TierColor(relic.Tier), 0.35f);
			drawColor *= 0.82f + 0.18f * MathF.Sin(spin * 2f + index * 1.7f);
			Vector2 drawPosition = (center + orbit).Floor();
			float rotation = -angle * 0.12f;
			drawInfo.DrawDataCache.Add(new DrawData(
				texture,
				drawPosition,
				null,
				drawColor,
				rotation,
				new Vector2(texture.Width, texture.Height) * 0.5f,
				scale,
				SpriteEffects.None,
				0));
		}
	}

	private static Color TierColor(MobRelicTier tier) => tier switch
	{
		MobRelicTier.CommonTrace => new Color(105, 165, 255),
		MobRelicTier.CommonCurio => new Color(100, 225, 145),
		MobRelicTier.ExpertSeal => new Color(255, 175, 70),
		MobRelicTier.MasterRelic => new Color(255, 90, 130),
		MobRelicTier.LegendaryMyth => new Color(205, 110, 255),
		_ => Color.White
	};
}
