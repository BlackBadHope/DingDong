using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalSectionStreamingNetwork
{
	private const byte RequestKind = (byte)DingDongPacketKind.SectionResendRequest;
	private const byte ProbeKind = (byte)DingDongPacketKind.SectionDeliveryProbe;
	private const byte AckKind = (byte)DingDongPacketKind.SectionDeliveryAck;
	internal const int MaximumAckBatch = ContinentalSectionFlowControl.MaximumInFlight;
	private const int MaximumClientPending = 32;
	private static readonly Peer[] Peers = new Peer[Main.maxPlayers];
	private static readonly Dictionary<(int X, int Y), (uint Token, double SeenAt)> ClientProbes = new();
	private static readonly Dictionary<(int X, int Y), (double SentAt, bool Urgent)> ClientRequests = new();
	private static readonly List<(int X, int Y)> ClientReady = new(MaximumClientPending);
	private static double clientRetrySeconds = 2;
	private static double lastClientRequest = double.NegativeInfinity;
	private static double lastUrgentClientRequest = double.NegativeInfinity;
	// Tokens survive world resets, so old acknowledgements cannot match new transfers.
	private static int nextToken;
	internal static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

	internal static bool IsPacketKind(byte kind) =>
		kind is RequestKind or ProbeKind or AckKind;

	internal static bool RequestSection(int sectionX, int sectionY, bool urgent = false)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient ||
			!InBounds(sectionX, sectionY) || !ContinentalWorldDimensions.IsContinental)
			return false;
		double now = Now;
		PruneClientRequests(now);
		bool pending = ClientRequests.TryGetValue((sectionX, sectionY), out var previous);
		double last = urgent ? lastUrgentClientRequest : lastClientRequest;
		double interval = (urgent ? ContinentalSectionRequestPolicy.MinimumUrgentRequestInterval :
			ContinentalSectionRequestPolicy.MinimumRequestInterval) / 60.0;
		if (now - last < interval || pending && now - previous.SentAt < clientRetrySeconds &&
			(!urgent || previous.Urgent) || !pending && ClientRequests.Count >= MaximumClientPending)
			return false;
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write(RequestKind);
		packet.Write((short)sectionX);
		packet.Write((short)sectionY);
		// Version 0.27.30: bounded priority hint; all peers must use this version.
		packet.Write((byte)(urgent ? 1 : 0));
		packet.Send();
		ClientRequests[(sectionX, sectionY)] = (now, urgent);
		if (urgent) lastUrgentClientRequest = now;
		else lastClientRequest = now;
		return true;
	}

	internal static void HandlePacket(BinaryReader reader, int whoAmI, byte kind = RequestKind)
	{
		try
		{
			using BinaryReader bounded = ContinentalSectionPacketEnvelope.Read(reader);
			switch (kind)
			{
				case RequestKind: ReceiveRequest(bounded, whoAmI); break;
				case ProbeKind: ReceiveProbe(bounded); break;
				case AckKind: ReceiveAcknowledgements(bounded, whoAmI); break;
			}
		}
		catch (Exception exception) when (exception is EndOfStreamException or IOException)
		{
			ContinentalSectionPacketEnvelope.Discard(reader);
			DingDongBiomeHomes.Instance?.Logger.Warn(
				$"DINGDONG_SECTION_PACKET_REJECT|sender={whoAmI}|kind={kind}|reason={exception.GetType().Name}");
		}
	}

	private static void ReceiveRequest(BinaryReader reader, int whoAmI)
	{
		int sectionX = reader.ReadInt16(), sectionY = reader.ReadInt16();
		byte priority = reader.ReadByte();
		RequireEnd(reader);
		if (priority > 1) throw new IOException("Invalid section priority.");
		if (Main.netMode != NetmodeID.Server || !ValidSender(whoAmI) ||
			!ContinentalWorldDimensions.IsContinental) return;
		Peer peer = GetPeer(whoAmI);
		Point current = SectionAt(Main.player[whoAmI].Center);
		bool urgent = priority == 1;
		if (peer == null || !ContinentalSectionRequestPolicy.IsAllowed(sectionX, sectionY,
			current.X, current.Y, Main.maxSectionsX, Main.maxSectionsY,
			Main.GameUpdateCount, urgent ? peer.LastUrgentRequestTick : peer.Flow.LastRequestTick,
			urgent) || sectionX >= peer.Sections.GetLength(0) || sectionY >= peer.Sections.GetLength(1)) return;
		// Admission is independent of flight availability: a full window must not
		// discard the very section that currently blocks the player's movement.
		if (!peer.Pending.Enqueue(sectionX, sectionY, urgent, Now)) return;
		ulong acceptedAt = Math.Max(1UL, Main.GameUpdateCount);
		if (urgent) peer.LastUrgentRequestTick = acceptedAt;
		else peer.Flow.LastRequestTick = acceptedAt;
	}

	internal static bool CanDispatch(int who)
	{
		Peer peer = GetPeer(who);
		return peer != null && Main.GameUpdateCount >= peer.NextDispatchTick;
	}

	internal static bool CanPrefetch(int who)
	{
		Peer peer = GetPeer(who);
		return peer != null && Main.GameUpdateCount >= peer.NextDispatchTick &&
			Main.GameUpdateCount >= peer.NextPrefetchTick;
	}

	internal static void NotePrefetchScan(int who, bool sent)
	{
		Peer peer = GetPeer(who);
		if (peer != null) peer.NextPrefetchTick = Main.GameUpdateCount +
			(ulong)(sent ? ContinentalSectionStreamingSystem.MinimumSendInterval :
			ContinentalSectionStreamingSystem.PrefetchInterval);
	}

	internal static bool SendPending(int who, bool urgent)
	{
		Peer peer = GetPeer(who);
		if (peer == null) return false;
		Point current = SectionAt(Main.player[who].Center);
		int attempts = peer.Pending.Count;
		while (attempts-- > 0 && peer.Pending.TryDequeue(urgent, Now, out var request))
		{
			// Position and connection state may have changed while this was queued.
			if (!InBounds(request.X, request.Y) || request.X >= peer.Sections.GetLength(0) ||
				request.Y >= peer.Sections.GetLength(1) ||
				Math.Abs(request.X - current.X) > ContinentalSectionRequestPolicy.MaximumSectionDistance ||
				Math.Abs(request.Y - current.Y) > ContinentalSectionRequestPolicy.MaximumSectionDistance) continue;
			if (!SendSection(who, request.X, request.Y, recovery: true))
			{
				peer.Pending.Requeue(request);
				continue;
			}
			DingDongBiomeHomes.Instance?.Logger.Info(
				$"DINGDONG_SECTION_RESEND_BURST|sender={who}|sectionX={request.X}|sectionY={request.Y}|sent=1|urgent={urgent}|inFlight={peer.Flow.InFlight}");
			return true;
		}
		return false;
	}

	internal static bool SendSection(int who, int x, int y, bool recovery = false)
	{
		Peer peer = GetPeer(who);
		if (peer == null || !InBounds(x, y) || x >= peer.Sections.GetLength(0) ||
			y >= peer.Sections.GetLength(1) || !recovery && peer.Sections[x, y] ||
			!peer.Flow.CanSend(x, y, Now, recovery)) return false;
		if (recovery) peer.Sections[x, y] = false;
		NetMessage.SendSection(who, x, y);
		if (!peer.Sections[x, y]) return false;
		uint token;
		do { token = unchecked((uint)Interlocked.Increment(ref nextToken)); } while (token == 0);
		peer.Flow.Record(x, y, token, Now);
		peer.NextDispatchTick = Main.GameUpdateCount + (ulong)ContinentalSectionStreamingSystem.MinimumSendInterval;
		// A marker follows tile data on the same reliable ordered connection.
		// The client only acknowledges after its sectionManager reports loaded.
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write(ProbeKind);
		packet.Write((short)x);
		packet.Write((short)y);
		packet.Write(token);
		packet.Write((ushort)Math.Ceiling(peer.Flow.RetrySeconds * 60));
		packet.Send(who);
		return true;
	}

	private static void ReceiveProbe(BinaryReader reader)
	{
		int x = reader.ReadInt16(), y = reader.ReadInt16();
		uint token = reader.ReadUInt32();
		int retryTicks = reader.ReadUInt16();
		RequireEnd(reader);
		if (Main.netMode != NetmodeID.MultiplayerClient || !ContinentalWorldDimensions.IsContinental ||
			!InBounds(x, y) || token == 0 || retryTicks < 60 || retryTicks > 360 ||
			!ClientProbes.ContainsKey((x, y)) && ClientProbes.Count >= MaximumClientPending) return;
		ClientProbes[(x, y)] = (token, Now);
		clientRetrySeconds = retryTicks / 60.0;
	}

	internal static void UpdateClient()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient || Main.sectionManager == null ||
			!ContinentalWorldDimensions.IsContinental) return;
		PruneClientRequests(Now);
		ClientReady.Clear();
		foreach (var probe in ClientProbes)
			if (InBounds(probe.Key.X, probe.Key.Y) &&
				Main.sectionManager.SectionLoaded(probe.Key.X, probe.Key.Y))
			{
				ClientReady.Add(probe.Key);
				if (ClientReady.Count == MaximumAckBatch) break;
			}
		if (ClientReady.Count == 0) return;
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write(AckKind);
		packet.Write((byte)ClientReady.Count);
		foreach (var key in ClientReady) packet.Write(ClientProbes[key].Token);
		packet.Send();
		foreach (var key in ClientReady) { ClientProbes.Remove(key); ClientRequests.Remove(key); }
	}

	private static void ReceiveAcknowledgements(BinaryReader reader, int who)
	{
		int count = reader.ReadByte();
		if (count < 1 || count > MaximumAckBatch) throw new IOException("Invalid ACK count.");
		Span<uint> tokens = stackalloc uint[MaximumAckBatch];
		for (int i = 0; i < count; i++) tokens[i] = reader.ReadUInt32();
		RequireEnd(reader); // Fully validate before releasing any credits.
		if (Main.netMode != NetmodeID.Server || !ValidSender(who) ||
			!ContinentalWorldDimensions.IsContinental) return;
		Peer peer = GetPeer(who);
		if (peer == null) return;
		double now = Now;
		for (int i = 0; i < count; i++) peer.Flow.Acknowledge(tokens[i], now);
	}

	internal static ContinentalSectionFlowControl FlowFor(int who) => GetPeer(who)?.Flow;

	private static Peer GetPeer(int who)
	{
		if (!ValidSender(who) || Netplay.Clients[who].TileSections == null)
		{
			if (who >= 0 && who < Peers.Length) Peers[who] = null;
			return null;
		}
		RemoteClient client = Netplay.Clients[who];
		Peer peer = Peers[who];
		if (peer == null || !ReferenceEquals(peer.Client, client) ||
			!ReferenceEquals(peer.Sections, client.TileSections))
			Peers[who] = peer = new(client, client.TileSections);
		peer.Flow.Expire(Now);
		return peer;
	}

	private static void PruneClientRequests(double now)
	{
		ClientReady.Clear();
		foreach (var request in ClientRequests)
			if (now - request.Value.SentAt >= 10 || Main.sectionManager != null &&
				InBounds(request.Key.X, request.Key.Y) &&
				Main.sectionManager.SectionLoaded(request.Key.X, request.Key.Y)) ClientReady.Add(request.Key);
		foreach (var key in ClientReady) ClientRequests.Remove(key);
		ClientReady.Clear();
		foreach (var probe in ClientProbes)
			if (now - probe.Value.SeenAt >= 20) ClientReady.Add(probe.Key);
		foreach (var key in ClientReady) ClientProbes.Remove(key);
	}

	internal static void ResetPeer(int who)
	{
		if (who >= 0 && who < Peers.Length)
			Peers[who] = null;
	}

	internal static void Reset()
	{
		Array.Clear(Peers);
		ClientProbes.Clear(); ClientRequests.Clear(); ClientReady.Clear();
		clientRetrySeconds = 2; lastClientRequest = double.NegativeInfinity;
		lastUrgentClientRequest = double.NegativeInfinity;
		ContinentalSectionStreamingSystem.ResetScheduler();
	}

	private static void RequireEnd(BinaryReader reader)
	{
		if (reader.BaseStream.CanSeek && reader.BaseStream.Position != reader.BaseStream.Length)
			throw new IOException("Trailing section packet data.");
	}
	private static bool InBounds(int x, int y) => x >= 0 && y >= 0 &&
		x < Main.maxSectionsX && y < Main.maxSectionsY;
	private static bool ValidSender(int who) => who >= 0 && who < Main.maxPlayers &&
		Main.player != null && who < Main.player.Length && Main.player[who].active &&
		Netplay.Clients != null && who < Netplay.Clients.Length &&
		Netplay.Clients[who] != null && Netplay.Clients[who].IsActive && Netplay.Clients[who].State >= 10;
	private static Point SectionAt(Vector2 position) => new(
		Netplay.GetSectionX(Math.Clamp((int)(position.X / 16f), 0, Main.maxTilesX - 1)),
		Netplay.GetSectionY(Math.Clamp((int)(position.Y / 16f), 0, Main.maxTilesY - 1)));
	private sealed record Peer(RemoteClient Client, bool[,] Sections)
	{
		internal ContinentalSectionFlowControl Flow { get; } = new();
		internal ContinentalSectionRequestQueue Pending { get; } = new();
		internal ulong LastUrgentRequestTick { get; set; }
		internal ulong NextDispatchTick { get; set; }
		internal ulong NextPrefetchTick { get; set; }
	}
}
