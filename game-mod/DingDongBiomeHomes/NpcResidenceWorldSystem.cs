using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class NpcResidenceWorldSystem : ModSystem
{
	private const int SaveVersion = 3;
	internal const int SettlementRadiusTiles = 1000;
	private const int SpawnArrivalRadiusTiles = 18;
	private const int MaximumEscortStringLength = 256;
	private const int MinimumEscortStateEnvelopeBytes = 43;
	private static readonly Dictionary<string, ResidentEscortState> States = new(StringComparer.Ordinal);
	private static long worldRevision;

	internal static bool IsOriginReleased(string key) => States.TryGetValue(key, out var state) && (state.OriginReleased || state.Stage == ResidentEscortStage.Arrived);

	internal static bool WithinSettlementRadius(Point16 destination, Point spawnAnchor)
	{
		long dx = destination.X - spawnAnchor.X;
		long dy = destination.Y - spawnAnchor.Y;
		return dx * dx + dy * dy <= (long)SettlementRadiusTiles * SettlementRadiusTiles;
	}

	internal static bool SettlementContractPass() =>
		SettlementRadiusTiles == 1000 &&
		SpawnArrivalRadiusTiles == 18 &&
		WithinSettlementRadius(new Point16(1000, 0), Point.Zero) &&
		!WithinSettlementRadius(new Point16(1001, 0), Point.Zero) &&
		ResidentEscortStage.AwaitingHome != ResidentEscortStage.Arrived;
}
