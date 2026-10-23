using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using DingDongBiomeHomes.Items.SynergyArmor;

namespace DingDongBiomeHomes.Items.MobRelics;
internal static partial class MobRelicMechanics
{
	internal const int TriggerCount = 20;
	internal const int InvocationCount = 20;
	internal const int TwistCount = 16;
	internal const int LinkThreshold = 2;
	internal const int EchoThreshold = 3;
	internal const int WardThreshold = 4;
	internal const int ConvergenceThreshold = 5;
	internal const int ProviderHarmonyThreshold = 3;
}
