using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

internal readonly record struct OriginScanRegion(int Left, int Top, int Width, int Height);
internal readonly record struct OriginScanPoint(int X, int Y);
internal delegate bool OriginHousingValidator(int x, int y, int npcType, out OriginScanPoint home);

// One shared server queue. AI lookups enqueue only; unsuccessful rooms cannot
// restart a whole-area scan in every NPC's PreAI and PostAI.
internal sealed class OriginHousingScanQueue<T> where T : class
{
    internal const int MaximumPositionsPerTick = 256;
    internal const int MaximumValidationsPerTick = 2;
    internal const ulong FailedRetryTicks = 600;
    private readonly Queue<Job> jobs = new();
    private readonly HashSet<string> pending = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ulong> retryAfter = new(StringComparer.Ordinal);
    internal int Count => pending.Count;

    internal void Clear() { jobs.Clear(); pending.Clear(); retryAfter.Clear(); }
    internal void Enqueue(string key, T state, OriginScanRegion region, int npcType, ulong tick)
    {
        if (pending.Contains(key) || (retryAfter.TryGetValue(key, out ulong next) && tick < next)) return;
        if (region.Width <= 2 || region.Height <= 2 || pending.Count >= 128) return;
        retryAfter.Remove(key);
        pending.Add(key);
        jobs.Enqueue(new Job(key, state, region, npcType));
    }

    internal int Pump(ulong tick, Func<bool> withinTimeBudget, Func<int, int, bool> eligible,
        OriginHousingValidator validate, Action<T, OriginScanPoint> resolved)
    {
        int positions = 0, validations = 0, completed = 0;
        while (jobs.Count > 0 && positions < MaximumPositionsPerTick &&
            validations < MaximumValidationsPerTick && withinTimeBudget())
        {
            Job job = jobs.Dequeue();
            int x = job.X, y = job.Y;
            job.Advance();
            positions++;
            bool found = false;
            OriginScanPoint home = default;
            if (eligible(x, y))
            {
                validations++;
                found = validate(x, y, job.NpcType, out home);
                // Native scoring can choose a home outside the searched shell.
                found &= home.X >= job.Region.Left && home.Y >= job.Region.Top &&
                    home.X < (long)job.Region.Left + job.Region.Width &&
                    home.Y < (long)job.Region.Top + job.Region.Height;
            }
            if (found)
            {
                pending.Remove(job.Key);
                resolved(job.State, home);
                completed++;
            }
            else if (job.Finished)
            {
                pending.Remove(job.Key);
                retryAfter[job.Key] = tick + FailedRetryTicks;
            }
            else jobs.Enqueue(job);
        }
        return completed;
    }

    private sealed class Job
    {
        internal readonly string Key;
        internal readonly T State;
        internal readonly OriginScanRegion Region;
        internal readonly int NpcType;
        internal int X, Y;
        internal bool Finished => Y >= (long)Region.Top + Region.Height - 1;
        internal Job(string key, T state, OriginScanRegion region, int npcType)
        { Key = key; State = state; Region = region; NpcType = npcType; X = region.Left + 1; Y = region.Top + 1; }
        internal void Advance()
        {
            X += 2;
            if (X >= (long)Region.Left + Region.Width - 1) { X = Region.Left + 1; Y += 2; }
        }
    }
}
