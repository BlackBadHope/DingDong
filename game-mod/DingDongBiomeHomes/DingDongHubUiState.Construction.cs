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
	private void BuildQuests(QuestJournalSnapshot snapshot = null)
	{
		snapshot ??= QuestJournalContent.Build(Main.LocalPlayer);
		lastQuestSnapshot = snapshot;
        bookJournal ??= new ResidentBookJournal();
        contentPanel.Append(bookJournal);bookJournal.Reload();

		lastQuestRevision = Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>().Revision;
		lastQuestRefreshTick = Main.GameUpdateCount;
	}

	// Combat chapters: the index lists the guide pages, the right page shows the chosen one.
	// DamageGuideContent is unchanged; only the presentation moved into the book spread.
	private void BuildCombat(BookSpread book)
	{
		book.AddIndexCaption(TabLabel(DingDongHubTab.Combat));
		foreach (DamageGuidePageId id in DamageGuideContent.PageOrder)
		{
			DamageGuidePageId target = id;
			var row = new BookIndexRow(DamageGuideContent.Build(id, Main.LocalPlayer).Title, null, ItemID.None, NPCID.None, id == selectedDamagePage);
			row.OnLeftClick += (_, _) => { SoundEngine.PlaySound(SoundID.MenuTick); SelectDamagePage(target); };
			book.Index.Add(row);
		}
		DamageGuidePage page = DamageGuideContent.Build(selectedDamagePage, Main.LocalPlayer);
		book.AddHeading(page.Title, page.Subtitle);
		for (int i = 0; i < page.Blocks.Count; i++)
		{
			DamageGuideBlock block = page.Blocks[i];
			int itemType = ItemID.None;
			int npcType = NPCID.None;
			if (i == 0 && selectedDamagePage == DamageGuidePageId.CurrentWeapon)
			{
				Item held = Main.LocalPlayer.HeldItem;
				if (held != null && !held.IsAir)
					itemType = held.type;
			}
			else if (i == 0 && selectedDamagePage == DamageGuidePageId.LastHit && DamageGuideTelemetry.LastHit != null)
			{
				npcType = DamageGuideTelemetry.LastHit.TargetType;
			}

			book.Page.Add(CreateInfoCard(block.Heading, block.Body, block.Tone, itemType, npcType));
		}
	}

	private UIElement CreateDamageNavigation()
	{
		JournalFrame panel = Card(DamageGuideTone.Information);
		panel.Height.Set(52f, 0f);
		panel.SetPadding(8f);
		const float buttonWidth = 102f;
		UITextPanel<string> previous = CreateButton(Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.Guide.Navigation.Previous"), buttonWidth, 32f);
		previous.OnLeftClick += (_, _) => ChangeDamagePage(-1);
		panel.Append(previous);
		int pageNumber = Array.IndexOf(DamageGuideContent.PageOrder, selectedDamagePage) + 1;
		UIText indicator = new(Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.Guide.Navigation.Page", pageNumber, DamageGuideContent.PageOrder.Length), 0.84f)
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			TextColor = Ink
		};
		panel.Append(indicator);
		UITextPanel<string> next = CreateButton(Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.Guide.Navigation.Next"), buttonWidth, 32f);
		next.Left.Set(-buttonWidth, 1f);
		next.OnLeftClick += (_, _) => ChangeDamagePage(1);
		panel.Append(next);
		return panel;
	}
}
