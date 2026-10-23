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
internal static class DamageUiAudit
{
	public static DamageUiAuditResult Run()
	{
		int items = 0;
		int hostileNpcs = 0;
		int bannerBackedNpcs = 0;
		int missingLabels = 0;
		int outOfRangeProfiles = 0;
		int statefulBurstProfiles = 0;
		foreach (Item item in ContentSamples.ItemsByType.Values)
		{
			if (item.damage <= 0 && item.shoot <= ProjectileID.None)
				continue;
			items++;
			DamageSourceProfile source = DamageRuntimeClassifier.Classify(item).Profile;
			IReadOnlyList<string> deliveries = DamageRuntimeClassifier.DescribeItemDeliveries(item);
			IReadOnlyList<string> behaviors = DamageRuntimeClassifier.DescribeItemBehaviors(item);
			if (deliveries.Count == 0 || behaviors.Count == 0 || source.Element.Tags.Count == 0 || deliveries.Any(tag => string.IsNullOrWhiteSpace(DamageUiText.Tag(tag))) || behaviors.Any(tag => string.IsNullOrWhiteSpace(DamageUiText.Tag(tag))) || source.Element.Tags.Any(tag => string.IsNullOrWhiteSpace(DamageUiText.Tag(tag.Id))))
			{
				missingLabels++;
			}
		}

		HashSet<DamageTargetProfile> profiles = new();
		foreach (NPC npc in ContentSamples.NpcsByNetId.Values)
		{
			if (npc.friendly || npc.townNPC || npc.lifeMax <= 5)
				continue;
			hostileNpcs++;
			DamageTargetProfile profile = DamageRuntimeClassifier.Classify(npc).Profile;
			profiles.Add(profile);
			if (DamageKnowledgeSystem.BannerItemFor(npc) > ItemID.None)
				bannerBackedNpcs++;
		}

		foreach (DamageTargetProfile profile in profiles)
		{
			if (profile.MinimumMultiplier < 0.80d || profile.MaximumMultiplier > 1.30d)
				outOfRangeProfiles++;
			if (profile.RapidHitRule != null)
				statefulBurstProfiles++;
		}

		return new DamageUiAuditResult(items, hostileNpcs, bannerBackedNpcs, missingLabels, outOfRangeProfiles, statefulBurstProfiles);
	}
}
