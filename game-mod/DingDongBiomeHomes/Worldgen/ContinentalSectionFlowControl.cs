using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen;

// Credits cover DingDong prefetch/recovery, not vanilla foreground delivery.
internal sealed class ContinentalSectionFlowControl
{
	internal const int MaximumInFlight = 6;
	private readonly Dictionary<(int X, int Y), Flight> flights = new();
	private readonly List<(int X, int Y)> expired = new(MaximumInFlight);
	private double roundTripSeconds = 0.5;
	internal ulong LastRequestTick { get; set; }
	internal int InFlight => flights.Count;
	internal int RoundTripMilliseconds => (int)Math.Ceiling(roundTripSeconds * 1000);
	internal double RetrySeconds => Math.Clamp(roundTripSeconds * 3 + 0.25, 1, 6);
	internal int PredictionTicks => Math.Clamp((int)Math.Ceiling((roundTripSeconds + 0.5) * 60), 120, 300);
	internal long Acknowledged { get; private set; }
	internal long Suppressed { get; private set; }
	internal long Expired { get; private set; }

	internal bool CanSend(int x, int y, double now, bool recovery)
	{
		Expire(now);
		bool allowed = flights.TryGetValue((x, y), out Flight flight)
			? recovery && now >= flight.SentAt && now - flight.SentAt >= RetrySeconds
			: flights.Count < MaximumInFlight;
		if (!allowed) Suppressed++;
		return allowed;
	}

	internal void Record(int x, int y, uint token, double now)
	{
		if (token == 0 || !double.IsFinite(now)) throw new ArgumentOutOfRangeException(nameof(token));
		if (!flights.ContainsKey((x, y)) && flights.Count >= MaximumInFlight)
			throw new InvalidOperationException("Section delivery window is full.");
		flights[(x, y)] = new(token, now);
	}

	internal bool Acknowledge(uint token, double now)
	{
		if (token == 0 || !double.IsFinite(now)) return false;
		foreach (var entry in flights)
		{
			Flight flight = entry.Value;
			if (flight.Token != token || now < flight.SentAt) continue;
			double sample = Math.Clamp(now - flight.SentAt, 1.0 / 60, 5);
			roundTripSeconds = roundTripSeconds * 0.75 + sample * 0.25;
			flights.Remove(entry.Key);
			Acknowledged++;
			return true;
		}
		return false;
	}

	internal void Expire(double now)
	{
		expired.Clear();
		double timeout = Math.Max(10, RetrySeconds * 3);
		foreach (var entry in flights)
			if (now >= entry.Value.SentAt && now - entry.Value.SentAt >= timeout) expired.Add(entry.Key);
		foreach (var key in expired) { flights.Remove(key); Expired++; }
	}

	private sealed record Flight(uint Token, double SentAt);
}
