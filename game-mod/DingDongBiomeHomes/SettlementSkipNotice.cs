using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

// 30.10, owner's brother: an x10 world came out with 0 residences and nothing told the player why.
// Every path that skips the 61 residences records a reason here; the reason is saved with the
// world and shown in chat when a player enters a world without a residence registry.
public sealed class SettlementSkipNotice : ModSystem
{
	internal const string LocKey = "Mods.DingDongBiomeHomes.SettlementNotice";
	internal static string Reason { get; private set; }

	internal static void Record(string reason) => Reason = reason;

	// Not cleared in OnWorldUnload: worldgen's Reset pass unloads the world after ModifyWorldGenTasks
	// recorded a reason. OnWorldLoad runs before LoadWorldData, so a loaded world starts clean.
	public override void OnWorldLoad() => Reason = null;

	public override void SaveWorldData(TagCompound tag)
	{
		if (Reason != null && !SettlementWorldState.HasReport)
			tag["SettlementSkipReason"] = Reason;
	}

	public override void LoadWorldData(TagCompound tag) =>
		Reason = tag.ContainsKey("SettlementSkipReason") ? tag.GetString("SettlementSkipReason") : null;

	internal static string Describe()
	{
		string code = Reason ?? "unknown";
		string kind = code.Split(':')[0];
		string key = kind switch
		{
			"plan-failed" or "config-disabled" or "not-continental" or "manifest-incomplete" => kind,
			_ => "unknown",
		};
		return Language.GetTextValue($"{LocKey}.Reason.{key}") + $" [{code}]";
	}
}

public sealed class SettlementSkipNoticePlayer : ModPlayer
{
	// Multiplayer clients receive the residence registry a moment after entering the world.
	private const int CheckDelayTicks = 300;
	private int ticksSinceEnter = -1;

	public override void OnEnterWorld() => ticksSinceEnter = 0;

	public override void PostUpdate()
	{
		if (ticksSinceEnter < 0 || Player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server) return;
		if (++ticksSinceEnter < CheckDelayTicks) return;
		ticksSinceEnter = -1;
		if (SettlementWorldState.HasReport && SettlementWorldState.Residences.Count > 0) return;
		Main.NewText(Language.GetTextValue(SettlementSkipNotice.LocKey + ".NoResidences", SettlementSkipNotice.Describe()), new Color(255, 140, 90));
	}
}
