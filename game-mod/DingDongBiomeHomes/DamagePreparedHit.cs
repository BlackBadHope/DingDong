using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal sealed record DamagePreparedHit(DamageHitIntent Intent, DamageHitLedger TentativeLedger, float ServerMultiplier, long ExpiresAtTick, int ExpectedDamageType, DamagePayloadEnvelope PayloadEnvelope = default, DamageStrikePayload? Payload = null);
