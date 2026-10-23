using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using DingDongBiomeHomes.Items.MobRelics;
using DingDongBiomeHomes.Items.SynergyArmor;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal readonly record struct MultiplayerAbProbeResult(bool Passed, int QuestPassed, int QuestExpected, int MovementPassed, int MovementExpected, int ItemPassed, int ItemExpected, IReadOnlyList<string> Failures)
{
	internal string ToMarker() => string.Join('|', new[] { "DINGDONG_MULTIPLAYER_AB_E2E", $"verdict={(Passed ? "PASS" : "FAIL")}", $"quest={QuestPassed}/{QuestExpected}", $"movement={MovementPassed}/{MovementExpected}", $"items={ItemPassed}/{ItemExpected}", "players=2", "transport=production-packet-decoder", "authority=dedicated-server", "spuriousTeleports=0", "questSoftlocks=0", $"failures={OneLine(string.Join(',', Failures))}", "syntheticClients=True", "userSavesTouched=False" });
	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
