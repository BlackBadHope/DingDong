using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

public sealed class ContinentalSectionStreamingSystem : ModSystem
{
	internal const int PrefetchInterval = 6;
	internal const int SectionBudgetPerSweep = 3;
	internal const int MinimumSendInterval = PrefetchInterval / SectionBudgetPerSweep;
	internal const double SendTimeBudgetMilliseconds = 4;
	private static readonly ContinentalSectionDispatchBudget Budget = new();
	private static readonly int[] Active = new int[Main.maxPlayers];
	private static int nextPlayer;

	public override void PostUpdatePlayers()
	{
		if (!ContinentalWorldDimensions.IsContinental) return;
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			// ACKs stay batched, but no longer wait for the prefetch cadence.
			ContinentalSectionStreamingNetwork.UpdateClient();
			return;
		}
		if (Main.netMode != NetmodeID.Server) return;
		int count = CollectActivePlayers();
		Budget.Refill(Main.GameUpdateCount, count);
		long started = Stopwatch.GetTimestamp();
		// All players' collision repairs precede viewport repairs and prefetch.
		// No in-flight slots are permanently reserved for either request class.
		DispatchRequests(count, urgent: true, started);
		DispatchRequests(count, urgent: false, started);
		for (int index = 0; index < count; index++)
		{
			double allowance = Budget.Available(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
			if (allowance <= 0) break;
			int who = Active[index];
			if (!ContinentalSectionStreamingNetwork.CanPrefetch(who)) continue;
			long attempt = Stopwatch.GetTimestamp();
			bool sent = PrefetchOne(who, allowance);
			ContinentalSectionStreamingNetwork.NotePrefetchScan(who, sent);
			Budget.Charge(Stopwatch.GetElapsedTime(attempt).TotalMilliseconds);
			// A costly scan can find no tiles to send; it still used its turn.
			nextPlayer = (who + 1) % Main.maxPlayers;
		}
	}

	public override void OnWorldUnload() =>
		ContinentalSectionStreamingNetwork.Reset();
	public override void OnWorldLoad() => ContinentalSectionStreamingNetwork.Reset();

	internal static void ResetScheduler()
	{
		Budget.Reset();
		nextPlayer = 0;
	}

	private static int CollectActivePlayers()
	{
		int count = 0;
		for (int offset = 0; offset < Main.maxPlayers; offset++)
		{
			int who = (nextPlayer + offset) % Main.maxPlayers;
			ContinentalSectionFlowControl flow = ContinentalSectionStreamingNetwork.FlowFor(who);
			if (flow == null) continue;
			Active[count++] = who;
			if (Main.GameUpdateCount % 600 == 0 && (flow.InFlight > 0 || flow.Acknowledged > 0))
				DingDongBiomeHomes.Instance?.Logger.Info(
					$"DINGDONG_SECTION_FLOW|sender={who}|inFlight={flow.InFlight}|acked={flow.Acknowledged}|suppressed={flow.Suppressed}|expired={flow.Expired}|roundTripMs={flow.RoundTripMilliseconds}|predictionTicks={flow.PredictionTicks}");
		}
		return count;
	}

	private static void DispatchRequests(int count, bool urgent, long started)
	{
		for (int index = 0; index < count; index++)
		{
			if (Budget.Available(Stopwatch.GetElapsedTime(started).TotalMilliseconds) <= 0) break;
			int who = Active[index];
			if (!ContinentalSectionStreamingNetwork.CanDispatch(who)) continue;
			long attempt = Stopwatch.GetTimestamp();
			ContinentalSectionStreamingNetwork.SendPending(who, urgent);
			Budget.Charge(Stopwatch.GetElapsedTime(attempt).TotalMilliseconds);
			nextPlayer = (who + 1) % Main.maxPlayers;
		}
	}

	private static bool PrefetchOne(int who, double allowance)
	{
		ContinentalSectionFlowControl flow = ContinentalSectionStreamingNetwork.FlowFor(who);
		if (flow == null || flow.InFlight >= ContinentalSectionFlowControl.MaximumInFlight) return false;
		if (Fishing.FishingSectionPrefetch.TrySend(who, allowance)) return true;
		Player player = Main.player[who];
		RemoteClient client = Netplay.Clients[who];
		Point current = SectionAt(player.Center);
		Point predicted = SectionAt(player.Center + player.velocity * flow.PredictionTicks);
		bool sent = false;
		long started = Stopwatch.GetTimestamp();
		ContinentalSectionCorridor.Visit(current.X, current.Y, predicted.X, predicted.Y, (x, y) =>
		{
			ContinentalSectionPrefetchPolicy.Visit(x, y, predicted.X - current.X, predicted.Y - current.Y,
				Math.Min(Main.maxSectionsX, client.TileSections.GetLength(0)),
				Math.Min(Main.maxSectionsY, client.TileSections.GetLength(1)), (sectionX, sectionY) =>
			{
				if (Stopwatch.GetElapsedTime(started).TotalMilliseconds >= allowance) return false;
				sent = ContinentalSectionStreamingNetwork.SendSection(who, sectionX, sectionY);
				return !sent && Stopwatch.GetElapsedTime(started).TotalMilliseconds < allowance;
			});
			return !sent && Stopwatch.GetElapsedTime(started).TotalMilliseconds < allowance;
		});
		return sent;
	}

	private static Point SectionAt(Vector2 worldPosition)
	{
		int tileX = Math.Clamp((int)(worldPosition.X / 16f), 0, Main.maxTilesX - 1);
		int tileY = Math.Clamp((int)(worldPosition.Y / 16f), 0, Main.maxTilesY - 1);
		return new(Netplay.GetSectionX(tileX), Netplay.GetSectionY(tileY));
	}
}
