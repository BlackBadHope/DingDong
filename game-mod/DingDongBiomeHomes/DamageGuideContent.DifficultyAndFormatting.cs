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
	private static DamageGuidePage Difficulty()
	{
		DamageDifficultySnapshot snapshot = DamageDifficultyContextSystem.Capture();
		List<DamageGuideBlock> blocks = new()
		{
			Block("Pages.Difficulty.WorldTitle", Text("Pages.Difficulty.WorldBody", snapshot.VanillaMode, snapshot.SeedContext), DamageGuideTone.Information),
			Block("Pages.Difficulty.DingDongTitle", Text("Pages.Difficulty.DingDongBody", snapshot.DingDongEnabled ? Text("Common.Enabled") : Text("Common.Disabled")), snapshot.DingDongEnabled ? DamageGuideTone.Positive : DamageGuideTone.Warning)
		};
		if (snapshot.ExternalModes.Count == 0)
		{
			blocks.Add(Block("Pages.Difficulty.ModsTitle", Text("Pages.Difficulty.NoModsBody")));
		}
		else
		{
			foreach (DamageDifficultyEntry entry in snapshot.ExternalModes)
			{
				blocks.Add(new DamageGuideBlock(entry.Source, Text("Pages.Difficulty.ModBody", entry.Mode, entry.Detail), entry.AdapterHealthy ? DamageGuideTone.Normal : DamageGuideTone.Warning));
			}
		}

		blocks.Add(Block("Pages.Difficulty.StackTitle", Text("Pages.Difficulty.StackBody"), DamageGuideTone.Positive));
		blocks.Add(Block("Pages.Difficulty.LegendaryTitle", Text("Pages.Difficulty.LegendaryBody")));
		return Page(DamageGuidePageId.Difficulty, blocks.ToArray());
	}

	private static DamageGuidePage Page(DamageGuidePageId id, params DamageGuideBlock[] blocks) => new(id, Text($"Pages.{id}.Title"), Text($"Pages.{id}.Subtitle"), Array.AsReadOnly(blocks));
	private static DamageGuideBlock Block(string headingKey, string body, DamageGuideTone tone = DamageGuideTone.Normal) => new(Text(headingKey), body, tone);
	private static string ActiveRuleAxes()
	{
		bool delivery = false;
		bool element = false;
		bool behavior = false;
		bool acquisition = false;
		bool segment = false;
		bool phase = false;
		HashSet<DamageTargetProfile> profiles = new();
		foreach (NPC npc in ContentSamples.NpcsByNetId.Values)
		{
			if (!npc.friendly && !npc.townNPC && npc.lifeMax > 5)
				profiles.Add(DamageRuntimeClassifier.Classify(npc).Profile);
		}

		foreach (DamageTargetProfile profile in profiles)
		{
			delivery |= profile.DeliveryMultipliers.Any(pair => !NearlyOne(pair.Value));
			element |= profile.ElementMultipliers.Any(pair => !NearlyOne(pair.Value));
			behavior |= profile.BehaviorMultipliers.Any(pair => !NearlyOne(pair.Value));
			acquisition |= profile.AcquisitionMultipliers.Any(pair => !NearlyOne(pair.Value));
			segment |= profile.SegmentMultipliers.Any(pair => !NearlyOne(pair.Value));
			phase |= profile.PhaseMultipliers.Any(pair => !NearlyOne(pair.Value));
		}

		List<string> axes = new();
		if (delivery)
			axes.Add(Text("Axes.Delivery"));
		if (element)
			axes.Add(Text("Axes.Element"));
		if (behavior)
			axes.Add(Text("Axes.Behavior"));
		if (acquisition)
			axes.Add(Text("Axes.Acquisition"));
		if (segment)
			axes.Add(Text("Axes.Segment"));
		if (phase)
			axes.Add(Text("Axes.Phase"));
		return axes.Count == 0 ? Text("Common.None") : string.Join(", ", axes);
	}

	private static string Coverage(DamageCoverageKind coverage) => Text($"Coverage.{coverage}");
	private static string Tags(params string[] tags) => string.Join(", ", tags.Select(DamageUiText.Tag));
	private static string F(double value) => value.ToString("0.00", CultureInfo.CurrentCulture);
}
