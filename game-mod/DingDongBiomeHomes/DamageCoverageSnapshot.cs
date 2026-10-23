using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public readonly record struct DamageCoverageSnapshot(int Items, int Projectiles, int HostileNpcs, int DamageOverTimeEffects, int TrapProjectiles, int EnvironmentAdapters, int Explicit, int Inherited, int Adapters, int Fallbacks, int Unknown)
{
	public string ToMarker() => string.Join('|', new[] { "DINGDONG_DAMAGE_COVERAGE", $"items={Items}", $"projectiles={Projectiles}", $"hostileNpcs={HostileNpcs}", $"dot={DamageOverTimeEffects}", $"traps={TrapProjectiles}", $"environment={EnvironmentAdapters}", $"explicit={Explicit}", $"inherited={Inherited}", $"adapters={Adapters}", $"fallback={Fallbacks}", $"unknown={Unknown}", $"verdict={(Unknown == 0 ? "PASS" : "FAIL")}" });
}
