using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace DingDongBiomeHomes;
internal sealed class DamageBestiaryInfoElement : IBestiaryInfoElement
{
	private readonly DamageTargetProfile profile;
	private readonly int bannerItemType;
	public DamageBestiaryInfoElement(DamageTargetProfile profile, int bannerItemType)
	{
		this.profile = profile;
		this.bannerItemType = bannerItemType;
	}

	public UIElement ProvideUIElement(BestiaryUICollectionInfo info)
	{
		if (info.UnlockState == BestiaryEntryUnlockState.NotKnownAtAll_0)
			return null;
		bool unlocked = bannerItemType <= ItemID.None || Main.LocalPlayer.GetModPlayer<DamageKnowledgePlayer>().HasLearned(bannerItemType);
		string text = unlocked ? BuildKnowledgeText() : BuildLockedText();
		Color color = unlocked ? new Color(180, 225, 240) : new Color(145, 145, 155);
		UIElement container = new();
		container.Width.Set(0f, 1f);
		container.Height.Set(unlocked ? 88f : 48f, 0f);
		UIText label = new(text, 0.82f)
		{
			IsWrapped = true,
			TextColor = color
		};
		label.Width.Set(-12f, 1f);
		label.Left.Set(6f, 0f);
		container.Append(label);
		return container;
	}

	private string BuildLockedText()
	{
		string bannerName = bannerItemType > ItemID.None ? Lang.GetItemNameValue(bannerItemType) : string.Empty;
		return Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.BestiaryLocked", bannerName, DamageUiText.GuideKey());
	}

	private string BuildKnowledgeText()
	{
		string weaknesses = FormatInteractions(DamageUiText.Interactions(profile, weaknesses: true));
		string resistances = FormatInteractions(DamageUiText.Interactions(profile, weaknesses: false));
		return Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.BestiaryKnowledge", DamageUiText.TargetArchetype(profile), weaknesses, resistances, DamageUiText.GuideKey());
	}

	private static string FormatInteractions(List<(string Tag, double Multiplier)> interactions)
	{
		if (interactions.Count == 0)
			return Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.None");
		return string.Join(", ", interactions.Select(entry => $"{DamageUiText.Tag(entry.Tag)} ×{entry.Multiplier.ToString("0.00", CultureInfo.CurrentCulture)}"));
	}
}
