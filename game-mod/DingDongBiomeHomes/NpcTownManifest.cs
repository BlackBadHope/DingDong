using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
public static partial class NpcTownManifest
{
	private static readonly Dictionary<string, NpcTownIdentity> Entries = BuildEntries();
	private static readonly ReadOnlyCollection<NpcTownIdentity> AuthoredEntries = new(new List<NpcTownIdentity>(Entries.Values));
	public static int AuthoredCount => Entries.Count;
	public static IReadOnlyList<NpcTownIdentity> All => AuthoredEntries;
}
