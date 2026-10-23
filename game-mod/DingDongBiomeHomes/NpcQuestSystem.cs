using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static partial class NpcQuestSystem
{
	private static readonly string[] SurveyTargetKeys =
	{
		"Center",
		"WestWing",
		"EastWing",
		"UpperFloor",
		"GroundFloor",
		"UpperWest",
		"UpperEast",
		"Entrance"
	};
	private static readonly string[] PatrolDirectionKeys =
	{
		"North",
		"NorthEast",
		"East",
		"SouthEast",
		"South",
		"SouthWest",
		"West",
		"NorthWest"
	};
}
