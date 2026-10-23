using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	internal static void ProbeLiquidSafety(Mod mod)
	{
		if (Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" || !Main.dedServ)
			throw new InvalidOperationException("Liquid controls require the isolated dedicated probe.");
		var residence = SettlementWorldState.Residences.Single(r => r.NpcKey == "Terraria/GoblinTinkerer");
		NpcAuthoredPresetManifest.TryGetCandidate(residence.NpcKey, out var preset);
		AuthoredCandidateDeploymentManifest.TryGet(residence.NpcKey, out var profile);
		if (!HasAmbientLavaExposure(mod, preset, profile, residence.Area.Location, out Point threatenedRoom))
			throw new InvalidOperationException("Known pre-settling Goblin ingress fixture was not rejected.");
		// Bounded negative/positive controls use the same production graph kernel.
		const byte open = Worldgen.LiquidIngressReachability.Open;
		const byte lava = Worldgen.LiquidIngressReachability.Lava;
		const byte target = Worldgen.LiquidIngressReachability.Room;
		byte[] grid = Enumerable.Repeat(open, 35).ToArray();
		grid[1 * 5 + 1] |= lava; grid[5 * 5 + 3] |= target;
		if (!Worldgen.LiquidIngressReachability.CanReachRoom(grid, 7, 5, out _)) throw new InvalidOperationException("LIQ-02 open route escaped rejection");
		for (int y = 0; y < 5; y++) grid[3 * 5 + y] = 0;
		if (Worldgen.LiquidIngressReachability.CanReachRoom(grid, 7, 5, out _)) throw new InvalidOperationException("LIQ-03 sealed barrier rejected");
		grid[3 * 5 + 2] = open;
		if (!Worldgen.LiquidIngressReachability.CanReachRoom(grid, 7, 5, out _)) throw new InvalidOperationException("LIQ-04 single-cell breach escaped rejection");
		grid = Enumerable.Repeat(open, 35).ToArray(); grid[1 * 5 + 4] |= lava; grid[5 * 5 + 1] |= target;
		if (Worldgen.LiquidIngressReachability.CanReachRoom(grid, 7, 5, out _)) throw new InvalidOperationException("LIQ-05 source below target exceeded head");
		if (!IsLavaFragileSceneTile(Terraria.ID.TileID.GolfGrass) || !IsLavaFragileSceneTile(Terraria.ID.TileID.Trees) || IsLavaFragileSceneTile(Terraria.ID.TileID.Stone))
			throw new InvalidOperationException("LIQ-06 fragile source classification");
		mod.Logger.Info($"DINGDONG_LIQUID_SAFETY_PROBE|verdict=PASS|checks=6|knownIngressRejected=True|threatenedSceneCell={threatenedRoom}|worldMutated=False|worldSaved=False");
	}
}
