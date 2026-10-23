using System;
using System.Collections.Generic;

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
	private readonly int capacity;
	private int nextLane;
	internal int Count => entries.Count;

	internal ContinentalLiquidQueue(int laneCount, int capacity)
	{
		if (laneCount <= 0 || capacity <= 0) throw new ArgumentOutOfRangeException();
		this.capacity = capacity;
		lanes = new LinkedList<int>[laneCount];
		for (int i = 0; i < lanes.Length; i++) lanes[i] = new();
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
	}

	internal void Move(int key, int index)
	{
		if (entries.TryGetValue(key, out Entry entry)) entry.Index = index;
	}

	internal void Remove(int key)
	{
		if (!entries.Remove(key, out Entry entry)) return;
		lanes[entry.Lane].Remove(entry.Node);
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
		for (int offset = 0; offset < lanes.Length; offset++)
		{
			int lane = (nextLane + offset) % lanes.Length;
			LinkedListNode<int> node = lanes[lane].First;
			if (node == null) continue;
			key = node.Value;
			index = entries[key].Index;
			nextLane = (lane + 1) % lanes.Length;
			return true;
		}
		key = index = -1;
		return false;
	}

	internal void Clear()
	{
		entries.Clear();
		foreach (LinkedList<int> lane in lanes) lane.Clear();
		nextLane = 0;
	}
}
