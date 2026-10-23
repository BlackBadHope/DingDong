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
	private static DamageGuidePage LastHit()
	{
		DamageLastHitSnapshot hit = DamageGuideTelemetry.LastHit;
		if (hit == null)
		{
			return Page(DamageGuidePageId.LastHit, Block("Pages.LastHit.EmptyTitle", Text("Pages.LastHit.EmptyBody"), DamageGuideTone.Warning), Block("Pages.LastHit.FeedbackTitle", Text("Pages.LastHit.FeedbackBody")));
		}

		DamageEvaluationTrace trace = hit.Trace;
		(string dominantTag, _) = DamageUiText.DominantInteraction(hit.Source, hit.Target);
		string outcome = trace.FinalMultiplier >= 1.075d ? Text("Common.Weakness") : trace.FinalMultiplier <= 0.925d ? Text("Common.Resistance") : Text("Common.Neutral");
		string age = Math.Max(0L, (long)Main.GameUpdateCount - hit.GameTick).ToString(CultureInfo.CurrentCulture);
		return Page(DamageGuidePageId.LastHit, new DamageGuideBlock(hit.TargetName, Text("Pages.LastHit.ResultBody", outcome, F(trace.FinalMultiplier), DamageUiText.Tag(dominantTag)), Tone(trace.FinalMultiplier)), Block("Pages.LastHit.BreakdownTitle", Text("Pages.LastHit.BreakdownBody", F(trace.DeliveryMultiplier), F(trace.ElementMultiplier), F(trace.BehaviorMultiplier), F(trace.AcquisitionMultiplier), F(trace.SegmentMultiplier), F(trace.PhaseMultiplier))), Block("Pages.LastHit.ClampTitle", Text("Pages.LastHit.ClampBody", F(trace.RawMultiplier), F(trace.FinalMultiplier), trace.WasClamped ? Text("Common.Yes") : Text("Common.No"))), Block("Pages.LastHit.AgeTitle", Text("Pages.LastHit.AgeBody", age)));
	}
}
