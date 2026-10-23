using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerVanillaPassRegistry
{
	private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
	{
		"Terrain", "Reset", "Dunes", "Ocean Sand", "Sand Patches", "Tunnels",
		"Mount Caves", "Dirt Wall Backgrounds", "Rocks In Dirt", "Dirt In Rocks",
		"Clay", "Small Holes", "Dirt Layer Caves", "Rock Layer Caves",
		"Surface Caves", "Wavy Caves", "Generate Ice Biome", "Grass",
		"Jungle", "Mud Caves To Grass", "Full Desert", "Floating Islands",
		"Mushroom Patches", "Marble", "Granite", "Dirt To Mud", "Silt",
		"Shinies", "Webs", "Underworld", "Corruption", "Lakes", "Dungeon",
		"Slush", "Mountain Caves", "Beaches", "Gems", "Gravitating Sand",
		"Create Ocean Caves", "Shimmer", "Clean Up Dirt", "Pyramids",
		"Dirt Rock Wall Runner", "Living Trees", "Wood Tree Walls", "Altars",
		"Wet Jungle", "Jungle Temple", "Hives", "Jungle Chests",
		"Settle Liquids", "Remove Water From Sand", "Oasis", "Shell Piles",
		"Smooth World", "Waterfalls", "Ice", "Wall Variety", "Life Crystals",
		"Statues", "Buried Chests", "Surface Chests", "Jungle Chests Placement",
		"Water Chests", "Spider Caves", "Gem Caves", "Moss", "Temple",
		"Cave Walls", "Jungle Trees", "Floating Island Houses", "Quick Cleanup",
		"Pots", "Hellforge", "Spreading Grass", "Surface Ore and Stone",
		"Place Fallen Log", "Traps", "Piles", "Spawn Point", "Grass Wall",
		"Guide", "Sunflowers", "Planting Trees", "Herbs", "Dye Plants",
		"Webs And Honey", "Weeds", "Glowing Mushrooms and Jungle Plants",
		"Jungle Plants", "Vines", "Flowers", "Mushrooms", "Gems In Ice Biome",
		"Random Gems", "Moss Grass", "Muds Walls In Jungle", "Larva",
		"Settle Liquids Again", "Cactus, Palm Trees, & Coral", "Tile Cleanup",
		"Lihzahrd Altars", "Micro Biomes", "Water Plants", "Stalac",
		"Remove Broken Traps", "Final Cleanup"
	};

	internal static int Count => Names.Count;
	internal static bool Contains(string name) => Names.Contains(name);
}
