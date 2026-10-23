using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Data-driven runtime classifier. Optional-mod content is inspected only via
/// public Terraria/tModLoader metadata, so no compatibility assembly is a hard
/// dependency.
/// </summary>
public static partial class DamageRuntimeClassifier
{
	private sealed class CoverageCounter
	{
		public int Explicit { get; private set; }
		public int Inherited { get; private set; }
		public int Adapters { get; private set; }
		public int Fallbacks { get; private set; }
		public int Unknown { get; private set; }

		public void Add(DamageCoverageKind kind)
		{
			switch (kind)
			{
				case DamageCoverageKind.Explicit:
					Explicit++;
					break;
				case DamageCoverageKind.Inherited:
					Inherited++;
					break;
				case DamageCoverageKind.Adapter:
					Adapters++;
					break;
				case DamageCoverageKind.Fallback:
					Fallbacks++;
					break;
				default:
					Unknown++;
					break;
			}
		}
	}
}
