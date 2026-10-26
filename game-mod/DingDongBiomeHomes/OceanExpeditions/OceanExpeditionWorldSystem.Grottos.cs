using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.OceanExpeditions;

public sealed partial class OceanExpeditionWorldSystem
{
    private static bool TryPlaceAdaptiveGrotto(OceanExpeditionLayout p, Admission admission,
        OceanGrotto requested, int roomCount, ref int candidates, ref int blocked)
    {
        int minY = p.SeaLevel + (int)(p.Depth * .12), maxY = p.Bottom - 140;
        int localRange = Math.Max(384, p.Depth / Math.Max(2, roomCount * 2));
        // Prefer the requested depth band. Only use the entire depth as a last
        // resort, so independent grotto habitats remain spread through the sea.
        foreach (bool global in new[] { false, true })
        foreach (float scale in new[] { 1f, .75f, .50f })
        {
            int w = Math.Max(101, (int)(requested.Bounds.Width * scale)) | 1;
            int h = Math.Max(49, (int)(requested.Bounds.Height * scale)) | 1;
            foreach (int offset in GrottoDepthOffsets(global ? p.Depth : localRange))
            {
                int cy = requested.Bounds.Center.Y + offset;
                if (cy < minY || cy > maxY) continue;
                int cliff = p.CliffEdge(cy);
                // Dense foreign objects often fill the cliff rather than the
                // basin. Move seaward; never weaken the same admission envelope.
                int start = cliff + w / 6;
                int last = w / 2 + 40;
                for (int edge = start; edge >= last; edge -= 32)
                {
                    Rectangle b = new(p.X(edge) - w / 2, cy - h / 2, w, h);
                    Rectangle envelope = b; envelope.Inflate(16, 16);
                    candidates++;
                    if (!p.Bounds.Contains(envelope) || !admission.Allows(envelope)) { blocked++; continue; }
                    bool reef = edge < cliff - w / 2 - 16;
                    if (reef)
                    {
                        // A reef grotto is surrounded by water, with no connection
                        // to a nearby seabed or surface island. Its own stone shell
                        // and bubble airlock enclose the breathing space.
                        int nearShore = Math.Max(p.Edge(envelope.Left), p.Edge(envelope.Right - 1));
                        if (p.Floor(nearShore) <= envelope.Bottom + 8) continue;
                    }
                    else if (p.Floor(p.Edge(b.Center.X) + b.Width / 4) > b.Center.Y) continue;
                    if (p.Grottos.Any(g => { Rectangle padded = g.Bounds; padded.Inflate(32, 32); return padded.Intersects(b); })) continue;
                    CarveGrotto(p, b);
                    p.Grottos.Add(new(b));
                    GenVars.structures.AddProtectedStructure(envelope, 6);
                    return true;
                }
            }
        }
        return false;
    }

    private static IEnumerable<int> GrottoDepthOffsets(int range)
    {
        yield return 0;
        for (int dy = 32; dy <= range; dy += 32)
        {
            yield return -dy;
            yield return dy;
        }
    }
}
