using System;
using System.Linq;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerProgressionObligations
{
	internal static void Objects(WorldCompilerProgressionReceipt receipt)
	{
		Require("golem-altar", 1); Require("temple-locked-door", 1);
		Require("queen-bee-larva", 6); Require("evil-altar", 8);
		Require("evil-orb", 8); Require("hellforge", 12);
		void Require(string name, int expected)
		{
			int count = receipt.Objects.Count(value => value.Name == name);
			if (count != expected)
				throw new InvalidOperationException($"Missing progression obligation: {name} {count}/{expected}.");
		}
	}

	internal static void Chests(WorldCompilerProgressionReceipt receipt)
	{
		Require("dungeon", 8); Require("jungle", 4); Require("sky", 4);
		Require("underworld", 5); Require("pyramid", 3); Require("temple", 1);
		void Require(string name, int variety)
		{
			int count = receipt.Chests.Where(value => value.Kind == name)
				.Select(value => value.Primary).Distinct().Count();
			if (count < variety)
				throw new InvalidOperationException($"Missing progression chest family: {name} {count}/{variety} primary items.");
		}
	}
}
