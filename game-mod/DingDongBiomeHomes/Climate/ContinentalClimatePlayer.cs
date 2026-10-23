using DingDongBiomeHomes.Worldgen;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Climate;

public sealed partial class ContinentalClimatePlayer : ModPlayer
{
	private int heatExposure;
	private int coldExposure;
	private int jungleExposure;
	private int refreshTicks;

	internal int HeatExposure => heatExposure;
	internal int ColdExposure => coldExposure;
	internal int JungleExposure => jungleExposure;

	public override void PreUpdateBuffs()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (!Player.active || Player.dead || !ClimateEnabled)
		{
			ResetExposure();
			return;
		}

		ContinentalClimateRegion region = ContinentalClimatePolicy.ResolveRegion(Player);
		UpdateExposure(region);
		RefreshClimateBuffs(region);
	}

	public override void UpdateDead() => ResetExposure();

	private bool ClimateEnabled =>
		ContinentalWorldDimensions.IsContinental &&
		ModContent.GetInstance<DingDongServerConfig>().EnableContinentalWorlds;

	private void ResetExposure()
	{
		heatExposure = 0;
		coldExposure = 0;
		jungleExposure = 0;
		refreshTicks = 0;
	}
}
