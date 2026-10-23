using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;
/// <summary>
/// The plain-text view model shared by the interactive guide and dedicated
/// audits. All explanations are derived from the same profiles used by combat.
/// </summary>
internal static partial class DamageGuideContent
{
	internal static DamageGuidePage Build(DamageGuidePageId id, Player player = null)
	{
		return id switch
		{
			DamageGuidePageId.Overview => Overview(),
			DamageGuidePageId.CurrentWeapon => CurrentWeapon(player),
			DamageGuidePageId.LastHit => LastHit(),
			DamageGuidePageId.EnemyKnowledge => EnemyKnowledge(),
			DamageGuidePageId.Delivery => Delivery(),
			DamageGuidePageId.Elements => Elements(),
			DamageGuidePageId.Patterns => Patterns(),
			DamageGuidePageId.Difficulty => Difficulty(),
			_ => Overview()};
	}
}
