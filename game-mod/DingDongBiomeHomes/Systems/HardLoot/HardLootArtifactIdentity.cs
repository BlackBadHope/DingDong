namespace DingDongBiomeHomes.Systems.HardLoot;

internal readonly record struct HardLootArtifactIdentity(
	string Name,
	string Version,
	string Mvid,
	bool ClientOnly = false);
