using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class DingDongCommand
{
	public override void Action(CommandCaller caller, string input, string[] args)
	{
		if (args.Length < 1 || args.Length > 2)
		{
			caller.Reply($"Usage: {Usage}", Color.OrangeRed);
			return;
		}

		if (string.Equals(args[0], "audit", StringComparison.OrdinalIgnoreCase) && args.Length == 1)
		{
			foreach (string line in SettlementWorldState.BuildAuditLines())
				caller.Reply(line, Color.LightGreen);
			var failures = ResidenceIntegrityAudit.ReadFailures(Mod);
			foreach (string failure in failures)
			{
				Mod.Logger.Warn("DINGDONG_RESIDENCE_AUDIT_FAILURE|" + failure);
				caller.Reply(failure, Color.OrangeRed);
			}
			caller.Reply($"Structure integrity: {SettlementWorldState.Residences.Count - failures.Count}/{SettlementWorldState.Residences.Count}; visual approval is separate.", failures.Count == 0 ? Color.LightGreen : Color.OrangeRed);
			return;
		}

		if (string.Equals(args[0], "review", StringComparison.OrdinalIgnoreCase))
		{
			RunReview(caller, args.Length == 2 ? args[1] : null);
			return;
		}

		caller.Reply($"Usage: {Usage}", Color.OrangeRed);
	}

	private void RunReview(CommandCaller caller, string selector)
	{
		const string prefix = "DINGDONG_PLAYER_REVIEW_TARGET";
		string worldPath = Main.ActiveWorldFileData?.Path ?? string.Empty;
		string worldFile = Path.GetFileNameWithoutExtension(worldPath);
		if (worldFile.StartsWith(PreviewWorldPrefix, StringComparison.Ordinal))
		{
			RunPreviewReview(caller, prefix, worldFile, selector);
			return;
		}

		RunSettlementReview(caller, prefix, worldFile, selector);
	}

	private void RunPreviewReview(CommandCaller caller, string prefix, string worldFile, string selector)
	{
		if (!PreviewWorldState.PreviewPlaced || !PreviewWorldState.LoadedFromSave)
		{
			ReplyReviewFailure(caller, prefix, "persisted-preview-state-not-loaded");
			return;
		}

		if (!TryResolveReviewId(PreviewWorldState.NpcKey, PreviewWorldState.PresetId, out int reviewId, out string reviewIdentityFailure))
		{
			ReplyReviewFailure(caller, prefix, reviewIdentityFailure);
			return;
		}

		if (selector != null && (!int.TryParse(selector, out int selectedId) || selectedId != reviewId))
		{
			ReplyReviewFailure(caller, prefix, $"preview-world-contains-only-review-id:{reviewId}");
			return;
		}

		Rectangle area = PreviewWorldState.Area;
		FocusReviewTarget(caller, prefix, worldFile, reviewId, PreviewWorldState.NpcKey, PreviewWorldState.PresetId, area, "preview", loadedFromSave: true);
	}

	private void RunSettlementReview(CommandCaller caller, string prefix, string worldFile, string selector)
	{
		if (!SettlementWorldState.HasReport || !SettlementWorldState.HasCompleteResidenceRegistry)
		{
			ReplyReviewFailure(caller, prefix, $"complete-residence-registry-not-loaded:{SettlementWorldState.Residences.Count}/{NpcTownManifest.AuthoredCount}");
			return;
		}

		if (!TryResolveSettlementReviewId(caller, selector, out int reviewId, out string selectorFailure))
		{
			ReplyReviewFailure(caller, prefix, selectorFailure);
			return;
		}

		if (!SettlementWorldState.TryGetResidence(reviewId, out SettlementResidenceReport residence) || !residence.IsCanonical())
		{
			ReplyReviewFailure(caller, prefix, $"canonical-residence-not-found:{reviewId}");
			return;
		}

		FocusReviewTarget(caller, prefix, worldFile, residence.ReviewId, residence.NpcKey, residence.PresetId, residence.Area, "settlement", SettlementWorldState.LoadedFromSave);
	}
}
