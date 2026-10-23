using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class NpcEscortMobilityGlobalNPC
{
	private void RunEscortAI(NPC npc, string npcKey, ResidentEscortState state)
	{
		Player owner = NpcResidenceWorldSystem.FindOwner(state);
		if (owner is null)
		{
			Stop(npc);
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NpcResidenceWorldSystem.SetWaiting(npcKey, "owner-offline-or-dead");
			return;
		}

		float distanceSquared = Vector2.DistanceSquared(npc.Center, owner.Center);
		if (distanceSquared > 100f * 16f * 100f * 16f)
		{
			Stop(npc);
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NpcResidenceWorldSystem.SetWaiting(npcKey, "owner-too-far");
			return;
		}

		if (NpcResidenceWorldSystem.IsWaitingForDestination(state))
		{
			// An unchanged invalid house must not toggle the state and broadcast
			// twice per tick. Retry while waiting so repairing the house is enough.
			if (Main.netMode == NetmodeID.MultiplayerClient || Main.GameUpdateCount < nextDestinationRetryTick)
			{
				Stop(npc);
				return;
			}
			nextDestinationRetryTick = Main.GameUpdateCount + 60UL;
		}
		if (Main.netMode != NetmodeID.MultiplayerClient && NpcResidenceWorldSystem.TryCompleteEscort(npc, npcKey, owner))
		{
			Stop(npc);
			return;
		}
		if (NpcResidenceWorldSystem.IsWaitingForDestination(state))
		{
			Stop(npc);
			return;
		}
		if (state.Stage == ResidentEscortStage.Waiting && Main.netMode != NetmodeID.MultiplayerClient)
			NpcResidenceWorldSystem.ResumeFollowing(npcKey);

		if (distanceSquared < 4f * 16f * 4f * 16f)
		{
			npc.velocity.X *= 0.72f;
			if (Math.Abs(npc.velocity.X) < 0.08f)
				npc.velocity.X = 0f;
			stuckTicks = 0;
			lastPosition = npc.position;
			return;
		}

		bool progressed = Vector2.DistanceSquared(npc.position, lastPosition) > 0.35f * 0.35f;
		stuckTicks = progressed ? Math.Max(0, stuckTicks - 3) : stuckTicks + 1;
		lastPosition = npc.position;
		replanTimer--;
		if (replanTimer <= 0 || waypointIndex >= path.Count || stuckTicks == StuckReplanTicks)
		{
			path.Clear();
			if (EscortPathfinder.TryBuild(npc, owner.Center.ToTileCoordinates(), path))
			{
				waypointIndex = Math.Min(1, path.Count - 1);
				replanTimer = ReplanInterval;
			}
			else
			{
				waypointIndex = 0;
				replanTimer = 10;
			}
		}

		Point targetTile = waypointIndex < path.Count ? path[waypointIndex] : owner.Bottom.ToTileCoordinates();
		Vector2 target = new(targetTile.X * 16f + 8f, targetTile.Y * 16f - npc.height * 0.5f);
		Vector2 delta = target - npc.Center;
		if (waypointIndex < path.Count && Math.Abs(delta.X) < 12f && Math.Abs(delta.Y) < 22f)
		{
			waypointIndex++;
			if (waypointIndex < path.Count)
			{
				targetTile = path[waypointIndex];
				target = new Vector2(targetTile.X * 16f + 8f, targetTile.Y * 16f - npc.height * 0.5f);
				delta = target - npc.Center;
			}
		}

		int direction = Math.Sign(delta.X);
		if (direction == 0)
			direction = npc.direction == 0 ? 1 : npc.direction;
		npc.direction = direction;
		npc.spriteDirection = direction;
		float desiredSpeed = Math.Abs(delta.X) > 10f * 16f ? 3.4f : 2.4f;
		npc.velocity.X = MathHelper.Clamp(npc.velocity.X + direction * 0.16f, -desiredSpeed, desiredSpeed);
		TryOpenDoor(npc, direction);
		float requiredRise = npc.Bottom.Y - targetTile.Y * 16f;
		if ((requiredRise > 12f || npc.collideX || stuckTicks >= StuckJumpTicks) && npc.collideY)
		{
			float jumpSpeed = MathHelper.Clamp(5.2f + requiredRise / 40f, 5.2f, 7.4f);
			npc.velocity.Y = -jumpSpeed;
			npc.netUpdate = true;
		}

		if (stuckTicks >= StuckWaitTicks)
		{
			Stop(npc);
			path.Clear();
			waypointIndex = 0;
			stuckTicks = 0;
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NpcResidenceWorldSystem.SetWaiting(npcKey, "local-path-blocked");
		}
		else if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 20UL == (ulong)(npc.whoAmI % 20))
		{
			npc.netUpdate = true;
		}
	}
}
