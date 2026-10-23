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
internal sealed class ResidentEscortState
{
	internal string NpcKey { get; init; } = string.Empty;
	internal ResidentEscortStage Stage { get; set; }
	internal string OwnerId { get; set; } = string.Empty;
	internal int OwnerPlayerIndex { get; set; } = -1;
	internal Point16 FlagPosition { get; set; } = new(-1, -1);
	internal Point OriginHome { get; set; } = new(-1, -1);
	internal Point SpawnAnchor { get; set; } = new(-1, -1);
	internal Point DestinationHome { get; set; } = new(-1, -1);
	internal string FailureReason { get; set; } = string.Empty;
	internal long Revision { get; set; }

	internal ResidentEscortState Clone() => new()
	{
		NpcKey = NpcKey,
		Stage = Stage,
		OwnerId = OwnerId,
		OwnerPlayerIndex = OwnerPlayerIndex,
		FlagPosition = FlagPosition,
		OriginHome = OriginHome,
		SpawnAnchor = SpawnAnchor,
		DestinationHome = DestinationHome,
		FailureReason = FailureReason,
		Revision = Revision
	};
}
