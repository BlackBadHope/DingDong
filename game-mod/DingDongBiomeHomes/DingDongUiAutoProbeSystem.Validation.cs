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
	private void RunMenuProbeImmediate()
	{
		if (finished)
			return;
		try
		{
			if (!Main.gameMenu)
				throw new InvalidOperationException("isolated menu probe did not remain at the main menu");
			DamageGuidePageId[] pages = DamageGuideContent.PageOrder;
			visited.Clear();
			DingDongHubUiSystem.OpenCombatForMenuProbe(pages[0]);
			VerifyState(pages[0]);
			visited.Add(pages[0]);
			for (int index = 1; index < pages.Length; index++)
			{
				DingDongHubUiSystem.MoveDamagePageForProbe(1);
				VerifyState(pages[index]);
				visited.Add(pages[index]);
			}

			DingDongHubUiSystem.MoveDamagePageForProbe(1);
			VerifyState(pages[0]);
			DingDongHubUiSystem.MoveDamagePageForProbe(-1);
			VerifyState(pages[^1]);
			DingDongHubUiSystem.MoveDamagePageForProbe(1);
			VerifyState(pages[0]);
			bool paginationContract = DingDongPagination.ContractPass(out string paginationFailure);
			bool allPagesVisited = visited.Distinct().Count() == pages.Length;
			if (!paginationContract || !allPagesVisited)
				throw new InvalidOperationException($"pagination={paginationFailure};visited={visited.Count}");
			DingDongHubProbeState state = DingDongHubUiSystem.CaptureProbeState();
			Mod.Logger.Info($"DINGDONG_UI_PAGINATION_PROBE|verdict=PASS|context=isolated-menu|pages={pages.Length}|visited={visited.Distinct().Count()}|nextWrap=True|previousWrap=True|hubActive={state.Active}|tab={state.Tab}|page={state.PageNumber}/{state.PageCount}|titleLocalized={Localized(state.Title)}|worldMutated=False");
			finished = true;
		}
		catch (Exception exception)
		{
			Mod.Logger.Error($"DINGDONG_UI_PAGINATION_PROBE|verdict=FAIL|stage=menu-run|exception={exception.GetType().Name}|message={OneLine(exception.Message)}|worldMutated=False");
			finished = true;
		}
		finally
		{
			DingDongHubUiSystem.CloseMenuProbe();
		}
	}

	private static void VerifyState(DamageGuidePageId expected)
	{
		DingDongHubProbeState state = DingDongHubUiSystem.CaptureProbeState();
		if (!state.Active || state.Tab != DingDongHubTab.Combat || state.Page != expected)
			throw new InvalidOperationException($"ui-state:active={state.Active},tab={state.Tab},page={state.Page},expected={expected}");
		if (!Localized(state.Title) || state.PageNumber < 1 || state.PageNumber > state.PageCount)
			throw new InvalidOperationException($"ui-content:title={OneLine(state.Title)},page={state.PageNumber}/{state.PageCount}");
	}
}
