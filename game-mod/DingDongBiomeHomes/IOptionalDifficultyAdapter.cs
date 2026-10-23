using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal interface IOptionalDifficultyAdapter
{
	DamageDifficultyEntry Read(bool worldReady);
}
