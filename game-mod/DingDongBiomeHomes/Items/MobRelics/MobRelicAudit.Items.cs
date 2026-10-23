using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static partial class MobRelicAudit
{
	private static void AuditRelicItem(MobRelicAuditContext context, MobRelicVisualPlayer visualProbe, GeneratedMobRelicItem relic)
	{
		context.UniqueTypes.Add(relic.Type);
		context.UniqueNames.Add(relic.Name);
		context.UniqueTexturePaths.Add(relic.Texture);
		context.UniqueVisualSignatures.Add(relic.VisualSignature);
		context.UniqueMechanics.Add(relic.Profile.BehaviorSignature);
		context.UniqueTriggers.Add(relic.Profile.Trigger);
		context.UniqueInvocations.Add(relic.Profile.Invocation);
		context.UniqueTwists.Add(relic.Profile.Twist);

		Item original = new();
		original.SetDefaults(relic.Type);
		if (original.ModItem is GeneratedMobRelicItem runtime && runtime.Type == relic.Type && runtime.Tier == relic.Tier && runtime.Source.ContentKey == relic.Source.ContentKey && runtime.Profile.MechanicsKey == relic.Profile.MechanicsKey && runtime.Name == relic.Name && runtime.Texture == relic.Texture && runtime.VisualSignature == relic.VisualSignature)
			context.ItemsPassed++;
		else
			Failure(context, $"item:{relic.Name}");

		if (ContentSamples.ItemsByType.TryGetValue(relic.Type, out Item sample) && SameIdentity(original, sample))
			context.ContentSamplesPassed++;
		else
			Failure(context, $"content-sample:{relic.Name}");
		AuditCloneAndStack(context, relic, original);
		AuditVisualAndLocalization(context, visualProbe, relic, original);
		AuditPersistence(context, relic, original);
	}

	private static void AuditFunctionality(MobRelicAuditContext context, GeneratedMobRelicItem relic, Item item)
	{
		bool valid = item.damage <= 0 && item.shoot == ProjectileID.None && item.useStyle == ItemUseStyleID.None && item.accessory && !item.material && !item.consumable && item.maxStack == 1 && relic.Profile.IsFunctional && HasPlayerFacingTooltip(relic);
		if (valid)
			context.FunctionalItemsPassed++;
		else
			Failure(context, $"functional:{relic.Name}");
	}
}
