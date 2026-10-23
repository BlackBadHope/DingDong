using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DingDongBiomeHomes;
public sealed class DamageHitLedger
{
	private readonly Dictionary<CombatPair, HitBucket> buckets = new();
	public HitWindowResult Register(DamageHitObservation hit, int requiredHits, int windowTicks)
	{
		if (hit.ServerTick < 0)
			throw new ArgumentOutOfRangeException(nameof(hit), hit.ServerTick, "Server tick cannot be negative.");
		if (hit.AttackInstanceId == 0)
			throw new ArgumentException("AttackInstanceId zero is reserved for unclassified attacks.", nameof(hit));
		if (hit.TrueMultistrike && hit.HitEventId == 0)
			throw new ArgumentException("A true-multistrike event requires a non-zero HitEventId.", nameof(hit));
		if (requiredHits < 2)
			throw new ArgumentOutOfRangeException(nameof(requiredHits));
		if (windowTicks < 1)
			throw new ArgumentOutOfRangeException(nameof(windowTicks));
		CombatPair pair = new(hit.AttackerId, hit.TargetId);
		if (!buckets.TryGetValue(pair, out HitBucket bucket))
		{
			bucket = new HitBucket();
			buckets.Add(pair, bucket);
		}

		if (hit.ServerTick < bucket.LatestTick)
			throw new InvalidOperationException($"Server hit ticks must be monotonic for attacker={hit.AttackerId}, target={hit.TargetId}.");
		bucket.LatestTick = hit.ServerTick;
		Prune(bucket.RecentIdentities, hit.ServerTick, windowTicks);
		Prune(bucket.PendingSequence, hit.ServerTick, windowTicks);
		IndependentHitIdentity identity = new(hit.AttackInstanceId, hit.TrueMultistrike ? hit.HitEventId : 0u);
		if (bucket.RecentIdentities.Any(entry => entry.Identity == identity))
			return new HitWindowResult(false, bucket.PendingSequence.Count, false);
		TimedHit entry = new(hit.ServerTick, identity);
		bucket.RecentIdentities.Add(entry);
		bucket.PendingSequence.Add(entry);
		int count = bucket.PendingSequence.Count;
		bool triggered = count == requiredHits;
		if (triggered)
			bucket.PendingSequence.Clear();
		return new HitWindowResult(true, count, triggered);
	}

	public void Reset() => buckets.Clear();
	public DamageHitLedger Clone()
	{
		DamageHitLedger clone = new();
		clone.ReplaceWith(this);
		return clone;
	}

	public void ReplaceWith(DamageHitLedger source)
	{
		ArgumentNullException.ThrowIfNull(source);
		if (ReferenceEquals(this, source))
			return;
		buckets.Clear();
		foreach ((CombatPair pair, HitBucket sourceBucket)in source.buckets)
		{
			HitBucket destination = new()
			{
				LatestTick = sourceBucket.LatestTick
			};
			destination.RecentIdentities.AddRange(sourceBucket.RecentIdentities);
			destination.PendingSequence.AddRange(sourceBucket.PendingSequence);
			buckets.Add(pair, destination);
		}
	}

	private static void Prune(List<TimedHit> entries, long currentTick, int windowTicks) => entries.RemoveAll(entry => currentTick - entry.ServerTick >= windowTicks);
	private readonly record struct CombatPair(int AttackerId, int TargetId);
	private readonly record struct IndependentHitIdentity(ulong AttackInstanceId, uint HitEventId);
	private readonly record struct TimedHit(long ServerTick, IndependentHitIdentity Identity);
	private sealed class HitBucket
	{
		public long LatestTick { get; set; } = -1;
		public List<TimedHit> RecentIdentities { get; } = new();
		public List<TimedHit> PendingSequence { get; } = new();
	}
}
