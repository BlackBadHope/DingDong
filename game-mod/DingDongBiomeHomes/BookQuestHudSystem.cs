using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;

/// <summary>
/// HUD for the quest book: up to three tracked residents (BookTrackerPlayer) in a small
/// parchment card, and quest markers above residents — gold "!" for an available main-story
/// stage, silver "!" for a side stage, "?" when the local player can turn in. Everything is
/// computed on a timer from synchronized state (client only); Draw only reads cached arrays
/// and constant strings, so drawing allocates nothing and never restarts the SpriteBatch.
/// </summary>
public sealed class BookQuestHudSystem : ModSystem
{
	private enum Marker : byte { None, MainOffer, SideOffer, TurnIn }

	private const int MarkerPeriod = 20, TrackerPeriod = 30;
	private const float TrackerWidth = 270f, EntryHeight = 54f, HeaderHeight = 26f;
	private static readonly int[] markerNpc = new int[Main.maxNPCs];
	private static readonly Marker[] markerKind = new Marker[Main.maxNPCs];
	private static int markerCount;
	private static readonly string[] trackName = new string[BookTrackerPlayer.MaxTracked];
	private static readonly string[] trackLine = new string[BookTrackerPlayer.MaxTracked];
	private static readonly string[] trackCount = new string[BookTrackerPlayer.MaxTracked];
	private static readonly string[] trackKey = new string[BookTrackerPlayer.MaxTracked];
	private static readonly float[] trackFraction = new float[BookTrackerPlayer.MaxTracked];
	private static readonly float[] trackCountWidth = new float[BookTrackerPlayer.MaxTracked];
	private static readonly bool[] trackReady = new bool[BookTrackerPlayer.MaxTracked];
	private static int trackCountTotal;
	// Lines of info accessories (PDA, Cell Phone, ...) vanilla prints under the minimap at the
	// right edge while the inventory is closed; refreshed on the tracker timer, not in Draw.
	private static int infoLines;
	private static string trackerTitle = string.Empty;
	private static uint lastQuestRevision = uint.MaxValue, lastTrackerRevision = uint.MaxValue;
	private static readonly ResidentQuestProgress Empty = new();
	private const string Exclamation = "!", Question = "?";

	public override void ClearWorld()
	{
		markerCount = 0;
		trackCountTotal = 0;
		lastQuestRevision = lastTrackerRevision = uint.MaxValue;
	}

	public override void PostUpdateEverything()
	{
		if (Main.dedServ || Main.gameMenu || Main.LocalPlayer == null || !Main.LocalPlayer.active)
			return;
		try
		{
			if (Main.GameUpdateCount % MarkerPeriod == 0)
				RebuildMarkers(Main.LocalPlayer);
			var quests = Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>();
			var tracker = Main.LocalPlayer.GetModPlayer<BookTrackerPlayer>();
			if (Main.GameUpdateCount % TrackerPeriod == 0 || quests.Revision != lastQuestRevision || tracker.Revision != lastTrackerRevision)
			{
				lastQuestRevision = quests.Revision;
				lastTrackerRevision = tracker.Revision;
				RebuildTracker(Main.LocalPlayer, tracker);
			}
			if (Main.GameUpdateCount % TrackerPeriod == 0)
				infoLines = Math.Clamp(InfoDisplayLoader.ActiveDisplays(), 0, 16);
		}
		catch (Exception ex)
		{
			// Presentation only: a failure here must never interrupt the game update.
			markerCount = trackCountTotal = 0;
			Mod.Logger.Debug("Book HUD refresh skipped: " + ex.Message);
		}
	}

