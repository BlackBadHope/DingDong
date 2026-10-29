using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace DingDongBiomeHomes;

// Height follows the rendered font at the real available width, including translated text.
internal sealed class JournalContentCard : JournalFrame
{
	private readonly BookText heading;
    internal string HeadingText=>heading.Value;
	private readonly BookText body;
	private readonly float minimumContentHeight;

	internal JournalContentCard(string title, string description, DamageGuideTone tone, int itemType = ItemID.None, int npcType = NPCID.None, int rewardItemType = ItemID.None)
		: base(tone)
	{
		Width.Set(0f, 1f);
		Height.Set(100f, 0f);
		SetPadding(18f);
		bool hasIcon = itemType > ItemID.None || npcType > NPCID.None;
		bool hasReward = rewardItemType > ItemID.None;
		float leading = hasIcon ? 62f : 0f;
		float trailing = hasReward ? 46f : 0f;
		minimumContentHeight = hasIcon ? 50f : 0f;
		if (hasIcon)
		{
			JournalFrame slot = new();
			slot.Width.Set(48f, 0f);
			slot.Height.Set(48f, 0f);
			slot.SetPadding(3f);
			QuestTargetIcon icon = new(itemType, npcType);
			icon.Width.Set(0f, 1f);
			icon.Height.Set(0f, 1f);
			slot.Append(icon);
			Append(slot);
		}
		heading = new BookText(title,.96f,tone==DamageGuideTone.Normal?DingDongUiTheme.Text:DingDongUiTheme.Accent(tone));
		heading.Left.Set(leading, 0f);
		heading.Width.Set(-leading - trailing, 1f);
		Append(heading);
		body = new BookText(description,.85f,DingDongUiTheme.Text);
		body.Left.Set(leading, 0f);
		body.Width.Set(-leading - trailing, 1f);
		Append(body);
		if (hasReward)
		{
			JournalFrame rewardSlot = new(DamageGuideTone.Positive);
			rewardSlot.Left.Set(-34f, 1f);
			rewardSlot.Width.Set(34f, 0f);
			rewardSlot.Height.Set(34f, 0f);
			rewardSlot.SetPadding(2f);
			QuestTargetIcon reward = new(rewardItemType, NPCID.None);
			reward.Width.Set(0f, 1f);
			reward.Height.Set(0f, 1f);
			rewardSlot.Append(reward);
			Append(rewardSlot);
		}
	}

	public override void Recalculate()
	{
		// UIText wraps using its previous inner width. The second pass uses the newly laid-out width.
		base.Recalculate();
		heading.Recalculate();
		body.Top.Set((float)Math.Ceiling(heading.GetOuterDimensions().Height) + 8f, 0f);
		body.Recalculate();
		float contentHeight = body.Top.Pixels + body.GetOuterDimensions().Height;
		Height.Set((float)Math.Ceiling(Math.Max(minimumContentHeight, contentHeight)) + PaddingTop + PaddingBottom, 0f);
		base.Recalculate();
	}
}

// The heading scrolls with its section. Long localized descriptions cannot consume the viewport.
internal sealed class JournalSectionHeader : Terraria.UI.UIElement
{
	internal UIText Title { get; } = new(string.Empty, 1.02f)
	{
		IsWrapped = true, TextOriginX = 0f, TextColor = DingDongUiTheme.Text,
		IgnoresMouseInteraction = true, WrappedTextBottomPadding = 0f
	};
	internal UIText Subtitle { get; } = new(string.Empty, 0.8f)
	{
		IsWrapped = true, TextOriginX = 0f, TextColor = DingDongUiTheme.Muted,
		IgnoresMouseInteraction = true, WrappedTextBottomPadding = 0f
	};

	internal JournalSectionHeader()
	{
		Width.Set(0f, 1f);
		Height.Set(64f, 0f);
		Title.Width.Set(0f, 1f);
		Subtitle.Width.Set(0f, 1f);
		Append(Title);
		Append(Subtitle);
		IgnoresMouseInteraction = true;
	}

	public override void Recalculate()
	{
		base.Recalculate();
		Title.Recalculate();
		Subtitle.Top.Set((float)Math.Ceiling(Title.GetOuterDimensions().Height) + 6f, 0f);
		Subtitle.Recalculate();
		Height.Set((float)Math.Ceiling(Subtitle.Top.Pixels + Subtitle.GetOuterDimensions().Height) + 8f, 0f);
		base.Recalculate();
	}
}
