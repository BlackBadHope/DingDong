using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Dedicated-only exhaustive compatibility gate. Every loaded damage-capable
/// item, projectile and debuff source is evaluated against every loaded
/// hostile NPC target through the production rule engine. The audit never
/// mutates world, player, item or NPC persistence state.
/// </summary>
internal static partial class DamageExhaustiveAudit
{
	private const ulong DigestSeed = 14695981039346656037UL;
	private const ulong DigestPrime = 1099511628211UL;
	private const double Epsilon = 0.000001d;
}
