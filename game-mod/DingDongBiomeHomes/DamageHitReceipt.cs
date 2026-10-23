using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal readonly record struct DamageHitReceipt(bool Accepted, int TargetId, ulong AttackInstanceId, float ServerMultiplier, DamageReceiptReason Reason);
