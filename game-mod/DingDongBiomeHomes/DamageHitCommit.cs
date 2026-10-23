using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal readonly record struct DamageHitCommit(DamageSourceKind SourceKind, int TargetId, int SourceType, int ProjectileIdentity, ulong AttackInstanceId, uint HitEventId, DamageStrikePayload Payload)
{
	public bool Matches(DamageHitIntent intent) => SourceKind == intent.SourceKind && TargetId == intent.TargetId && SourceType == intent.SourceType && ProjectileIdentity == intent.ProjectileIdentity && AttackInstanceId == intent.AttackInstanceId && HitEventId == intent.HitEventId;
}
