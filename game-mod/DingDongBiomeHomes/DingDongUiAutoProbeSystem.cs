using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class DingDongUiAutoProbeSystem : ModSystem
{
	private const string ProbeEnvironmentVariable = "DINGDONG_UI_PROBE";
	private const string MenuProbeEnvironmentVariable = "DINGDONG_UI_MENU_PROBE";
	private const string ProbeWorldPrefix = "DingDongUiProbe-";
	private readonly List<DamageGuidePageId> visited = new();
	private int step;
	private ulong nextStepTick;
	private bool finished;
	private bool initialized;
}
