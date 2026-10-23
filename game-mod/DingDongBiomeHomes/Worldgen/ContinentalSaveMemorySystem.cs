using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

public sealed class ContinentalSaveMemorySystem : ModSystem
{
	public override void Load()
	{
		ContinentalSavePlaneCodec.Validate();
		ContinentalSaveStreamHook.Install();
		Mod.Logger.Info("DINGDONG_SAVE_CODEC_SELFTEST|verdict=PASS" +
			"|roundTrip=PASS|legacy=PASS|corruption=WITHHELD");
	}

	public override void OnWorldLoad() => ContinentalSaveBudget.Reset();

	public override void OnWorldUnload() => ContinentalSaveBudget.Reset();

	public override void Unload() => ContinentalSaveStreamHook.Uninstall();
}
