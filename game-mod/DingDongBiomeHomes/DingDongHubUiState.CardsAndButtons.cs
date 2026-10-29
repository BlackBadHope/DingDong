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
	private static UIElement CreateQuestCard(QuestJournalEntry entry)
	{
		return new JournalContentCard(entry.Resident, entry.Body,
			entry.Ready ? DamageGuideTone.Positive : DamageGuideTone.Normal,
			entry.IconItemType, entry.IconNpcType, entry.RewardItemType);
	}

	private static UIElement CreateInfoCard(string heading, string body, DamageGuideTone tone, int itemType = ItemID.None, int npcType = NPCID.None)
	{
		return new JournalContentCard(heading, body, tone, itemType, npcType);
	}

	private static UITextPanel<string> CreateButton(string text, float width, float height)
	{
		UITextPanel<string> button = new BookActionButton(text, 0.78f)
		{
			BackgroundColor = DingDongUiTheme.Raised,
			BorderColor = DingDongUiTheme.Border,
			TextColor = DingDongUiTheme.Text
		};
		button.Width.Set(width, 0f);
		button.Height.Set(height, 0f);
		button.OnMouseOver += (_, element) => ((UITextPanel<string>)element).BorderColor = DingDongUiTheme.Gold;
		button.OnMouseOut += (_, element) => ((UITextPanel<string>)element).BorderColor = DingDongUiTheme.Border;
		return button;
	}
}
