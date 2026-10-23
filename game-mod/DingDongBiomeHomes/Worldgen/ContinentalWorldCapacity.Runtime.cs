using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldCapacity
{
	private static void RefreshRuntimeCompanions(bool resetSections = false)
	{
		SynchronizeLegacyWorldGenBounds();
		// WorldData is also sent during normal play. Capacity checks must not
		// erase the client's received sections or the server's sent sections.
		Main.sectionManager = ContinentalSectionState.EnsureWorldSections(
			resetSections ? null : Main.sectionManager, Main.maxSectionsX, Main.maxSectionsY);
		ResizeRemoteSections(resetSections);
		ResizeMapTargets();
	}

	private static void ResizeRemoteSections(bool resetSections)
	{
		int width = Main.maxTilesX / Main.sectionWidth + 1;
		int height = Main.maxTilesY / Main.sectionHeight + 1;
		if (Netplay.Clients == null) return;
		foreach (RemoteClient client in Netplay.Clients)
			if (client != null)
				client.TileSections = ContinentalSectionState.EnsureRemoteSections(
					resetSections ? null : client.TileSections, width, height);
	}

	internal static void ResetRemoteSectionState()
	{
		lock (allocationGate)
		{
			if (Netplay.Clients == null) return;
			foreach (RemoteClient client in Netplay.Clients)
				if (client?.TileSections != null)
					Array.Clear(client.TileSections);
		}
	}

	private static void ResizeMapTargets()
	{
		if (Main.dedServ) return;
		int width = Main.maxTilesX / Main.textureMaxWidth + 2;
		int height = Main.maxTilesY / Main.textureMaxHeight + 2;
		if (width <= Main.mapTargetX && height <= Main.mapTargetY) return;
		Main.mapTargetX = Math.Max(5, width);
		Main.mapTargetY = Math.Max(3, height);
		Main.instance.mapTarget = new RenderTarget2D[Main.mapTargetX, Main.mapTargetY];
		Main.initMap = new bool[Main.mapTargetX, Main.mapTargetY];
		Main.mapWasContentLost = new bool[Main.mapTargetX, Main.mapTargetY];
	}
}
