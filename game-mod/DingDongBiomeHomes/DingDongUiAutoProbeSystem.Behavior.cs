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
	public override void PostSetupContent()
	{
		if (Main.dedServ || !string.Equals(Environment.GetEnvironmentVariable(ProbeEnvironmentVariable), "1", StringComparison.Ordinal) || !string.Equals(Environment.GetEnvironmentVariable(MenuProbeEnvironmentVariable), "1", StringComparison.Ordinal))
			return;
		_ = RunMenuProbeAfterLoadAsync();
	}

	private static bool Localized(string value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("Mods.", StringComparison.Ordinal);
	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
