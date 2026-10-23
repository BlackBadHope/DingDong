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
internal enum ResidentEscortStage : byte
{
	Anchored,
	Befriended,
	Invited,
	Following,
	Waiting,
	AwaitingHome,
	Arrived
}
