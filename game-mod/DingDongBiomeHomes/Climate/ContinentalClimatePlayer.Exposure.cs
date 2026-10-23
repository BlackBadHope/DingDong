using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Climate;

public sealed partial class ContinentalClimatePlayer
{
	private void UpdateExposure(ContinentalClimateRegion region)
	{
		int recovery = region == ContinentalClimateRegion.Forest
			? ContinentalClimatePolicy.ForestRecovery
			: ContinentalClimatePolicy.NormalRecovery;
		heatExposure = ContinentalClimatePolicy.Step(heatExposure,
			region == ContinentalClimateRegion.Sahara ? ContinentalClimatePolicy.HeatDelta(Player) : -recovery);
		coldExposure = ContinentalClimatePolicy.Step(coldExposure,
			region == ContinentalClimateRegion.Arctic ? ContinentalClimatePolicy.ColdDelta(Player) : -recovery);
		jungleExposure = ContinentalClimatePolicy.Step(jungleExposure,
			region == ContinentalClimateRegion.Amazon ? ContinentalClimatePolicy.JungleDelta(Player) : -recovery);
	}

	private void RefreshClimateBuffs(ContinentalClimateRegion region)
	{
		refreshTicks = (refreshTicks + 1) % ContinentalClimatePolicy.BuffRefreshInterval;
		bool pulse = refreshTicks == 0;
		RefreshBuff<HeatstrokeBuff>(heatExposure >= ContinentalClimatePolicy.DebuffThreshold, pulse);
		RefreshBuff<HypothermiaBuff>(coldExposure >= ContinentalClimatePolicy.DebuffThreshold, pulse);
		RefreshBuff<AmazonPressureBuff>(jungleExposure >= ContinentalClimatePolicy.DebuffThreshold, pulse);
		bool recoveredForest = region == ContinentalClimateRegion.Forest &&
			heatExposure < ContinentalClimatePolicy.DebuffThreshold &&
			coldExposure < ContinentalClimatePolicy.DebuffThreshold &&
			jungleExposure < ContinentalClimatePolicy.DebuffThreshold;
		RefreshBuff<ForestComfortBuff>(recoveredForest, pulse);
	}

	private void RefreshBuff<T>(bool active, bool pulse) where T : ModBuff
	{
		if (!active)
			return;
		int type = ModContent.BuffType<T>();
		if (pulse || !Player.HasBuff(type))
			Player.AddBuff(type, ContinentalClimatePolicy.BuffDuration, quiet: false);
	}
}
