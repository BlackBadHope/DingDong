using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static void CaptureDiagnosticPools(Mod mod, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools)
	{
		if (Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1") return;
		string directory = Path.Combine(Main.SavePath, "DingDongDiagnostics", "ResidencePools");
		Directory.CreateDirectory(directory);
		string path = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + ".json");
		var data = pools.ToDictionary(p => p.Key, p => p.Value.Select(c => new {
			asset = new { c.Site.AssetArea.X, c.Site.AssetArea.Y, c.Site.AssetArea.Width, c.Site.AssetArea.Height },
			protect = new { c.Site.ProtectedArea.X, c.Site.ProtectedArea.Y, c.Site.ProtectedArea.Width, c.Site.ProtectedArea.Height }
		}).ToArray());
		File.WriteAllText(path, JsonSerializer.Serialize(new {
			seed = Main.ActiveWorldFileData.Seed, width = Main.maxTilesX, height = Main.maxTilesY,
			purpose = "candidate geometry only; not a placement or compatibility witness", pools = data
		}));
		mod.Logger.Info("DINGDONG_RESIDENCE_POOL_SNAPSHOT|path=" + path);
	}
}
