using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

/// <summary>
/// Dedicated, environment-gated developer exporter used to freeze the exact
/// loaded NPC identities into the generated per-mob content catalog.
/// Normal games never write this evidence file.
/// </summary>
public sealed class MobRelicCatalogExportSystem : ModSystem
{
	private const string ExportPathEnvironment = "DINGDONG_MOB_RELIC_EXPORT_PATH";

	public override void PostAddRecipes()
	{
		if (!Main.dedServ)
			return;
		string path = Environment.GetEnvironmentVariable(ExportPathEnvironment);
		if (string.IsNullOrWhiteSpace(path))
			return;
		try
		{
			var mobs = ContentSamples.NpcsByNetId.Values
				.Where(MobRelicGlobalNPC.IsEligible)
				.GroupBy(QuestContentRegistry.ContentKey, StringComparer.Ordinal)
				.Select(group => group.First())
				.OrderBy(QuestContentRegistry.ContentKey, StringComparer.Ordinal)
				.ToArray();
			string[] lines = mobs.Select(npc => string.Join('\t', new[]
			{
				Encode(QuestContentRegistry.ContentKey(npc)),
				Encode(npc.FullName),
				Encode(QuestContentRegistry.ContentMod(npc)),
				npc.boss ? "1" : "0"
			})).ToArray();
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
			File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			string sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
			int providers = mobs.Select(QuestContentRegistry.ContentMod).Distinct(StringComparer.Ordinal).Count();
			Mod.Logger.Info($"DINGDONG_MOB_CATALOG_EXPORT|verdict=PASS|mobs={mobs.Length}|providers={providers}|sha256={sha256}|userSavesTouched=False");
		}
		catch (Exception exception)
		{
			Mod.Logger.Error($"DINGDONG_MOB_CATALOG_EXPORT|verdict=FAIL|exception={exception.GetType().Name}|message={OneLine(exception.Message)}|userSavesTouched=False", exception);
		}
	}

	private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));

	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value)
		? "none"
		: value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
