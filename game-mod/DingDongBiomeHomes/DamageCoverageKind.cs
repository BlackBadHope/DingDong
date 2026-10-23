using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public enum DamageCoverageKind
{
	Explicit,
	Inherited,
	Adapter,
	Fallback,
	Unknown
}
