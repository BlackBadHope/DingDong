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
public enum DamageGuidePageId
{
	Overview,
	CurrentWeapon,
	LastHit,
	EnemyKnowledge,
	Delivery,
	Elements,
	Patterns,
	Difficulty
}
