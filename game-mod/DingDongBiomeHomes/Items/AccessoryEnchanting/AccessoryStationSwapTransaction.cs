using Terraria;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;

internal static class AccessoryStationSwapTransaction
{
	internal static bool TryApply(
		ref Item stationItem,
		ref Item cursorItem,
		uint expectedRevision,
		ref uint revision,
		bool sessionValid,
		out string failure)
	{
		if (!sessionValid)
		{
			failure = "goblin-session-required";
			return false;
		}
		if (expectedRevision != revision)
		{
			failure = "stale-station-revision";
			return false;
		}
		if (!AccessoryEnchantingStationPlayer.Accepts(cursorItem))
		{
			failure = "accessories-only";
			return false;
		}

		Item swap = stationItem;
		stationItem = cursorItem;
		cursorItem = swap;
		unchecked
		{
			revision++;
		}
		failure = string.Empty;
		return true;
	}
}
