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
public sealed class DamageTooltipGlobalItem : GlobalItem
{
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (item.damage <= 0 && item.shoot <= ProjectileID.None)
			return;
		DamageSourceProfile profile = DamageRuntimeClassifier.Classify(item).Profile;
		string affinity = Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.WeaponAffinity", DamageUiText.Tags(DamageRuntimeClassifier.DescribeItemDeliveries(item)), DamageUiText.Axis(profile.Element));
		string behavior = Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.WeaponBehavior", DamageUiText.Tags(DamageRuntimeClassifier.DescribeItemBehaviors(item)));
		string guide = DamageAuthoritySystem.IsEnabled ? Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.WeaponGuide", DamageUiText.GuideKey()) : Language.GetTextValue("Mods.DingDongBiomeHomes.Damage.WeaponDisabled", DamageUiText.GuideKey());
		
		
		guide = Compatibility.ProgressionTooltip.Russian ? $"[{DamageUiText.GuideKey()}] DingDong: свойства и прокачка" : $"[{DamageUiText.GuideKey()}] DingDong: stats and progression";
		tooltips.Add(new TooltipLine(Mod, "DingDongDamageGuide", guide) { OverrideColor = DamageAuthoritySystem.IsEnabled ? new Color(155, 220, 176) : new Color(205, 151, 105) });
	}
}
