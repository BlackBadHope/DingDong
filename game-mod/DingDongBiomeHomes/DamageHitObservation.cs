using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DingDongBiomeHomes;
public readonly record struct DamageHitObservation(long ServerTick, int AttackerId, int TargetId, ulong AttackInstanceId, uint HitEventId, bool TrueMultistrike);
