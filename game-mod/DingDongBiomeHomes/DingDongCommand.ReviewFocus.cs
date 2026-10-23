using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class DingDongCommand
{
	private void FocusReviewTarget(CommandCaller caller, string prefix, string worldFile, int reviewId, string npcKey, string presetId, Rectangle area, string source, bool loadedFromSave)
	{
		bool approved = NpcAuthoredPresetManifest.TryGet(npcKey, out AuthoredPreset approvedPreset) && string.Equals(approvedPreset.Id, presetId, StringComparison.Ordinal);
		string target = $"reviewId={reviewId}|world={worldFile}|npc={npcKey}|preset={presetId}|area={area.X},{area.Y},{area.Width},{area.Height}|source={source}|loadedFromSave={loadedFromSave}|ownerReply={reviewId}:PASS-or-FAIL-with-reason";
		if (Main.dedServ)
		{
			if (!TryFindSafeReviewPosition(area, DefaultReviewPlayerWidth, DefaultReviewPlayerHeight, out Vector2 dedicatedPosition))
			{
				ReplyReviewFailure(caller, prefix, $"safe-focus-position-not-found:area={area.X},{area.Y},{area.Width},{area.Height}");
				return;
			}

			int dedicatedTargetTileX = (int)((dedicatedPosition.X + DefaultReviewPlayerWidth * 0.5f) / 16f);
			int dedicatedTargetTileY = (int)((dedicatedPosition.Y + DefaultReviewPlayerHeight) / 16f);
			string dedicatedSummary = $"{prefix}|verdict=READY|{target}|targetTile={dedicatedTargetTileX},{dedicatedTargetTileY}|focusReady=True|teleported=False|reason=dedicated-preflight|playerVisualApproved={approved}|approved={NpcAuthoredPresetManifest.Count}/61";
			Mod.Logger.Info(dedicatedSummary);
			caller.Reply(dedicatedSummary, Color.LightGreen);
			return;
		}

		if (Main.netMode != NetmodeID.SinglePlayer)
		{
			ReplyReviewFailure(caller, prefix, "review-focus-requires-single-player");
			return;
		}

		Player player = caller.Player;
		if (player == null || !player.active)
		{
			ReplyReviewFailure(caller, prefix, "active-player-not-found");
			return;
		}

		if (!TryFindSafeReviewPosition(area, player.width, player.height, out Vector2 position))
		{
			ReplyReviewFailure(caller, prefix, $"safe-focus-position-not-found:area={area.X},{area.Y},{area.Width},{area.Height}");
			return;
		}

		player.Teleport(position, TeleportationStyleID.DebugTeleport);
		player.velocity = Vector2.Zero;
		player.fallStart = (int)(player.position.Y / 16f);
		int targetTileX = (int)((position.X + player.width * 0.5f) / 16f);
		int targetTileY = (int)((position.Y + player.height) / 16f);
		string summary = $"{prefix}|verdict=READY|{target}|targetTile={targetTileX},{targetTileY}|teleported=True|worldMutated=False|playerVisualApproved={approved}|approved={NpcAuthoredPresetManifest.Count}/61";
		Mod.Logger.Info(summary);
		caller.Reply(summary, Color.LightGreen);
	}

	private void ReplyReviewFailure(CommandCaller caller, string prefix, string reason)
	{
		string summary = $"{prefix}|verdict=FAIL|reason={reason.Replace('|', '/')}|worldMutated=False|playerVisualApproved=False|approved={NpcAuthoredPresetManifest.Count}/61";
		Mod.Logger.Warn(summary);
		caller.Reply(summary, Color.OrangeRed);
	}
}
