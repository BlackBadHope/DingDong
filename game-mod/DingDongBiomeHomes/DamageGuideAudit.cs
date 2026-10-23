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
internal static class DamageGuideAudit
{
	internal static DamageGuideAuditResult Run()
	{
		int empty = 0;
		foreach (DamageGuidePageId id in DamageGuideContent.PageOrder)
		{
			DamageGuidePage page = DamageGuideContent.Build(id);
			if (string.IsNullOrWhiteSpace(page.Title) || string.IsNullOrWhiteSpace(page.Subtitle) || page.Blocks.Count == 0 || page.Blocks.Any(block => string.IsNullOrWhiteSpace(block.Heading) || string.IsNullOrWhiteSpace(block.Body)))
			{
				empty++;
			}
		}

		DamageDifficultySnapshot difficulty = DamageDifficultyContextSystem.Capture();
		return new DamageGuideAuditResult(DamageGuideContent.PageOrder.Length, empty, difficulty.AdapterFailures);
	}
}
