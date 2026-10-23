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
	private static DamageGuidePage Delivery() => Page(DamageGuidePageId.Delivery, Block("Pages.Delivery.MeleeTitle", Text("Pages.Delivery.MeleeBody", DamageUiText.Tag(DamageTags.KineticContact), DamageUiText.Tag(DamageTags.Thrown), DamageUiText.Tag(DamageTags.Rogue))), Block("Pages.Delivery.RangedTitle", Text("Pages.Delivery.RangedBody", DamageUiText.Tag(DamageTags.Arrow), DamageUiText.Tag(DamageTags.Bullet), DamageUiText.Tag(DamageTags.MagicProjectile), DamageUiText.Tag(DamageTags.Beam), DamageUiText.Tag(DamageTags.Explosion))), Block("Pages.Delivery.SummonTitle", Text("Pages.Delivery.SummonBody", DamageUiText.Tag(DamageTags.MinionContact), DamageUiText.Tag(DamageTags.MinionProjectile), DamageUiText.Tag(DamageTags.Sentry))), Block("Pages.Delivery.WorldTitle", Text("Pages.Delivery.WorldBody", DamageUiText.Tag(DamageTags.Trap), DamageUiText.Tag(DamageTags.DamageOverTime), DamageUiText.Tag(DamageTags.Environment))), Block("Pages.Delivery.RuleTitle", Text("Pages.Delivery.RuleBody"), DamageGuideTone.Information));
	private static DamageGuidePage Elements() => Page(DamageGuidePageId.Elements, Block("Pages.Elements.NaturalTitle", Text("Pages.Elements.NaturalBody", Tags(DamageTags.Neutral, DamageTags.Fire, DamageTags.Frost, DamageTags.Lightning, DamageTags.Water, DamageTags.Earth, DamageTags.Poison))), Block("Pages.Elements.MysticTitle", Text("Pages.Elements.MysticBody", Tags(DamageTags.Holy, DamageTags.Shadow, DamageTags.Astral, DamageTags.Void, DamageTags.Gel, DamageTags.Nature, DamageTags.ModElement))), Block("Pages.Elements.RuleTitle", Text("Pages.Elements.RuleBody"), DamageGuideTone.Information), Block("Pages.Elements.NoDebuffTitle", Text("Pages.Elements.NoDebuffBody"), DamageGuideTone.Warning));
	private static DamageGuidePage Patterns() => Page(DamageGuidePageId.Patterns, Block("Pages.Patterns.BasicTitle", Text("Pages.Patterns.BasicBody", Tags(DamageTags.Single, DamageTags.Burst, DamageTags.Sustained, DamageTags.RapidMultihit))), Block("Pages.Patterns.ShapeTitle", Text("Pages.Patterns.ShapeBody", Tags(DamageTags.Piercing, DamageTags.Area, DamageTags.Chain, DamageTags.Delayed))), Block("Pages.Patterns.CurrentTitle", Text("Pages.Patterns.CurrentBody"), DamageGuideTone.Positive), Block("Pages.Patterns.IFramesTitle", Text("Pages.Patterns.IFramesBody"), DamageGuideTone.Warning));
}
