using System.Globalization;
using DingDongBiomeHomes.Climate;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Acceptance;

public sealed partial class X10MultiplayerClientProbe
{
	private bool ClimateReady() => stage switch
	{
		"sahara" => Player.HasBuff(ModContent.BuffType<HeatstrokeBuff>()),
		"arctic" => Player.HasBuff(ModContent.BuffType<HypothermiaBuff>()),
		"amazon" => Player.HasBuff(ModContent.BuffType<AmazonPressureBuff>()),
		"forest" => Player.HasBuff(ModContent.BuffType<ForestComfortBuff>()),
		_ => false
	};
	private bool Loaded()
	{
		int sx = Netplay.GetSectionX((int)(Player.Center.X / 16));
		int sy = Netplay.GetSectionY((int)(Player.Bottom.Y / 16));
		int floor = Netplay.GetSectionY(anchor.Y + 16);
		return Main.sectionManager.SectionLoaded(sx, sy) &&
			Main.sectionManager.SectionLoaded(Netplay.GetSectionX(anchor.X), floor);
	}
	private void Emit(string verdict, string reason)
	{
		string moved = (Player.position.X - start.X).ToString("F1", CultureInfo.InvariantCulture);
		string step = maxStep.ToString("F1", CultureInfo.InvariantCulture);
		X10MultiplayerProbe.Emit($"DINGDONG_X10_MP_CLIENT|verdict={verdict}|stage={stage}" +
			$"|player={Player.whoAmI}|name64={X10MultiplayerProbe.Name(Player)}" +
			$"|world={Main.maxTilesX}x{Main.maxTilesY}|x={(int)(Player.position.X / 16)}" +
			$"|y={(int)(Player.Bottom.Y / 16)}|movedPixels={moved}|crossedSection={crossed}" +
			$"|ticks={ticks}|maxStep={step}|unloadedTicks={unloadedTicks}|loaded={Loaded()}" +
			$"|hp={Player.statLife}|maxHP={Player.statLifeMax2}" +
			$"|heatBuff={Player.HasBuff(ModContent.BuffType<HeatstrokeBuff>())}" +
			$"|coldBuff={Player.HasBuff(ModContent.BuffType<HypothermiaBuff>())}" +
			$"|jungleBuff={Player.HasBuff(ModContent.BuffType<AmazonPressureBuff>())}" +
			$"|forestBuff={Player.HasBuff(ModContent.BuffType<ForestComfortBuff>())}" +
			$"|terrain={X10MultiplayerTerrain.Hash(anchor.X, anchor.Y)}|dead={Player.dead}|reason={reason}");
	}
}
