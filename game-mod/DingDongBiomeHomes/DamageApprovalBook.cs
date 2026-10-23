using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// One in-flight transaction per sender. Terraria processes each connection in
/// order, so honest hits complete intent -> commit -> vanilla before the next
/// hit from that player. Separate sender slots keep two clients independent.
/// </summary>
internal sealed class DamageApprovalBook
{
	private readonly int capacity;
	private readonly Dictionary<int, DamagePreparedHit> preparedBySender = new();
	public DamageApprovalBook(int capacity)
	{
		if (capacity <= 0)
			throw new ArgumentOutOfRangeException(nameof(capacity));
		this.capacity = capacity;
	}

	public int Count => preparedBySender.Count;

	public DamageApprovalResult TryPrepare(int sender, DamageHitIntent intent, DamageHitLedger tentativeLedger, float serverMultiplier, long tick, long expiresAtTick, int expectedDamageType, DamagePayloadEnvelope payloadEnvelope = default)
	{
		RemoveIfExpired(sender, tick);
		if (preparedBySender.ContainsKey(sender) || preparedBySender.Count >= capacity)
			return DamageApprovalResult.Busy;
		preparedBySender.Add(sender, new DamagePreparedHit(intent, tentativeLedger, serverMultiplier, expiresAtTick, expectedDamageType, payloadEnvelope));
		return DamageApprovalResult.Prepared;
	}

	public DamageApprovalResult TryCommit(int sender, DamageHitCommit commit, long tick)
	{
		if (!preparedBySender.TryGetValue(sender, out DamagePreparedHit prepared))
			return DamageApprovalResult.Missing;
		if (prepared.ExpiresAtTick < tick)
		{
			preparedBySender.Remove(sender);
			return DamageApprovalResult.Expired;
		}

		if (!commit.Matches(prepared.Intent))
		{
			preparedBySender.Remove(sender);
			return DamageApprovalResult.IdentityMismatch;
		}

		if (commit.Payload.DamageType != prepared.ExpectedDamageType)
		{
			preparedBySender.Remove(sender);
			return DamageApprovalResult.PayloadMismatch;
		}
		if (prepared.PayloadEnvelope.Enforced && !prepared.PayloadEnvelope.Accepts(commit.Payload))
		{
			preparedBySender.Remove(sender);
			return DamageApprovalResult.PayloadMismatch;
		}

		if (prepared.Payload.HasValue)
		{
			preparedBySender.Remove(sender);
			return DamageApprovalResult.DuplicateCommit;
		}

		preparedBySender[sender] = prepared with
		{
			Payload = commit.Payload
		};
		return DamageApprovalResult.Committed;
	}

	public DamageApprovalResult TryConsume(int sender, int targetId, DamageStrikePayload payload, long tick, out DamagePreparedHit prepared)
	{
		if (!preparedBySender.TryGetValue(sender, out prepared))
			return DamageApprovalResult.Missing;
		if (prepared.ExpiresAtTick < tick)
		{
			preparedBySender.Remove(sender);
			prepared = null;
			return DamageApprovalResult.Expired;
		}

		if (prepared.Intent.TargetId != targetId)
		{
			preparedBySender.Remove(sender);
			prepared = null;
			return DamageApprovalResult.IdentityMismatch;
		}

		if (!prepared.Payload.HasValue)
		{
			preparedBySender.Remove(sender);
			prepared = null;
			return DamageApprovalResult.MissingCommit;
		}

		if (!prepared.Payload.Value.MatchesExactly(payload))
		{
			preparedBySender.Remove(sender);
			prepared = null;
			return DamageApprovalResult.PayloadMismatch;
		}

		preparedBySender.Remove(sender);
		return DamageApprovalResult.Consumed;
	}

	public void Prune(long tick)
	{
		List<int> expired = new();
		foreach ((int sender, DamagePreparedHit prepared)in preparedBySender)
		{
			if (prepared.ExpiresAtTick < tick)
				expired.Add(sender);
		}

		foreach (int sender in expired)
			preparedBySender.Remove(sender);
	}

	public void Clear() => preparedBySender.Clear();
	public void Reject(int sender) => preparedBySender.Remove(sender);
	private void RemoveIfExpired(int sender, long tick)
	{
		if (preparedBySender.TryGetValue(sender, out DamagePreparedHit prepared) && prepared.ExpiresAtTick < tick)
		{
			preparedBySender.Remove(sender);
		}
	}
}
