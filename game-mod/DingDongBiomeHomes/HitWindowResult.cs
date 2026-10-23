using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DingDongBiomeHomes;
public readonly record struct HitWindowResult(bool AcceptedIndependentEvent, int IndependentHitsInSequence, bool Triggered);
