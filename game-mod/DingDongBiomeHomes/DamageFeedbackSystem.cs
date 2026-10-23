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
public sealed class DamageFeedbackSystem : ModSystem
{
	private const long FeedbackCooldownTicks = 45;
	private static readonly Dictionary<int, long> LastFeedbackByTarget = new();
	public override void OnWorldLoad()
	{
		LastFeedbackByTarget.Clear();
		DamageGuideTelemetry.Clear();
	}

	public override void OnWorldUnload()
	{
		LastFeedbackByTarget.Clear();
		DamageGuideTelemetry.Clear();
	}

	public override void Unload()
	{
		LastFeedbackByTarget.Clear();
		DamageGuideTelemetry.Clear();
	}

	internal static void Report(NPC target, DamageSourceProfile source, DamageTargetProfile profile, DamageEvaluationTrace trace)
	{
		if (Main.netMode == NetmodeID.Server)
			return;
		DamageGuideTelemetry.Record(target, source, profile, trace);
		if (trace.FinalMultiplier is> 0.925d and < 1.075d)
			return;
		long tick = (long)Main.GameUpdateCount;
		if (LastFeedbackByTarget.TryGetValue(target.whoAmI, out long previous) && tick - previous < FeedbackCooldownTicks)
		{
			return;
		}

		LastFeedbackByTarget[target.whoAmI] = tick;
		(string tag, _) = DamageUiText.DominantInteraction(source, profile);
		bool weakness = trace.FinalMultiplier >= 1.075d;
		string key = weakness ? "WeaknessHit" : "ResistanceHit";
		string text = Language.GetTextValue($"Mods.DingDongBiomeHomes.Damage.{key}", DamageUiText.Tag(tag), trace.FinalMultiplier.ToString("0.00", CultureInfo.CurrentCulture));
		CombatText.NewText(target.Hitbox, weakness ? new Color(255, 196, 92) : new Color(135, 190, 255), text, dramatic: false, dot: false);
	}
}
