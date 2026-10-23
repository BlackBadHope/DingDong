using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal readonly record struct UniversalQuestTemplate(ResidentQuestKind Kind, string TargetKey, string TargetName, int ItemType, int Goal, int RadiusTiles, int BaseRewardCopper);
