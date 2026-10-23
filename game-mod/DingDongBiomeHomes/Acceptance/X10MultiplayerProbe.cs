using System;
using System.IO;
using System.Linq;
using System.Text;
using DingDongBiomeHomes.Climate;
using DingDongBiomeHomes.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Acceptance;

internal static class X10MultiplayerProbe
{
	internal static bool Enabled => Environment.GetEnvironmentVariable("DINGDONG_X10_MP_ACCEPTANCE") == "1";
	private static string stage;
	private static Point anchor;
	internal static void RequireServer()
	{
		string name = Path.GetFileNameWithoutExtension(Main.ActiveWorldFileData?.Path ?? "");
		if (!Enabled || !Main.dedServ || !ContinentalWorldDimensions.IsContinental ||
			!name.StartsWith("DingDongAllModE2E-X10MP-", StringComparison.Ordinal))
			throw new InvalidOperationException("disposable-server-guard");
		if (Main.player.Count(p => p.active && Netplay.Clients[p.whoAmI].State == 10) != 2)
			throw new InvalidOperationException("two-active-network-players-required");
	}
	internal static void Route(string requested)
	{
		anchor = X10MultiplayerRoute.Find(requested);
		stage = requested;
		foreach (Player player in Main.player.Where(p => p.active))
		{
			Vector2 position = new(anchor.X * 16, anchor.Y * 16 - player.height);
			NetMessage.SendSection(player.whoAmI, Netplay.GetSectionX(anchor.X), Netplay.GetSectionY(anchor.Y));
			player.Teleport(position, TeleportationStyleID.TeleporterTile);
			player.velocity = Vector2.Zero;
			NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI,
				position.X, position.Y, TeleportationStyleID.TeleporterTile);
			Emit($"DINGDONG_X10_MP_ROUTE|verdict=READY|stage={stage}|player={player.whoAmI}" +
				$"|name64={Name(player)}|x={anchor.X}|y={anchor.Y}|world={Main.maxTilesX}x{Main.maxTilesY}" +
				$"|terrain={X10MultiplayerTerrain.Hash(anchor.X, anchor.Y)}|plannedTeleport=True");
		}
	}
	internal static void Sample(string requested)
	{
		if (requested != stage) throw new InvalidOperationException("stage-mismatch");
		foreach (Player player in Main.player.Where(p => p.active))
		{
			var climate = player.GetModPlayer<ContinentalClimatePlayer>();
			Emit($"DINGDONG_X10_MP_SAMPLE|verdict=OBSERVED|stage={stage}|player={player.whoAmI}" +
				$"|name64={Name(player)}|x={(int)(player.position.X / 16)}|y={(int)(player.Bottom.Y / 16)}" +
				$"|dead={player.dead}|region={ContinentalClimatePolicy.ResolveRegion(player)}" +
				$"|heat={climate.HeatExposure}|cold={climate.ColdExposure}|jungle={climate.JungleExposure}" +
				$"|heatDelta={ContinentalClimatePolicy.HeatDelta(player)}|coldDelta={ContinentalClimatePolicy.ColdDelta(player)}" +
				$"|jungleDelta={ContinentalClimatePolicy.JungleDelta(player)}" +
				$"|heatBuff={player.HasBuff(Terraria.ModLoader.ModContent.BuffType<HeatstrokeBuff>())}" +
				$"|coldBuff={player.HasBuff(Terraria.ModLoader.ModContent.BuffType<HypothermiaBuff>())}" +
				$"|jungleBuff={player.HasBuff(Terraria.ModLoader.ModContent.BuffType<AmazonPressureBuff>())}" +
				$"|forestBuff={player.HasBuff(Terraria.ModLoader.ModContent.BuffType<ForestComfortBuff>())}" +
				$"|terrain={X10MultiplayerTerrain.Hash(anchor.X, anchor.Y)}");
		}
	}
	internal static string Name(Player player) => Convert.ToBase64String(Encoding.UTF8.GetBytes(player.name));
	internal static void Emit(string line)
	{
		DingDongBiomeHomes.Instance.Logger.Info(line);
		Console.WriteLine(line);
	}
}
