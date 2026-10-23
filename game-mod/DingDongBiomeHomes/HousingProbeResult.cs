using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public readonly record struct HousingProbeResult(int ValidRooms, int CandidatePoints, string FirstFailure);
