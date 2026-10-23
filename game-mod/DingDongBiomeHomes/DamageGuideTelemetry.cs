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
internal static class DamageGuideTelemetry
{
	internal static DamageLastHitSnapshot LastHit { get; private set; }

	internal static void Record(NPC target, DamageSourceProfile source, DamageTargetProfile profile, DamageEvaluationTrace trace)
	{
		if (Main.netMode == NetmodeID.Server)
			return;
		LastHit = new DamageLastHitSnapshot(target.type, target.FullName, source, profile, trace, (long)Main.GameUpdateCount);
	}

	internal static void Clear() => LastHit = null;
}
