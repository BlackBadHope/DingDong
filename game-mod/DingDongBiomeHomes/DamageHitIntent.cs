using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal readonly record struct DamageHitIntent(DamageSourceKind SourceKind, int TargetId, int SourceType, int ProjectileIdentity, ulong AttackInstanceId, uint HitEventId, long ClientTick, float PredictedMultiplier);
