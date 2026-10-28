using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace DingDongBiomeHomes;

// Roadmap item 8 (owner reference "Центр DingDong", 27.10): small book widgets that bring the
// quest page closer to the reference — role icons in the resident list, framed objective and
// performer slots, a story-line badge. Everything here is built on Reload/Recalculate; DrawSelf
// only draws cached values (no per-frame allocations).
internal static class BookUiText
{
	internal static string Get(string key) => Language.GetTextValue("Mods.DingDongBiomeHomes.Book." + key);
}

/// <summary>Profession glyph shown in the right column of a resident row (reference: leaf, gear, coins, heart, paw...).</summary>
internal static class ResidentRoleIcons
{
	private static readonly Dictionary<string, int> ByResident = new(StringComparer.Ordinal)
	{
		["Terraria/Guide"] = ItemID.Book,
		["Terraria/Merchant"] = ItemID.GoldCoin,
		["Terraria/Nurse"] = ItemID.Heart,
		["Terraria/Dryad"] = ItemID.Acorn,
		["Terraria/GoblinTinkerer"] = ItemID.Cog,
		["Terraria/BestiaryGirl"] = ItemID.Bunny,
		["Terraria/Clothier"] = ItemID.Silk,
		["Terraria/Painter"] = ItemID.Paintbrush,
		["Terraria/WitchDoctor"] = ItemID.JungleSpores,
		["Terraria/DD2Bartender"] = ItemID.Ale,
		["Terraria/Wizard"] = ItemID.CrystalBall,
		["Terraria/ArmsDealer"] = ItemID.MusketBall,
		["Terraria/DyeTrader"] = ItemID.DyeVat,
		["Terraria/Steampunker"] = ItemID.Clentaminator,
		["Terraria/Cyborg"] = ItemID.RocketI,
		["Terraria/Pirate"] = ItemID.Cannonball,
		["Terraria/Demolitionist"] = ItemID.Dynamite,
		["Terraria/PartyGirl"] = ItemID.Confetti,
		["Terraria/Golfer"] = ItemID.GolfBall,
		["Terraria/Mechanic"] = ItemID.Wrench,
		["Terraria/TaxCollector"] = ItemID.PlatinumCoin,
		["Terraria/Stylist"] = ItemID.HairDyeRemover,
		["Terraria/Truffle"] = ItemID.GlowingMushroom,
		["Terraria/SantaClaus"] = ItemID.Present,
		["Terraria/Angler"] = ItemID.WoodFishingPole,
		["Terraria/Princess"] = ItemID.Diamond,
		["FishermanNPC/Fisherman"] = ItemID.WoodFishingPole,
		["UnuBattleRods/Fishylady"] = ItemID.WoodFishingPole,
		["ThoriumMod/Cook"] = ItemID.CookingPot,
		["ThoriumMod/Blacksmith"] = ItemID.IronAnvil,
		["ThoriumMod/Diverman"] = ItemID.DivingHelmet,
		["ThoriumMod/Tracker"] = ItemID.Compass,
		["AlchemistNPCLite/Alchemist"] = ItemID.Bottle,
		["AlchemistNPCLite/Brewer"] = ItemID.Bottle,
		["AlchemistNPCLite/YoungBrewer"] = ItemID.Bottle,
		["AlchemistNPCLite/Jeweler"] = ItemID.Ruby,
		["Fargowiltas/LumberJack"] = ItemID.CopperAxe,
		["Fargowiltas/Squirrel"] = ItemID.Squirrel
	};

	/// <summary>Residents without a hand-picked glyph use their story force (ResidentStories.json).</summary>
	internal static int Get(string npcKey, string force)
	{
		if (npcKey != null && ByResident.TryGetValue(npcKey, out int item))
			return item;
		return force switch
		{
			"archivists" => ItemID.Book,
			"tiders" => ItemID.Seashell,
			"gardeners" => ItemID.Acorn,
			"tricksters" => ItemID.WhoopieCushion,
			_ => ItemID.Compass
		};
	}
}

/// <summary>Item sprite with an optional custom tooltip and tint (the role glyph, the story star).</summary>
internal sealed class BookItemGlyph : UIElement
{
	private readonly int itemType;
	private readonly string hover;
	private readonly Color tint;
	private readonly float maxSize;
	internal BookItemGlyph(int itemType, float maxSize, string hover, Color tint)
	{
		this.itemType = itemType; this.maxSize = maxSize; this.hover = hover; this.tint = tint;
		Width.Set(maxSize, 0); Height.Set(maxSize, 0);
	}

