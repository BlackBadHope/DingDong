namespace DingDongBiomeHomes.Worldgen;

internal readonly record struct ContinentalDungeonExpansionResult(
	int Rooms,
	int Portals,
	int ProtectedAreas,
	int TargetRooms,
	int Attempts,
	double AreaDeficit);
