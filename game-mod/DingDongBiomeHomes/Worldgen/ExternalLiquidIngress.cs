using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen;

// Bounded reverse reachability over ONE immutable projected world. The caller
// supplies a conservative upper source-head bound: every projected lava source
// must have Y >= conservativeHighestLavaY. A smaller bound only increases work.
// int.MaxValue/height denotes no eligible source; a dry local window cannot
// establish that claim. Rooms, source bound and callback must share a revision.
internal static class ExternalLiquidIngress
{
    internal enum Decision { Safe, Threat, Unknown }
    internal readonly record struct Result(Decision Verdict, int Reads, int ThreatIndex = -1);
    private sealed class ReadBudgetExceeded : Exception { }

    internal static Result Query(int width, int height, IReadOnlyList<int> rooms,
        int conservativeHighestLavaY, Func<int, byte> read, int maxReads)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(read);
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (maxReads < 0) throw new ArgumentOutOfRangeException(nameof(maxReads));
        int cellCount = checked(width * height);
        if (rooms.Count == 0)
            throw new ArgumentException("Missing protected rooms is not a safe residence.", nameof(rooms));
        int deepestRoomY = -1;
        for (int i = 0; i < rooms.Count; i++)
        {
            int room = rooms[i];
            if (room < 0 || room >= cellCount) throw new ArgumentOutOfRangeException(nameof(rooms));
            deepestRoomY = Math.Max(deepestRoomY, room % height);
        }
        // Negative lower bounds are conservative and mean no top pruning. A
        // bound at/below the world's bottom means all protected rooms are higher.
        int highestLavaY = Math.Clamp(conservativeHighestLavaY, 0, height);
        if (highestLavaY > deepestRoomY) return new(Decision.Safe, 0);

        // best[node] is the greatest attainable minimum Y along any known
        // room-to-node path. A source at Y threatens a room exactly when some
        // such path never goes above Y. All room seeds participate together.
        var best = new Dictionary<int, int>();
        var sampled = new Dictionary<int, byte>();
        var queue = new PriorityQueue<int, int>();
        byte Read(int index)
        {
            if (sampled.TryGetValue(index, out byte known)) return known;
            if (sampled.Count >= maxReads) throw new ReadBudgetExceeded();
            byte value = read(index);
            sampled.Add(index, value);
            return value;
        }
        try
        {
            // Charge seed reads before allocating seed/queue entries. A large
            // room list cannot bypass the same budget by filling the initial
            // frontier. Duplicate room coordinates never allocate twice.
            for (int i = 0; i < rooms.Count; i++)
            {
                int room = rooms[i], y = room % height;
                if (y < highestLavaY || best.ContainsKey(room)) continue;
                byte cell = Read(room);
                if ((cell & LiquidIngressReachability.Open) == 0) continue;
                if ((cell & LiquidIngressReachability.Lava) != 0)
                    return new(Decision.Threat, sampled.Count, room);
                best[room] = y;
                queue.Enqueue(room, -y);
            }
            while (queue.TryDequeue(out int index, out int priority))
            {
                int barrier = -priority;
                if (best[index] != barrier) continue;
                int x = index / height, y = index % height;
                int adjacentThreat = -1;
                void Visit(int next, int nextY)
                {
                    if (adjacentThreat >= 0 || nextY < highestLavaY) return;
                    int nextBarrier = Math.Min(barrier, nextY);
                    if (best.TryGetValue(next, out int previous) && previous >= nextBarrier) return;
                    byte nextCell = Read(next);
                    if ((nextCell & LiquidIngressReachability.Open) == 0) return;
                    // Reject as soon as a valid path reaches a source instead
                    // of postponing it behind a broader, deeper component.
                    if ((nextCell & LiquidIngressReachability.Lava) != 0 && nextBarrier >= nextY)
                    {
                        adjacentThreat = next;
                        return;
                    }
                    best[next] = nextBarrier;
                    queue.Enqueue(next, -nextBarrier);
                }
                if (x > 0) Visit(index - height, y);
                if (x + 1 < width) Visit(index + height, y);
                if (y > 0) Visit(index - 1, y - 1);
                if (y + 1 < height) Visit(index + 1, y + 1);
                if (adjacentThreat >= 0) return new(Decision.Threat, sampled.Count, adjacentThreat);
            }
            return new(Decision.Safe, sampled.Count);
        }
        catch (ReadBudgetExceeded) { return new(Decision.Unknown, sampled.Count); }
    }
}
