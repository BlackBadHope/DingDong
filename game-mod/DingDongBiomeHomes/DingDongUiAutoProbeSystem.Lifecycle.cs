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
	public override void PostUpdateEverything()
	{
		if (finished || Main.dedServ)
			return;
		if (!string.Equals(Environment.GetEnvironmentVariable(ProbeEnvironmentVariable), "1", StringComparison.Ordinal))
			return;
		bool menuProbe = Main.gameMenu && string.Equals(Environment.GetEnvironmentVariable(MenuProbeEnvironmentVariable), "1", StringComparison.Ordinal);
		string worldFile = menuProbe ? "isolated-menu" : Path.GetFileNameWithoutExtension(Main.ActiveWorldFileData?.Path ?? string.Empty);
		if (!menuProbe && (Main.gameMenu || !worldFile.StartsWith(ProbeWorldPrefix, StringComparison.Ordinal)))
			return;
		if (!initialized)
		{
			visited.Clear();
			step = -1;
			nextStepTick = Main.GameUpdateCount + 120UL;
			initialized = true;
		}

		if (Main.GameUpdateCount < nextStepTick)
			return;
		try
		{
			DamageGuidePageId[] pages = DamageGuideContent.PageOrder;
			if (step < 0)
			{
				if (menuProbe)
					DingDongHubUiSystem.OpenCombatForMenuProbe(pages[0]);
				else
					DingDongHubUiSystem.OpenCombat(pages[0]);
				visited.Add(pages[0]);
				step = 0;
				nextStepTick = Main.GameUpdateCount + 15UL;
				return;
			}

			if (step < pages.Length)
			{
				DingDongHubUiSystem.MoveDamagePageForProbe(1);
				step++;
				DamageGuidePageId expected = pages[step % pages.Length];
				VerifyState(expected);
				visited.Add(expected);
				nextStepTick = Main.GameUpdateCount + 15UL;
				return;
			}

			if (step == pages.Length)
			{
				DingDongHubUiSystem.MoveDamagePageForProbe(-1);
				VerifyState(pages[^1]);
				step++;
				nextStepTick = Main.GameUpdateCount + 15UL;
				return;
			}

			DingDongHubUiSystem.MoveDamagePageForProbe(1);
			VerifyState(pages[0]);
			bool paginationContract = DingDongPagination.ContractPass(out string paginationFailure);
			bool allPagesVisited = visited.Take(pages.Length).Distinct().Count() == pages.Length;
			if (!paginationContract || !allPagesVisited)
				throw new InvalidOperationException($"pagination={paginationFailure};visited={visited.Count}");
			DingDongHubProbeState state = DingDongHubUiSystem.CaptureProbeState();
			string marker = $"DINGDONG_UI_PAGINATION_PROBE|verdict=PASS|context={OneLine(worldFile)}|pages={pages.Length}|visited={visited.Take(pages.Length).Distinct().Count()}|nextWrap=True|previousWrap=True|hubActive={state.Active}|tab={state.Tab}|page={state.PageNumber}/{state.PageCount}|titleLocalized={Localized(state.Title)}|worldMutated=False";
			Mod.Logger.Info(marker);
			finished = true;
		}
		catch (Exception exception)
		{
			Mod.Logger.Error($"DINGDONG_UI_PAGINATION_PROBE|verdict=FAIL|exception={exception.GetType().Name}|message={OneLine(exception.Message)}|worldMutated=False");
			finished = true;
		}
		finally
		{
			if (menuProbe && finished)
				DingDongHubUiSystem.CloseMenuProbe();
		}
	}
}
