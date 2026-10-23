using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

// Called only by the dedicated, explicitly named diagnostic-copy dispatcher.
internal static class ResidenceWriteSafetyProbe
{
	internal static void Run(Mod mod)
	{
		StructureMap originalMap = GenVars.structures;
		Tile tile = Main.tile[500, 100];
		AuthoredTileSnapshot original = AuthoredTileSnapshot.Read(tile);
		Rectangle area = new(495, 95, 11, 11);
		int detected = 0, repaired = 0;
		try
		{
			GenVars.structures = new StructureMap();
			GenVars.structures.AddProtectedStructure(area, 0);
			Action[] corrupt = {
				() => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref tile.Get<TileTypeData>(), 1))[0] ^= 1,
				() => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref tile.Get<WallTypeData>(), 1))[0] ^= 1,
				() => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref tile.Get<LiquidData>(), 1))[0] ^= 1,
				() => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref tile.Get<TileWallBrightnessInvisibilityData>(), 1))[0] ^= 1,
				() => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref tile.Get<TileWallWireStateData>(), 1))[0] ^= 1
			};
			foreach (Action change in corrupt)
			{
				var guard = ForeignStructureIntegrityGuard.Capture(mod);
				change();
				try { guard.Validate(mod); }
				catch (InvalidOperationException ex) when (ex.Message.StartsWith("Foreign protected structure changed at ", StringComparison.Ordinal)) { detected++; }
				finally { original.Restore(tile); }
				var repairGuard = ForeignStructureIntegrityGuard.Capture(mod);
				change();
				repairGuard.RepairAndValidate(mod);
				if (AuthoredTileSnapshot.Read(tile) == original) repaired++;
			}
			if (detected != 5 || repaired != 5)
				throw new InvalidOperationException("Integrity guard failed a plane mutation or repair control.");
			var queryGuard = ForeignStructureIntegrityGuard.Capture(mod);
			AuthoredSettlementBuilder.ProbeHousing(area);
			AuthoredSettlementBuilder.ProbeHousingShells(area);
			AuthoredSettlementBuilder.TryFindHousingRoom(area, Terraria.ID.NPCID.Guide, out _, out _);
			queryGuard.Validate(mod);
			mod.Logger.Info("DINGDONG_WRITE_SAFETY_PROBE|verdict=PASS|planeMutationControls=5/5|planeRepairControls=5/5|housingQueriesUnchanged=True|tileRestored=True|synthetic=True");
		}
		finally { original.Restore(tile); GenVars.structures = originalMap; }
	}
}
