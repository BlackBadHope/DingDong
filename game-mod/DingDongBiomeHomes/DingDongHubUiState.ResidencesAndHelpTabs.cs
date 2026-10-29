using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;
internal sealed partial class DingDongHubUiState
{
	// "Жители": only residences this character has discovered; the page shows the portrait,
	// the biome, the residence status (protected until relocation / relocation complete) and
	// the coordinates, plus a shortcut to the resident's quest page.
	private void BuildResidences(BookSpread book)
	{
		ResidenceDiscoveryPlayer discovery = Main.LocalPlayer.GetModPlayer<ResidenceDiscoveryPlayer>();
		List<SettlementResidenceReport> known = SettlementWorldState.Residences
			.Where(residence => discovery.HasDiscovered(residence.ReviewId))
			.OrderBy(residence => NpcQuestSystem.ResidentName(residence.NpcKey), StringComparer.CurrentCulture).ToList();
		book.AddIndexCaption(BookUiText.Format("Residents.Summary", known.Count, NpcTownManifest.AuthoredCount));
		if (known.Count == 0)
		{
			book.AddHeading(Text("Residences.EmptyTitle"), null);
			book.Page.Add(new BookText(Text("Residences.EmptyBody"), .84f, Ink));
			return;
		}
		if (!known.Any(r => r.ReviewId == selectedResidence))
			selectedResidence = known[0].ReviewId;
		foreach (SettlementResidenceReport residence in known)
		{
			int reviewId = residence.ReviewId;
			NpcResidentRegistry.TryGetType(residence.NpcKey, out int npcType);
			var row = new BookIndexRow(NpcQuestSystem.ResidentName(residence.NpcKey), Text($"Biomes.{residence.Biome}"), ItemID.None, npcType,
				reviewId == selectedResidence, golden: ResidentStoryRoles.IsMain(residence.NpcKey));
			row.OnLeftClick += (_, _) => { SoundEngine.PlaySound(SoundID.MenuTick); SelectResidence(reviewId); };
			book.Index.Add(row);
		}

		SettlementResidenceReport chosen = known.First(r => r.ReviewId == selectedResidence);
		string key = chosen.NpcKey;
		NpcResidentRegistry.TryGetType(key, out int type);
		int percent = 0;
		if (NpcTownManifest.TryGet(key, out NpcTownIdentity identity) && Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>().TryGetCurrent(key, out ResidentQuestProgress progress))
			percent = ResidentTrustPolicy.Percent(progress, identity);
		string name = NpcQuestSystem.ResidentName(key);
		BookLoreResident lore = BookLore.Resident(key);
		string epithet = lore != null ? BookLore.Text(lore.Epithet, name) : string.Empty;
		book.Page.Add(new BookResidentHeader(key, percent, type, epithet, null, ResidentStoryRoles.IsMain(key)));
		book.Page.Add(new BookStoryBadge(ResidentStoryRoles.IsMain(key)));
		bool moved = NpcResidenceWorldSystem.IsOriginReleased(key);
		var facts = new BookSection();
		facts.Add(new BookText(BookUiText.Format("Residents.Biome", Text($"Biomes.{chosen.Biome}")), .86f, Ink));
		facts.Add(new BookText(BookUiText.Format("Residents.House", chosen.ReviewId), .86f, Ink));
		facts.Add(new BookText(BookUiText.Format("Residents.Coords", chosen.Area.Center.X, chosen.Area.Center.Y), .86f, Ink));
		var status = new BookSlotRow(new BookAssetIcon(moved ? BookGlyph.Residents : BookGlyph.Residence), BookUiText.Get(moved ? "ResidenceMoved" : "ResidenceProtected"), .86f,
			BookUiText.Get(moved ? "Residents.MovedNote" : "Residents.ProtectedNote"));
		facts.Add(status);
		book.Page.Add(facts);
		var open = new BookActionButton(BookUiText.Get("Residents.OpenQuests"), .86f) { UseSteel = true, TextColor = Color.White };
		open.Width.Set(0, 1); open.Height.Set(44, 0);
		open.OnLeftClick += (_, _) => { SoundEngine.PlaySound(SoundID.MenuOpen); ShowResidentQuests(key); };
		book.Page.Add(open);
	}

	private static readonly (string Title, string Body, int Item)[] HelpTopics =
	{
		("Hub.Help.ControlsTitle", "Hub.Help.ControlsBody", ItemID.Wrench),
		("Book.Help.Quests.Title", "Book.Help.Quests.Body", ItemID.Book),
		("Book.Help.Chronicle.Title", "Book.Help.Chronicle.Body", ItemID.SpellTome),
		("Book.Help.Tracker.Title", "Book.Help.Tracker.Body", ItemID.Compass),
		("Book.Help.Residents.Title", "Book.Help.Residents.Body", ItemID.LivingWoodWand),
		("Hub.Help.ServerTitle", "Hub.Help.ServerBody", ItemID.DefenderMedal)
	};

	private void BuildHelp(BookSpread book)
	{
		book.AddIndexCaption(Text("Help.Title"));
		selectedHelp = Math.Clamp(selectedHelp, 0, HelpTopics.Length - 1);
		for (int i = 0; i < HelpTopics.Length; i++)
		{
			int topic = i;
			var row = new BookIndexRow(Language.GetTextValue("Mods.DingDongBiomeHomes." + HelpTopics[i].Title), null, HelpTopics[i].Item, NPCID.None, i == selectedHelp);
			row.OnLeftClick += (_, _) => { SoundEngine.PlaySound(SoundID.MenuTick); SelectHelp(topic); };
			book.Index.Add(row);
		}
		var chosen = HelpTopics[selectedHelp];
		book.AddHeading(Language.GetTextValue("Mods.DingDongBiomeHomes." + chosen.Title), selectedHelp == 0 ? Text("Help.Subtitle") : null);
		book.Page.Add(CreateInfoCard(Language.GetTextValue("Mods.DingDongBiomeHomes." + chosen.Title), Language.GetTextValue("Mods.DingDongBiomeHomes." + chosen.Body), DamageGuideTone.Information, chosen.Item));
	}
}
