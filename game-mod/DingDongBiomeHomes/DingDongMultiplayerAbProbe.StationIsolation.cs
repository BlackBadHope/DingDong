using System.Collections.Generic;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

internal static partial class DingDongMultiplayerAbProbe
{
	private static void RunStationIsolation(Player playerA, Player playerB, List<string> failures, ref int passed)
	{
		AccessoryEnchantingStationPlayer stationA = playerA.GetModPlayer<AccessoryEnchantingStationPlayer>();
		AccessoryEnchantingStationPlayer stationB = playerB.GetModPlayer<AccessoryEnchantingStationPlayer>();
		Item cursorA = NewItem(ItemID.Aglet);
		bool swapped = stationA.TrySwap(AccessoryStationSlot.Target, ref cursorA, stationA.Revision, sessionValid: true, out _);
		Check(failures, "items:station-authority-isolation", swapped && stationA.Target.type == ItemID.Aglet && cursorA.IsAir && stationA.Revision == 1 && stationB.Target.IsAir && stationB.Revision == 0, ref passed);

		Item replayCursor = new();
		replayCursor.TurnToAir();
		bool replayRejected = !stationA.TrySwap(AccessoryStationSlot.Target, ref replayCursor, expectedRevision: 0, sessionValid: true, out _);
		Check(failures, "items:station-stale-replay", replayRejected && stationA.Target.type == ItemID.Aglet && replayCursor.IsAir && stationA.Revision == 1 && stationB.Target.IsAir, ref passed);
	}
}
