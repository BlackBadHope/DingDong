using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
internal static partial class AiArsenalAudit
{
	private static Item RunPhase(Mod mod, List<string> failures, ref int itemsPassed, ref int tiersPassed, ref int texturesPassed, ref int localizationPassed, ref int caseIndex, AiArsenalItem modItem)
	{
		caseIndex++;
		Item item = new();
		item.SetDefaults(modItem.Type);
		bool baseValid = item.damage > 0 && item.useTime > 0 && item.useAnimation > 0 && item.rare >= ItemRarityID.Blue && item.value > 0;
		if (baseValid)
			itemsPassed++;
		else
			failures.Add($"defaults:{modItem.Name}");
		if (Enum.IsDefined(modItem.Tier))
			tiersPassed++;
		else
			failures.Add($"tier:{modItem.Name}");
		string texturePath = modItem.Texture.StartsWith(mod.Name + "/", StringComparison.Ordinal) ? modItem.Texture[(mod.Name.Length + 1)..] + ".rawimg" : modItem.Texture + ".rawimg";
		if (mod.FileExists(texturePath))
			texturesPassed++;
		else
			failures.Add($"texture:{modItem.Name}");
		if (Localized(modItem.DisplayName.Value))
			localizationPassed++;
		else
			failures.Add($"display:{modItem.Name}");
		if (Localized(modItem.Tooltip.Value))
			localizationPassed++;
		else
			failures.Add($"tooltip:{modItem.Name}");
		return item;
	}

	private static bool Localized(string value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("Mods.DingDongBiomeHomes.Items.", StringComparison.Ordinal);
}
