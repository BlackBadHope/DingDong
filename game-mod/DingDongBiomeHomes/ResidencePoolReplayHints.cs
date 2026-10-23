using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;

// Diagnostic search restriction, NOT admission. Every retained coordinate still
// passes the original biome, terrain, persistent-object and structure-map gates.
internal sealed class ResidencePoolReplayHints
{
    private readonly Dictionary<string, HashSet<int>> columns = new();
    private readonly Dictionary<string, HashSet<Rectangle>> areas = new();

    public static ResidencePoolReplayHints Load()
    {
        string path = Environment.GetEnvironmentVariable("DINGDONG_RESIDENCE_POOL_REPLAY");
        if (string.IsNullOrEmpty(path)) return null;
        if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1")
            throw new InvalidOperationException("Pool hints require an explicit dedicated diagnostic.");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        if (root.GetProperty("width").GetInt32() != Main.maxTilesX ||
            root.GetProperty("height").GetInt32() != Main.maxTilesY ||
            root.GetProperty("seed").GetInt32() != Main.ActiveWorldFileData.Seed)
            throw new InvalidOperationException("Pool hint seed/dimensions do not match the loaded fixture.");
        ResidencePoolReplayHints hints = new();
        foreach (JsonProperty pool in root.GetProperty("pools").EnumerateObject())
        {
            HashSet<int> xs = new();
            HashSet<Rectangle> rectangles = new();
            foreach (JsonElement row in pool.Value.EnumerateArray())
            {
                JsonElement a = row.GetProperty("asset");
                Rectangle area = new(a.GetProperty("X").GetInt32(), a.GetProperty("Y").GetInt32(),
                    a.GetProperty("Width").GetInt32(), a.GetProperty("Height").GetInt32());
                xs.Add(area.X);
                rectangles.Add(area);
            }
            hints.columns.Add(pool.Name, xs);
            hints.areas.Add(pool.Name, rectangles);
        }
        return hints;
    }

    public bool ContainsColumn(string npc, int x) => columns.TryGetValue(npc, out var set) && set.Contains(x);
    public bool ContainsArea(string npc, Rectangle area) => areas.TryGetValue(npc, out var set) && set.Contains(area);
}
