using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Data-driven runtime classifier. Optional-mod content is inspected only via
/// public Terraria/tModLoader metadata, so no compatibility assembly is a hard
/// dependency.
/// </summary>
public static partial class DamageRuntimeClassifier
{
	public static DamageCoverageSnapshot AuditLoadedContent()
	{
		CoverageCounter counter = new();
		int items = 0;
		foreach (Item item in ContentSamples.ItemsByType.Values)
		{
			if (item.damage <= 0 && item.shoot <= ProjectileID.None)
				continue;
			items++;
			counter.Add(Classify(item).Coverage);
		}

		int projectiles = 0;
		int trapProjectiles = 0;
		foreach (Projectile projectile in ContentSamples.ProjectilesByType.Values)
		{
			projectiles++;
			if (projectile.trap)
				trapProjectiles++;
			counter.Add(Classify(projectile).Coverage);
		}

		int hostileNpcs = 0;
		foreach (NPC npc in ContentSamples.NpcsByNetId.Values)
		{
			if (npc.friendly || npc.townNPC || npc.lifeMax <= 5)
				continue;
			hostileNpcs++;
			counter.Add(Classify(npc).Coverage);
		}

		return new DamageCoverageSnapshot(items, projectiles, hostileNpcs, Main.debuff.Count(value => value), trapProjectiles, 1, counter.Explicit, counter.Inherited, counter.Adapters, counter.Fallbacks, counter.Unknown);
	}
}
