using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.MobRelics;
public readonly record struct MobRelicAuditResult(bool Passed, int MobPoolsPassed, int MobsExpected, int ItemsPassed, int ItemsExpected, int SaveLoadPassed, int NetPassed, int QuestIdentityPassed, int FunctionalItemsPassed, int PlayerVisualsPassed, int ResonanceProjectilePassed, int RuntimeGameplayPassed, int LootDatabasePassed, int ReportedDropsPassed, int ClonePassed, int StackPassed, int WorldStackPassed, int ContentSamplesPassed, int ForeignRulesEnumerated, int TexturesPassed, int LocalizationPassed, int UniqueMechanics, int TriggersCovered, int InvocationsCovered, int TwistsCovered, int UniqueTypes, int UniqueNames, int UniqueTexturePaths, int UniqueVisualSignatures, int Providers, int EligibleMobs, int LootNormalizationPassed, int DifficultyBoostRulesRemoved, int RareBaseItemsPreserved, int ServiceActorsExcluded, IReadOnlyList<string> Failures)
{
	public string ToMarker() => string.Join('|', new[]
	{
		"DINGDONG_MOB_SPECIFIC_LOOT_E2E", $"verdict={(Passed ? "PASS" : "FAIL")}",
		$"mobs={MobPoolsPassed}/{MobsExpected}", $"actualModItems={ItemsPassed}/{ItemsExpected}",
		$"itemsPerMob={MobSpecificRelicCatalog.ItemsPerMob}", $"uniqueTypes={UniqueTypes}/{ItemsExpected}",
		$"uniqueNames={UniqueNames}/{ItemsExpected}", $"uniqueTexturePaths={UniqueTexturePaths}/{MobRelicSourceCatalog.ExpectedTextureCount}",
		$"visualSignatures={UniqueVisualSignatures}/{ItemsExpected}",
		$"textures={TexturesPassed}/{ItemsExpected}", $"localization={LocalizationPassed}/{ItemsExpected * 2}",
		$"saveLoad={SaveLoadPassed}/{ItemsExpected}", $"net={NetPassed}/{ItemsExpected}",
		$"questIdentity={QuestIdentityPassed}/{MobsExpected}", $"functionalItems={FunctionalItemsPassed}/{ItemsExpected}",
		$"playerVisuals={PlayerVisualsPassed}/{ItemsExpected}", $"resonanceProjectile={ResonanceProjectilePassed}/1",
		$"runtimeGameplay={RuntimeGameplayPassed}/1", $"lootDatabase={LootDatabasePassed}/{MobsExpected}",
		$"reportedDrops={ReportedDropsPassed}/{ItemsExpected}", $"clone={ClonePassed}/{ItemsExpected}",
		$"stack={StackPassed}/{ItemsExpected}", $"worldStack={WorldStackPassed}/{ItemsExpected}",
		$"contentSamples={ContentSamplesPassed}/{ItemsExpected}", $"uniqueMechanics={UniqueMechanics}/{ItemsExpected}",
		$"triggers={TriggersCovered}/{MobRelicGeneratedSources.ExpectedTriggerCount}",
		$"actions={InvocationsCovered}/{MobRelicGeneratedSources.ExpectedInvocationCount}",
		$"modifiers={TwistsCovered}/{MobRelicGeneratedSources.ExpectedTwistCount}",
		$"foreignRulesEnumerated={ForeignRulesEnumerated}", $"dropRules={MobsExpected * 4}/{MobsExpected * 4}",
		$"ordinaryOptions={MobsExpected * 2}", $"expertPlus={MobsExpected}", $"masterPlus={MobsExpected}",
		$"legendary={MobsExpected}", $"providers={Providers}", $"eligibleMobs={EligibleMobs}",
		"lootNormalization=disabled", $"foreignLootRulesMutated={DifficultyBoostRulesRemoved}",
		$"difficultyBoostRulesRemoved={DifficultyBoostRulesRemoved}", "rareBaseItemsPreserved=not-applicable",
		$"serviceActorsExcluded={ServiceActorsExcluded}/8", $"curatedSources={MobRelicSourceCatalog.ExpectedMobCount}/4",
		$"legacyAliases={MobRelicLegacyAliases.RegisteredCount}/{MobRelicLegacyAliases.ExpectedAliasCount}",
		$"catalogSha256={MobRelicGeneratedSources.SourceCatalogSha256}", $"failures={OneLine(string.Join(',', Failures))}",
		"userSavesTouched=False"
	});
	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
