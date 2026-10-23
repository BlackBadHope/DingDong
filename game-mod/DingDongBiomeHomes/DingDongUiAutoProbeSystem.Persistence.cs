using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class DingDongUiAutoProbeSystem
{
	public override void OnWorldLoad()
	{
		visited.Clear();
		step = -1;
		nextStepTick = Main.GameUpdateCount + 120UL;
		finished = false;
		initialized = true;
	}

	private async Task RunMenuProbeAfterLoadAsync()
	{
		try
		{
			await Task.Delay(18000).ConfigureAwait(false);
			Main.QueueMainThreadAction(RunMenuProbeImmediate);
		}
		catch (Exception exception)
		{
			Mod.Logger.Error($"DINGDONG_UI_PAGINATION_PROBE|verdict=FAIL|stage=menu-queue|exception={exception.GetType().Name}|message={OneLine(exception.Message)}|worldMutated=False");
			finished = true;
		}
	}
}
