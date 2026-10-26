// Original local compatibility guards, integrated into the public DingDong project.
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Compatibility.LegacyPack
{
	public class LocalWorldgenGuards : ModSystem
	{
		private static LocalWorldgenGuards _instance;
		internal static bool SanctuaryShiftInstalled { get; private set; }

		public override void Load()
		{
			_instance = this;
			SanctuaryShiftInstalled = false;

			if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
			{
				Type customTemple = calamity.Code.GetType("CalamityMod.World.CustomTemple");
				MethodInfo generateTemple = customTemple?.GetMethod("NewJungleTemple", BindingFlags.Public | BindingFlags.Static);
				if (generateTemple != null)
				{
					MonoModHooks.Add(generateTemple, ClampRemixTempleRockLayer);
					Mod.Logger.Info("Installed Calamity remix-world Jungle Temple bounds guard.");
				}
				else
				{
					Mod.Logger.Warn("Calamity remix-world Jungle Temple bounds guard was not installed because the expected method was not found.");
				}
			}

			if (ModLoader.TryGetMod("SOTS", out Mod sots))
			{
				Type helper = sots.Code.GetType("SOTS.WorldgenHelpers.SOTSWorldgenHelper");
				MethodInfo trueTileSolid = helper?.GetMethod("TrueTileSolid", BindingFlags.Public | BindingFlags.Static);
				if (trueTileSolid != null)
				{
					MonoModHooks.Add(trueTileSolid, GuardOutOfWorldTrueTileSolid);
					Mod.Logger.Info("Installed SOTS out-of-world TrueTileSolid bounds guard.");
				}
				else
				{
					Mod.Logger.Warn("SOTS TrueTileSolid bounds guard was not installed because the expected method was not found.");
				}

				MethodInfo sky = helper?.GetMethod("GenerateSkyArtifact", BindingFlags.Public | BindingFlags.Static);
				if (sky != null)
					MonoModHooks.Add(sky, DisableSkyForce);

				Type sanctuary = sots.Code.GetType("SOTS.WorldgenHelpers.SanctuaryWorldgenHelper");
				MethodInfo spawn = sanctuary?.GetMethod("DetermineSpawnLocation", BindingFlags.Public | BindingFlags.Static);
				if (spawn != null && ModLoader.HasMod("CalamityMod"))
				{
					MonoModHooks.Add(spawn, ShiftSanctuaryOffHellLab);
					SanctuaryShiftInstalled = true;
				}
			}
		}

		// SOTS intends out-of-world coordinates to count as solid, but its final
		// Bubble check dereferences Main.tile even after !InWorld evaluated true.
		// Short-circuit here before the array access, preserving the intended value.
		private static bool GuardOutOfWorldTrueTileSolid(TrueTileSolid orig, int i, int j, bool includeActuated)
		{
			if (!WorldGen.InWorld(i, j, 20))
				return true;

			return orig(i, j, includeActuated);
		}

		public override void Unload()
		{
			_instance = null;
			SanctuaryShiftInstalled = false;
		}

		// Calamity chooses its temple Y before entering its remix-world branch. In
		// remix worlds Main.rockLayer can be at or below the exclusive upper bound
		// (maxTilesY - 500), which makes UnifiedRandom.Next(min, max) throw. Keep
		// Calamity's complete temple generator and only lend it a valid search band.
		private static void ClampRemixTempleRockLayer(GenerateJungleTemple orig)
		{
			double originalRockLayer = Main.rockLayer;
			int upperExclusive = Main.maxTilesY - 500;

			if (!Main.remixWorld || originalRockLayer < upperExclusive)
			{
				orig();
				return;
			}

			double searchBand = Math.Max(64.0, Main.maxTilesY / 12.0);
			double safeRockLayer = Math.Max(200.0, upperExclusive - searchBand);
			if (safeRockLayer >= upperExclusive)
				safeRockLayer = upperExclusive - 1;

			_instance?.Mod.Logger.Warn(
				$"Clamped Calamity remix Jungle Temple search range from [{originalRockLayer:0}, {upperExclusive}) " +
				$"to [{safeRockLayer:0}, {upperExclusive}) while preserving the original temple pass.");

			try
			{
				Main.rockLayer = safeRockLayer;
				orig();
			}
			finally
			{
				Main.rockLayer = originalRockLayer;
			}
		}

		// SOTS sets force=true after 55 failed checks and pastes the planetarium on top of the Calamity main planetoid.
		private static bool DisableSkyForce(GenerateSkyArtifact orig, int x, int y, Mod mod, bool force)
		{
			return orig(x, y, mod, false);
		}

		// Default Sanctuary X is 7/8. Calamity hell lab is also on that side when the dungeon is on the left.
		private static int ShiftSanctuaryOffHellLab(DetermineSpawnLocation orig)
		{
			if (!ModLoader.HasMod("CalamityMod"))
				return orig();

			return Main.maxTilesX * 3 / 8;
		}

		private delegate bool GenerateSkyArtifact(int x, int y, Mod mod, bool force);
		private delegate int DetermineSpawnLocation();
		private delegate void GenerateJungleTemple();
		private delegate bool TrueTileSolid(int i, int j, bool includeActuated);
	}

	public class WorldgenOrderSystem : ModSystem
	{
		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
		{
			// Vanilla's Mount Caves pass can index beyond the 4200-wide tile map on
			// For The Worthy when this large modpack has already altered early terrain.
			// The pass is decorative, so omit it only for that exact size/seed profile.
			if ((WorldGen.getGoodWorldGen || Main.getGoodWorld) && Main.maxTilesX <= 4200)
			{
				int mountCaves = IndexOf(tasks, "Mount Caves");
				if (mountCaves >= 0)
				{
					tasks[mountCaves] = new PassLegacy("Mount Caves (small FTW compat)", (progress, configuration) =>
					{
						progress.Message = "Skipping unsafe decorative mountain caves on small For The Worthy worlds";
						Mod.Logger.Warn("Skipped vanilla Mount Caves pass for the known small For The Worthy bounds failure.");
					}, tasks[mountCaves].Weight);
				}

				// Fargo Souls assumes its candidate-pyramid list is non-empty. Some
				// small For The Worthy layouts legitimately provide no candidate and
				// DesignatePyramidSpot then indexes the empty array. This pass only
				// guarantees an extra pyramid; omitting it preserves the generated
				// world instead of aborting worldgen before later mod passes run.
				int guaranteePyramid = IndexOf(tasks, "GuaranteePyramidAgain");
				if (guaranteePyramid >= 0)
				{
					tasks[guaranteePyramid] = new PassLegacy("GuaranteePyramidAgain (small FTW compat)", (progress, configuration) =>
					{
						progress.Message = "Skipping unsafe extra-pyramid guarantee on small For The Worthy worlds";
						Mod.Logger.Warn("Skipped Fargo Souls GuaranteePyramidAgain pass because its empty-candidate path crashes small For The Worthy worldgen.");
					}, tasks[guaranteePyramid].Weight);
				}
			}

			// Labs need Sunken Sea walls, but must run before Thorium/SOTS fill StructureMap.
			MoveAfter(tasks, "Draedon Structures", "Sunken Sea");

			// Let SOTS claim empty sky first; Calamity planetoids then refuse occupied tiles.
			MoveBefore(tasks, "SOTS: Planetarium", "Planetoids");
			MoveAfter(tasks, "Planetoids", "SOTS: Planetarium");

			Mod.Logger.Info("Worldgen pass order after compat:");
			for (int i = 0; i < tasks.Count; i++)
			{
				string name = tasks[i].Name;
				if (name.Contains("SOTS", StringComparison.OrdinalIgnoreCase)
					|| name.Contains("Draedon", StringComparison.OrdinalIgnoreCase)
					|| name.Contains("Planetoid", StringComparison.OrdinalIgnoreCase)
					|| name.Contains("Sunken", StringComparison.OrdinalIgnoreCase)
					|| name.Contains("Abyss", StringComparison.OrdinalIgnoreCase)
					|| name.Contains("Sulphur", StringComparison.OrdinalIgnoreCase)
					|| name.Contains("Thorium", StringComparison.OrdinalIgnoreCase)
					|| name.Contains("Brimstone", StringComparison.OrdinalIgnoreCase)
					|| name.Contains("Sanctuary", StringComparison.OrdinalIgnoreCase))
				{
					Mod.Logger.Info($"  [{i}] {name}");
				}
			}
		}

		private static bool MoveAfter(List<GenPass> tasks, string moveName, string afterName)
		{
			int from = IndexOf(tasks, moveName);
			int after = IndexOf(tasks, afterName);
			if (from < 0 || after < 0 || from == after + 1)
				return false;

			GenPass pass = tasks[from];
			tasks.RemoveAt(from);
			if (from < after)
				after--;
			tasks.Insert(after + 1, pass);
			return true;
		}

		private static bool MoveBefore(List<GenPass> tasks, string moveName, string beforeName)
		{
			int from = IndexOf(tasks, moveName);
			int before = IndexOf(tasks, beforeName);
			if (from < 0 || before < 0 || from + 1 == before)
				return false;

			GenPass pass = tasks[from];
			tasks.RemoveAt(from);
			if (from < before)
				before--;
			tasks.Insert(before, pass);
			return true;
		}

		private static int IndexOf(List<GenPass> tasks, string name)
		{
			return tasks.FindIndex(pass => pass.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}
	}
}
