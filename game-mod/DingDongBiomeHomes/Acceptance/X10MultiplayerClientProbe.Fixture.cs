using System;
using System.IO;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Acceptance;

public sealed partial class X10MultiplayerClientProbe
{
	private bool fixtureInitialized;
	public override void OnEnterWorld()
	{
		if (!Active || fixtureInitialized) return;
		try
		{
			string savedir = Path.GetDirectoryName(Path.GetFullPath(ControlPath));
			string playerDirectory = Path.Combine(savedir, "Players") + Path.DirectorySeparatorChar;
			string playerPath = Path.GetFullPath(Main.ActivePlayerFileData?.Path ?? "");
			if (!playerPath.StartsWith(playerDirectory, StringComparison.OrdinalIgnoreCase) ||
				!playerPath.EndsWith(".plr", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("disposable-player-path-guard");
			fixtureInitialized = true;
			int before = Player.statLife;
			int maximum = Player.statLifeMax2;
			if (maximum <= 0) throw new InvalidOperationException("invalid-existing-health-maximum");
			Player.statLife = maximum;
			NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
			X10MultiplayerProbe.Emit($"DINGDONG_X10_MP_FIXTURE|verdict=SETUP|player={Player.whoAmI}" +
				$"|name64={X10MultiplayerProbe.Name(Player)}|oldHP={before}|newHP={Player.statLife}" +
				$"|maxHP={maximum}|originalPlayerFileTouched=False|routeHealing=False");
		}
		catch (Exception error)
		{
			X10MultiplayerProbe.Emit("DINGDONG_X10_MP_FIXTURE|verdict=FAIL|reason=" +
				error.Message.Replace('|', '/'));
		}
	}
}
