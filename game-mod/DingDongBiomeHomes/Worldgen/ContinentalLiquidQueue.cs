using System;
using System.Collections.Generic;
using System.Numerics;

namespace DingDongBiomeHomes.Worldgen;

// Sidecar only: Terraria retains ownership of liquidBuffer and its tile flags.
// Native swap-delete changes an index, never the FIFO position in a lane.
internal sealed class ContinentalLiquidQueue
{
	private sealed class Entry
	{
		internal int Index;
		internal readonly int Lane;
		internal readonly LinkedListNode<int> Node;
		internal Entry(int index, int lane, LinkedListNode<int> node)
		{ Index = index; Lane = lane; Node = node; }
	}

	private readonly Dictionary<int, Entry> entries = new();
	private readonly LinkedList<int>[] lanes;
	// PerfOpt 28.10: one bit per non-empty lane, so TryNext no longer probes all 256 lanes per take.
	private readonly ulong[] occupied;
	private readonly int capacity;
	private int nextLane;
	internal int Count => entries.Count;

	internal ContinentalLiquidQueue(int laneCount, int capacity)
	{
		if (laneCount <= 0 || capacity <= 0) throw new ArgumentOutOfRangeException();
		this.capacity = capacity;
		lanes = new LinkedList<int>[laneCount];
		for (int i = 0; i < lanes.Length; i++) lanes[i] = new();
		occupied = new ulong[(laneCount + 63) >> 6]; // PerfOpt 28.10
	}

	internal bool Contains(int key) => entries.ContainsKey(key);
	internal bool TryIndex(int key, out int index)
	{
		if (entries.TryGetValue(key, out Entry entry)) { index = entry.Index; return true; }
		index = -1;
		return false;
	}

	internal void Add(int key, int index, int lane)
	{
		if (entries.ContainsKey(key)) throw new InvalidOperationException("Duplicate native liquid cell.");
		if (entries.Count >= capacity) throw new InvalidOperationException("Liquid sidecar capacity exceeded.");
		if ((uint)lane >= (uint)lanes.Length) throw new ArgumentOutOfRangeException(nameof(lane));
		entries.Add(key, new Entry(index, lane, lanes[lane].AddLast(key)));
		occupied[lane >> 6] |= 1UL << (lane & 63); // PerfOpt 28.10
	}

	internal void Move(int key, int index)
	{
		if (entries.TryGetValue(key, out Entry entry)) entry.Index = index;
	}

	internal void Remove(int key)
	{
		if (!entries.Remove(key, out Entry entry)) return;
		LinkedList<int> lane = lanes[entry.Lane];
		lane.Remove(entry.Node);
		if (lane.Count == 0) occupied[entry.Lane >> 6] &= ~(1UL << (entry.Lane & 63)); // PerfOpt 28.10
	}

	internal void RotateToEnd(int key)
	{
		if (!entries.TryGetValue(key, out Entry entry)) return;
		LinkedList<int> lane = lanes[entry.Lane];
		lane.Remove(entry.Node);
		lane.AddLast(entry.Node);
	}

	internal bool TryNext(out int key, out int index)
	{
		// PerfOpt 28.10: same round-robin order as the old per-lane probe, but
		// skips empty lanes 64 at a time (at most occupied.Length + 1 words).
		int lane = NextOccupied(nextLane);
		if (lane < 0)
		{
			key = index = -1;
			return false;
		}
		LinkedListNode<int> node = lanes[lane].First;
		key = node.Value;
		index = entries[key].Index;
		nextLane = (lane + 1) % lanes.Length;
		return true;
	}

	// PerfOpt 28.10
	private int NextOccupied(int start)
	{
		int word = start >> 6;
		ulong bits = occupied[word] & (~0UL << (start & 63));
		for (int visited = 0; visited <= occupied.Length; visited++)
		{
			if (bits != 0)
				return (word << 6) + BitOperations.TrailingZeroCount(bits);
			word = word + 1 == occupied.Length ? 0 : word + 1;
			bits = occupied[word];
		}
		return -1;
	}

	internal void Clear()
	{
		entries.Clear();
		foreach (LinkedList<int> lane in lanes) lane.Clear();
		Array.Clear(occupied); // PerfOpt 28.10
		nextLane = 0;
	}
}
