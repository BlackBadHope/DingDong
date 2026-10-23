using System;

namespace DingDongBiomeHomes;

/// <summary>
/// Dense symmetric cache for compatibility between globally indexed layout
/// candidates. Two bits per unordered pair record whether the pair is known
/// and, independently, whether it is compatible. This avoids the memory cost
/// and the saturation behaviour of a dictionary entry per pair.
/// </summary>
internal sealed class CandidatePairCompatibilityCache
{
	private readonly int candidateCount;
	private readonly ulong[] known;
	private readonly ulong[] compatible;
	private int count;

	public int Count => count;
	public long PairCapacity { get; }
	public long AllocatedBytes => (long)(known.Length + compatible.Length) * sizeof(ulong);

	public CandidatePairCompatibilityCache(int candidateCount)
	{
		if (candidateCount < 0)
			throw new ArgumentOutOfRangeException(nameof(candidateCount));

		this.candidateCount = candidateCount;
		PairCapacity = (long)candidateCount * (candidateCount - 1) / 2;
		int wordCount = checked((int)((PairCapacity + 63) / 64));
		known = new ulong[wordCount];
		compatible = new ulong[wordCount];
	}

	public bool TryGet(int leftId, int rightId, out bool value)
	{
		int pairIndex = PairIndex(leftId, rightId);
		int wordIndex = pairIndex >> 6;
		ulong bit = 1UL << (pairIndex & 63);
		if ((known[wordIndex] & bit) == 0)
		{
			value = false;
			return false;
		}

		value = (compatible[wordIndex] & bit) != 0;
		return true;
	}

	public void Store(int leftId, int rightId, bool value)
	{
		int pairIndex = PairIndex(leftId, rightId);
		int wordIndex = pairIndex >> 6;
		ulong bit = 1UL << (pairIndex & 63);
		if ((known[wordIndex] & bit) != 0)
		{
			bool existing = (compatible[wordIndex] & bit) != 0;
			if (existing != value)
				throw new InvalidOperationException("Candidate-pair compatibility changed for an immutable cache key.");
			return;
		}

		known[wordIndex] |= bit;
		if (value)
			compatible[wordIndex] |= bit;
		count++;
	}

	private int PairIndex(int leftId, int rightId)
	{
		if ((uint)leftId >= (uint)candidateCount)
			throw new ArgumentOutOfRangeException(nameof(leftId));
		if ((uint)rightId >= (uint)candidateCount)
			throw new ArgumentOutOfRangeException(nameof(rightId));
		if (leftId == rightId)
			throw new ArgumentException("A candidate cannot be paired with itself.");

		int low = Math.Min(leftId, rightId);
		int high = Math.Max(leftId, rightId);
		long rowOffset = (long)low * (2L * candidateCount - low - 1) / 2;
		return checked((int)(rowOffset + high - low - 1));
	}

	public static bool TrySelfTest(out string failure)
	{
		CandidatePairCompatibilityCache cache = new(5);
		cache.Store(0, 1, true);
		cache.Store(4, 0, false);
		cache.Store(2, 4, true);
		if (!cache.TryGet(1, 0, out bool first) || !first ||
			!cache.TryGet(0, 4, out bool second) || second ||
			!cache.TryGet(4, 2, out bool third) || !third ||
			cache.TryGet(1, 4, out _) || cache.Count != 3 || cache.PairCapacity != 10)
		{
			failure = "dense symmetric candidate-pair indexing is inconsistent";
			return false;
		}

		failure = string.Empty;
		return true;
	}
}