	protected override void DrawSelf(SpriteBatch batch)
	{
		if (itemType <= ItemID.None || itemType >= TextureAssets.Item.Length)
			return;
		Main.instance.LoadItem(itemType);
		Texture2D texture = TextureAssets.Item[itemType].Value;
		Rectangle frame = Main.itemAnimations[itemType]?.GetFrame(texture) ?? texture.Frame();
		CalculatedStyle d = GetDimensions();
		float room = Math.Max(1f, Math.Min(maxSize, Math.Min(d.Width, d.Height)));
		float scale = Math.Min(2f, room / Math.Max(frame.Width, frame.Height));
		using (BookPixelSampler.Use(batch))
			batch.Draw(texture, d.Center(), frame, tint, 0f, frame.Size() * .5f, scale, SpriteEffects.None, 0f);
		if (IsMouseHovering && !string.IsNullOrEmpty(hover))
			Main.hoverItemName = hover;
	}
}

/// <summary>Head of the player who carries the quest; nothing is drawn when that player is offline.</summary>
internal sealed class BookPlayerHead : UIElement
{
	private readonly string playerName;
	internal BookPlayerHead(string playerName) { this.playerName = playerName ?? ""; IgnoresMouseInteraction = true; }

	protected override void DrawSelf(SpriteBatch batch)
	{
		if (playerName.Length == 0)
			return;
		Player owner = null;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player candidate = Main.player[i];
			if (candidate != null && candidate.active && string.Equals(candidate.name, playerName, StringComparison.Ordinal))
			{
				owner = candidate;
				break;
			}
		}
		if (owner == null)
			return;
		CalculatedStyle d = GetDimensions();
		try
		{
			Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, owner, d.Center(), 1f, Math.Clamp(d.Height / 36f, .6f, 1.4f), Color.Transparent);
		}
		catch (Exception)
		{
			// The head renderer depends on render-target state; a missing head must never break the book.
		}
	}
}

/// <summary>Reference sub-card: a framed slot with an icon on the left, text (and optionally a bar) on the right.</summary>
internal sealed class BookSlotRow : UIElement
{
	private const float Slot = 46f;
	private readonly UIElement icon;
	private readonly BookText main;
	private readonly BookText sub;
	private readonly BookProgressBar bar;
	internal BookSlotRow(UIElement icon, string mainText, float mainScale, string subText = null, float progress = -1f)
	{
		Width.Set(0, 1); Height.Set(Slot + 8f, 0); SetPadding(4f);
		this.icon = icon;
		icon.Left.Set(4f, 0); icon.Width.Set(Slot - 8f, 0); icon.Height.Set(Slot - 8f, 0);
		Append(icon);
		main = new BookText(mainText, mainScale, ResidentBookJournal.Ink);
		Append(main);
		if (progress >= 0f)
		{
			bar = new BookProgressBar(progress);
			bar.Height.Set(14f, 0);
			Append(bar);
		}
		if (!string.IsNullOrEmpty(subText))
		{
			sub = new BookText(subText, .74f, ResidentBookJournal.Muted);
			Append(sub);
		}
	}

	public override void Recalculate()
	{
		base.Recalculate();
		float left = Slot + 10f;
		float y = 2f;
		main.Left.Set(left, 0); main.Width.Set(-left - 4f, 1); main.Top.Set(y, 0); main.Recalculate();
		y += main.GetOuterDimensions().Height + 4f;
		if (bar != null) { bar.Left.Set(left, 0); bar.Width.Set(-left - 8f, 1); bar.Top.Set(y, 0); y += 20f; }
		if (sub != null) { sub.Left.Set(left, 0); sub.Width.Set(-left - 4f, 1); sub.Top.Set(y, 0); sub.Recalculate(); y += sub.GetOuterDimensions().Height; }
		float content = Math.Max(Slot, y);
		icon.Top.Set((content - (Slot - 8f)) * .5f, 0);
		Height.Set(content + PaddingTop + PaddingBottom, 0);
		base.Recalculate();
	}

	protected override void DrawSelf(SpriteBatch batch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		DingDongUiAssets.DrawBookCard(batch, area, new Color(255, 248, 228) * .92f);
		CalculatedStyle inner = GetInnerDimensions();
		var slot = new Rectangle((int)inner.X, (int)(inner.Y + (inner.Height - Slot) * .5f), (int)Slot, (int)Slot);
		DingDongUiTheme.DrawPanel(batch, slot, new Color(214, 190, 142), new Color(128, 96, 54));
	}
}

/// <summary>Story-line badge: a star for the world's main storyline, a plain label for side stories.</summary>
internal sealed class BookStoryBadge : UIElement
{
	internal BookStoryBadge(bool main)
	{
		Width.Set(0, 1); Height.Set(28f, 0);
		float left = 0f;
		if (main)
		{
			var star = new BookItemGlyph(ItemID.FallenStar, 24f, null, Color.White);
			star.Top.Set(2f, 0);
			Append(star);
			left = 30f;
		}
		var text = new BookText(BookUiText.Get(main ? "MainStory" : "SideStory"), .84f, main ? DingDongUiTheme.Gold : ResidentBookJournal.Muted);
		text.Left.Set(left, 0); text.Top.Set(3f, 0); text.Width.Set(-left, 1);
		Append(text);
	}
}