	private static void RebuildMarkers(Player viewer)
	{
		markerCount = 0;
		if (!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort)
			return;
		var quests = viewer.GetModPlayer<NpcQuestPlayer>();
		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (!npc.townNPC || !NpcResidentRegistry.TryGet(npc, out string key, out NpcTownIdentity identity))
				continue;
			ResidentEscortStage stage = NpcResidenceWorldSystem.GetState(key).Stage;
			if (stage is ResidentEscortStage.Following or ResidentEscortStage.Waiting or ResidentEscortStage.AwaitingHome)
				continue;
			ResidentQuestProgress progress = quests.TryGetCurrent(key, out ResidentQuestProgress current) ? current : Empty;
			Marker kind = ResidentBookJournal.StatusOf(viewer, identity, progress) switch
			{
				BookQuestStatus.Available => ResidentStoryRoles.IsMain(key) ? Marker.MainOffer : Marker.SideOffer,
				BookQuestStatus.Ready => Marker.TurnIn,
				_ => Marker.None
			};
			if (kind == Marker.None)
				continue;
			markerNpc[markerCount] = npc.whoAmI;
			markerKind[markerCount] = kind;
			markerCount++;
		}
	}

	private static void RebuildTracker(Player viewer, BookTrackerPlayer tracker)
	{
		trackCountTotal = 0;
		trackerTitle = BookUiText.Get("Tracker.Title");
		var quests = viewer.GetModPlayer<NpcQuestPlayer>();
		DynamicSpriteFont font = FontAssets.MouseText.Value;
		IReadOnlyList<string> keys = tracker.Tracked;
		for (int i = 0; i < keys.Count && trackCountTotal < BookTrackerPlayer.MaxTracked; i++)
		{
			string key = keys[i];
			if (!NpcTownManifest.TryGet(key, out NpcTownIdentity npc))
				continue;
			ResidentQuestProgress p = quests.TryGetCurrent(key, out ResidentQuestProgress current) ? current : Empty;
			if (PersonalResidentQuestCatalog.Complete(key, p.Completed))
				continue;
			ResidentQuestDefinition d = p.Active ? p.Definition(npc) : PersonalResidentQuestCatalog.Resolve(npc, p.Completed);
			ResidentStoryStep step = PersonalResidentQuestCatalog.Step(d.StoryKey);
			if (step == null)
				continue;
			BookQuestStatus status = ResidentBookJournal.StatusOf(viewer, npc, p);
			int progress = d.Kind is ResidentQuestKind.DeliverSupplies or ResidentQuestKind.Expedition ? NpcQuestSystem.QuestInventoryCount(viewer, d, p) : p.Progress;
			int slot = trackCountTotal++;
			trackKey[slot] = key;
			trackName[slot] = Fit(font, NpcQuestSystem.ResidentName(key), .8f, TrackerWidth - 24f);
			string line = status switch
			{
				BookQuestStatus.Available => BookUiText.Format("Tracker.Available", step.Title),
				BookQuestStatus.Busy or BookQuestStatus.Paused => BookUiText.Format("Tracker.Busy", step.Title, p.AssignedName),
				BookQuestStatus.Find => BookUiText.Get("Status.Find"),
				_ => step.Title
			};
			trackLine[slot] = Fit(font, line, .7f, TrackerWidth - 24f);
			trackReady[slot] = status == BookQuestStatus.Ready;
			trackFraction[slot] = p.Active ? Math.Clamp(progress / (float)Math.Max(1, d.Goal), 0f, 1f) : 0f;
			trackCount[slot] = status == BookQuestStatus.Ready ? BookUiText.Get("Tracker.Ready") : ResidentBookJournal.CountText(step, d, p.Active ? progress : 0);
			trackCountWidth[slot] = font.MeasureString(trackCount[slot]).X * .68f;
		}
	}

	/// <summary>Cuts a line to the tracker width once, while rebuilding (never while drawing).</summary>
	private static string Fit(DynamicSpriteFont font, string text, float scale, float width)
	{
		if (string.IsNullOrEmpty(text) || font.MeasureString(text).X * scale <= width)
			return text ?? string.Empty;
		int cut = text.Length;
		while (cut > 1 && font.MeasureString(text[..cut] + "...").X * scale > width)
			cut--;
		return text[..cut].TrimEnd() + "...";
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int bars = layers.FindIndex(layer => layer.Name == "Vanilla: Entity Health Bars");
		layers.Insert(bars >= 0 ? bars : 0, new LegacyGameInterfaceLayer("DingDongBiomeHomes: Quest Markers", DrawMarkers, InterfaceScaleType.Game));
		int resources = layers.FindIndex(layer => layer.Name == "Vanilla: Resource Bars");
		layers.Insert(resources >= 0 ? resources + 1 : layers.Count, new LegacyGameInterfaceLayer("DingDongBiomeHomes: Quest Tracker", DrawTracker, InterfaceScaleType.UI));
	}

	private static bool DrawMarkers()
	{
		if (markerCount == 0 || Main.gameMenu || Main.mapFullscreen)
			return true;
		SpriteBatch batch = Main.spriteBatch;
		DynamicSpriteFont font = FontAssets.DeathText.Value;
		float bob = MathF.Sin(Main.GlobalTimeWrappedHourly * 3.2f) * 3f;
		for (int i = 0; i < markerCount; i++)
		{
			NPC npc = Main.npc[markerNpc[i]];
			if (!npc.active)
				continue;
			Marker kind = markerKind[i];
			string glyph = kind == Marker.TurnIn ? Question : Exclamation;
			Color color = kind switch
			{
				Marker.MainOffer => new Color(255, 205, 60),
				Marker.SideOffer => new Color(214, 220, 232),
				_ => new Color(255, 236, 110)
			};
			Vector2 size = font.MeasureString(glyph) * .62f;
			Vector2 at = new Vector2(npc.Center.X, npc.position.Y - 22f + npc.gfxOffY + bob) - Main.screenPosition - new Vector2(size.X * .5f, size.Y);
			if (at.X < -40 || at.Y < -40 || at.X > Main.screenWidth + 40 || at.Y > Main.screenHeight + 40)
				continue;
			for (int o = 0; o < 4; o++)
			{
				Vector2 offset = o switch { 0 => new Vector2(-2, 0), 1 => new Vector2(2, 0), 2 => new Vector2(0, -2), _ => new Vector2(0, 2) };
				batch.DrawString(font, glyph, at + offset, Color.Black * .85f, 0f, Vector2.Zero, .62f, SpriteEffects.None, 0f);
			}
			batch.DrawString(font, glyph, at, color, 0f, Vector2.Zero, .62f, SpriteEffects.None, 0f);
		}
		return true;
	}

	private static bool DrawTracker()
	{
		if (trackCountTotal == 0 || Main.gameMenu || Main.playerInventory || Main.inFancyUI || Main.ingameOptionsWindow || Main.mapFullscreen || Main.LocalPlayer.dead)
			return true;
		SpriteBatch batch = Main.spriteBatch;
		DynamicSpriteFont font = FontAssets.MouseText.Value;
		float screenWidth = Main.screenWidth / Main.UIScale;
		float top = Main.mapEnabled && Main.mapStyle == 1 ? Main.miniMapY + Main.miniMapHeight + 70f : 110f;
		// Below the info-accessory lines (22 px each, 20 on short screens) instead of over them,
		// and clear of the mana column at the right edge; kept on screen when the list is long.
		top += infoLines * (Main.screenHeight < 650 ? 20f : 22f) + (infoLines > 0 ? 10f : 0f);
		float height = HeaderHeight + trackCountTotal * EntryHeight + 10f;
		top = Math.Max(0f, Math.Min(top, Main.screenHeight / Main.UIScale - height - 12f));
		var area = new Rectangle((int)(screenWidth - TrackerWidth - 52f), (int)top, (int)TrackerWidth, (int)height);
		// Beside the minimap, top right, when it is shown and the card fits to its left.
		if (Main.mapEnabled && Main.mapStyle == 1 && Main.miniMapX - TrackerWidth - 14f >= 0f && Main.miniMapY + height <= Main.screenHeight / Main.UIScale)
			area = new Rectangle((int)(Main.miniMapX - TrackerWidth - 14f), Main.miniMapY, (int)TrackerWidth, (int)height);
		bool hovered = area.Contains(Main.MouseScreen.ToPoint());
		DingDongUiAssets.DrawBookCard(batch, area, Color.White * (hovered ? .98f : .86f));
		batch.DrawString(font, trackerTitle, new Vector2(area.X + 12, area.Y + 7), ResidentBookJournal.Ink, 0f, Vector2.Zero, .78f, SpriteEffects.None, 0f);
		for (int i = 0; i < trackCountTotal; i++)
		{
			float y = area.Y + HeaderHeight + i * EntryHeight;
			batch.DrawString(font, trackName[i], new Vector2(area.X + 12, y + 2), DingDongUiTheme.Gold, 0f, Vector2.Zero, .8f, SpriteEffects.None, 0f);
			batch.DrawString(font, trackLine[i], new Vector2(area.X + 12, y + 20), ResidentBookJournal.Ink, 0f, Vector2.Zero, .7f, SpriteEffects.None, 0f);
			var bar = new Rectangle(area.X + 12, (int)y + 40, (int)(TrackerWidth - 24 - trackCountWidth[i] - 8), 9);
			DingDongUiTheme.DrawPanel(batch, bar, new Color(112, 100, 76), new Color(71, 52, 29));
			var fill = new Rectangle(bar.X + 2, bar.Y + 2, (int)((bar.Width - 4) * trackFraction[i]), bar.Height - 4);
			DingDongUiTheme.Fill(batch, fill, trackReady[i] ? new Color(214, 160, 40) : new Color(89, 147, 49));
			batch.DrawString(font, trackCount[i], new Vector2(area.Right - 12 - trackCountWidth[i], y + 35), trackReady[i] ? new Color(170, 104, 18) : ResidentBookJournal.Ink, 0f, Vector2.Zero, .68f, SpriteEffects.None, 0f);
		}
		if (hovered)
		{
			// Clicking an entry opens that resident's quest page.
			Main.LocalPlayer.mouseInterface = true;
			int index = (int)((Main.MouseScreen.Y - area.Y - HeaderHeight) / EntryHeight);
			if (Main.mouseLeft && Main.mouseLeftRelease && index >= 0 && index < trackCountTotal)
			{
				Main.mouseLeftRelease = false;
				DingDongHubUiSystem.OpenResident(trackKey[index]);
			}
		}
		return true;
	}
}
