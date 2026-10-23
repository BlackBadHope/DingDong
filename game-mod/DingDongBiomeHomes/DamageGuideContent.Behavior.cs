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
	private static DamageGuidePage Overview()
	{
		string activeAxes = ActiveRuleAxes();
		string status = DamageAuthoritySystem.IsEnabled ? Text("Common.Enabled") : Text("Common.Disabled");
		return Page(DamageGuidePageId.Overview, Block("Pages.Overview.StatusTitle", Text("Pages.Overview.StatusBody", status), DamageAuthoritySystem.IsEnabled ? DamageGuideTone.Positive : DamageGuideTone.Warning), Block("Pages.Overview.FormulaTitle", Text("Pages.Overview.FormulaBody"), DamageGuideTone.Information), Block("Pages.Overview.RulesTitle", Text("Pages.Overview.RulesBody", activeAxes), DamageGuideTone.Positive));
	}

	private static DamageGuidePage CurrentWeapon(Player player)
	{
		Item item = player?.HeldItem;
		if (item == null || item.IsAir || false)
		{
			return Page(DamageGuidePageId.CurrentWeapon, Block("Pages.CurrentWeapon.EmptyTitle", Text("Pages.CurrentWeapon.EmptyBody"), DamageGuideTone.Warning), Block("Pages.CurrentWeapon.ReadTitle", Text("Pages.CurrentWeapon.ReadBody")));
		}

		DamageSourceClassification classification = DamageRuntimeClassifier.Classify(item);
		DamageSourceProfile source = classification.Profile;
		string delivery = DamageUiText.Tags(DamageRuntimeClassifier.DescribeItemDeliveries(item));
		string element = DamageUiText.Axis(source.Element);
		string behavior = DamageUiText.Tags(DamageRuntimeClassifier.DescribeItemBehaviors(item));
		string acquisition = DamageUiText.Axis(source.Acquisition);
		var blocks = new List<DamageGuideBlock> { new DamageGuideBlock(item.Name, Text("Pages.CurrentWeapon.IdentityBody", delivery, element), DamageGuideTone.Information), Block("Pages.CurrentWeapon.PatternTitle", Text("Pages.CurrentWeapon.PatternBody", behavior)), Block("Pages.CurrentWeapon.OriginTitle", Text("Pages.CurrentWeapon.OriginBody", acquisition)), Block("Pages.CurrentWeapon.CoverageTitle", Text("Pages.CurrentWeapon.CoverageBody", Coverage(classification.Coverage))), Block("Pages.CurrentWeapon.ExpectTitle", Text("Pages.CurrentWeapon.ExpectBody"), DamageGuideTone.Positive) };
        blocks.AddRange(Compatibility.ProgressionTooltip.Details(item));
        return Page(DamageGuidePageId.CurrentWeapon, blocks.ToArray());
	}

	private static DamageGuidePage EnemyKnowledge() => Page(DamageGuidePageId.EnemyKnowledge, Block("Pages.EnemyKnowledge.UnlockTitle", Text("Pages.EnemyKnowledge.UnlockBody"), DamageGuideTone.Information), Block("Pages.EnemyKnowledge.ReadTitle", Text("Pages.EnemyKnowledge.ReadBody")), Block("Pages.EnemyKnowledge.ThresholdTitle", Text("Pages.EnemyKnowledge.ThresholdBody")), Block("Pages.EnemyKnowledge.CharacterTitle", Text("Pages.EnemyKnowledge.CharacterBody")));
}
