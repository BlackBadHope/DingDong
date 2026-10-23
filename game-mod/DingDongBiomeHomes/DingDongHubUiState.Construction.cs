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
		contentTitle.SetText(snapshot.Title);
		contentSubtitle.SetText(snapshot.Subtitle);
		contentCards.Add(CreateInfoCard(Text("Quests.SummaryTitle"), snapshot.Summary, DamageGuideTone.Information, ItemID.Book));
		foreach (QuestJournalEntry entry in snapshot.Entries)
			contentCards.Add(CreateQuestCard(entry));
		lastQuestRevision = Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>().Revision;
		lastQuestRefreshTick = Main.GameUpdateCount;
	}

	private void BuildCombat()
	{
		DamageGuidePage page = DamageGuideContent.Build(selectedDamagePage, Main.LocalPlayer);
		contentTitle.SetText(page.Title);
		contentSubtitle.SetText(page.Subtitle);
		contentCards.Add(CreateDamageNavigation());
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

			contentCards.Add(CreateInfoCard(block.Heading, block.Body, block.Tone, itemType, npcType));
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
