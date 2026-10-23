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
public sealed class DamageBestiaryGlobalNPC : GlobalNPC
{
	public override void SetBestiary(NPC npc, BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		if (npc.friendly || npc.townNPC || npc.lifeMax <= 5)
			return;
		DamageTargetProfile profile = DamageRuntimeClassifier.Classify(npc).Profile;
		bestiaryEntry.Info.Add(new DamageBestiaryInfoElement(profile, DamageKnowledgeSystem.BannerItemFor(npc)));
	}
}
