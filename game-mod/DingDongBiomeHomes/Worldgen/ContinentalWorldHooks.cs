using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldHooks
{
	private static bool installed;

	internal static void Install()
	{
		if (installed)
			return;
		ContinentalWorldCapacity.Initialize();
		InstallWorldCreationMenu();
		On_WorldGen.clearWorld += ClearWorld;
		On_WorldGen.GenerateWorld += GenerateWorld;
		On_WorldGen.playWorldCallBack += PlayWorldCallback;
		On_WorldGen.UpdateMapTile += UpdateMapTile;
		Terraria.IO.On_WorldFile.LoadHeader += LoadHeader;
		On_MessageBuffer.GetData += GetData;
		installed = true;
	}

	internal static void Uninstall()
	{
		if (!installed)
			return;
		On_WorldGen.clearWorld -= ClearWorld;
		On_WorldGen.GenerateWorld -= GenerateWorld;
		On_WorldGen.playWorldCallBack -= PlayWorldCallback;
		On_WorldGen.UpdateMapTile -= UpdateMapTile;
		Terraria.IO.On_WorldFile.LoadHeader -= LoadHeader;
		On_MessageBuffer.GetData -= GetData;
		UninstallWorldCreationMenu();
		ContinentalWorldCapacity.Reset();
		installed = false;
	}
}
