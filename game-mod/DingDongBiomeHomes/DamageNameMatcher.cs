using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

// Shared lexical fallback for items, projectiles, NPCs and buffs. FullName's
// mod prefix is identity metadata, not evidence of the content's element.
internal static class DamageNameMatcher
{
	private const int CacheLimit = 4096;
	private sealed class TokenGeneration
	{
		internal readonly ConcurrentDictionary<string, string[]> Entries = new(StringComparer.Ordinal);
		// Only changed under CacheGate; never query ConcurrentDictionary.Count
		// on a combat/draw thread (Count acquires all dictionary stripes).
		internal int Count;
	}
	private static readonly object CacheGate = new();
	private static volatile TokenGeneration current = new();
	private static volatile TokenGeneration previous = new();
	// Word families are semantic vocabulary, never item IDs or free substring
	// matches. Closed compounds remain explicit so "ice" cannot match spice,
	// "star" cannot match starter, and "bee" cannot match beetle.
	private static readonly Dictionary<string, string> Families = new(StringComparer.Ordinal)
	{
		["holiness"] = "holy", ["hallowed"] = "hallow", ["radiance"] = "radiant", ["divinity"] = "divine",
		["flaming"] = "flame", ["flamelash"] = "flame", ["flamethrower"] = "flame", ["infernal"] = "inferno",
		["hellfire"] = "fire", ["hellstone"] = "hell", ["hellwing"] = "hell",
		["hellforge"] = "hell", ["hellbound"] = "hell", ["helltower"] = "hell", ["hellbat"] = "hell", ["hellhound"] = "hell",
		["fireblossom"] = "fire", ["firework"] = "fire", ["fireplace"] = "fire", ["campfire"] = "fire", ["flameburst"] = "flame",
		["sunfury"] = "sun", ["sunlight"] = "sun", ["sunplate"] = "sun", ["sunshine"] = "sun",
		["frostburn"] = "frost", ["frostbite"] = "frost", ["frostbrand"] = "frost", ["frostspark"] = "frost", ["frosting"] = "frost", ["frozen"] = "ice",
		["icicle"] = "ice", ["icy"] = "ice", ["snowball"] = "snow", ["snowflake"] = "snow", ["snowman"] = "snow", ["snowmen"] = "snow",
		["cryogen"] = "cryo", ["cryogenic"] = "cryo", ["cryonic"] = "cryo",
		["electrical"] = "electric", ["electrified"] = "electric", ["electrifying"] = "electric", ["stormbow"] = "storm", ["thunderbolt"] = "thunder",
		["waterfall"] = "water", ["aquatic"] = "aqua", ["oceanic"] = "ocean", ["seashell"] = "sea", ["seaweed"] = "sea",
		["seahorse"] = "sea", ["seagull"] = "sea", ["seafood"] = "sea", ["tsunami"] = "water",
		["earthen"] = "earth", ["rocky"] = "rock", ["sandstone"] = "sand", ["sandy"] = "sand", ["terrestrial"] = "terra", ["terragrim"] = "terra",
		["ebonstone"] = "stone", ["pearlstone"] = "stone", ["crimstone"] = "stone", ["mudstone"] = "stone", ["titanstone"] = "stone", ["coralstone"] = "stone",
		["tombstone"] = "stone", ["headstone"] = "stone", ["gravestone"] = "stone", ["ebonsand"] = "sand", ["pearlsand"] = "sand", ["crimsand"] = "sand",
		["sandgun"] = "sand", ["sandcastle"] = "sand", ["terrarium"] = "terra", ["terrarian"] = "terra", ["terraspark"] = "terra",
		["poisoned"] = "poison", ["poisonous"] = "poison", ["poisoning"] = "poison", ["venomous"] = "venom", ["acidic"] = "acid",
		["nightmare"] = "night", ["nightcrawler"] = "night", ["midnight"] = "night", ["darkness"] = "dark", ["darkmage"] = "dark",
		["corruption"] = "corrupt", ["corrupted"] = "corrupt", ["corruptor"] = "corrupt", ["bloody"] = "blood", ["bloodroot"] = "blood", ["bloodbath"] = "blood", ["plaguebringer"] = "plague",
		["galactic"] = "galaxy", ["interstellar"] = "stellar", ["starfury"] = "star", ["starlight"] = "star", ["starfall"] = "star", ["stardust"] = "star",
		["starry"] = "star", ["starlit"] = "star", ["starpower"] = "star",
		["abyssal"] = "abyss", ["nullified"] = "null", ["nullification"] = "null",
		["gelatin"] = "gel", ["gelatinous"] = "gel", ["slimy"] = "slime", ["slimeling"] = "slime", ["slimer"] = "slime", ["crimslime"] = "slime",
		["natural"] = "nature", ["leaves"] = "leaf", ["leafy"] = "leaf", ["beenade"] = "bee",
		["mechanic"] = "mechan", ["mechanical"] = "mechan", ["mechanism"] = "mechan", ["mechanized"] = "mechan",
		["spectre"] = "spectr", ["specter"] = "spectr", ["spectral"] = "spectr", ["poltergeist"] = "polter",
		["fungus"] = "fung", ["fungi"] = "fung", ["fungal"] = "fung", ["armored"] = "armor", ["armoured"] = "armor", ["shelly"] = "shell",
		["explosive"] = "explosion", ["exploding"] = "explosion", ["explode"] = "explosion",
		["throwing"] = "throw", ["thrown"] = "throw", ["thrower"] = "throw"
	};
	private static readonly Dictionary<string, string[]> Compounds = new(StringComparer.Ordinal)
	{
		["cinderlock"] = new[] { "cinder" }, ["candlewick"] = new[] { "candle" },
		["hearthflame"] = new[] { "hearth", "flame" },
		["sunbeam"] = new[] { "sun", "beam" }, ["moonbeam"] = new[] { "beam" },
		["shadowbeam"] = new[] { "shadow", "beam" },
		["shadowflame"] = new[] { "shadow", "flame" }, ["sandstorm"] = new[] { "sand", "storm" },
		["waterleaf"] = new[] { "water", "leaf" }, ["cryocore"] = new[] { "cryo", "core" },
		["sandshark"] = new[] { "sand", "shark" }, ["rockfish"] = new[] { "rock", "fish" },
		["slimefish"] = new[] { "slime", "fish" }, ["hellfish"] = new[] { "hell", "fish" }, ["bumblebee"] = new[] { "bee" },
		["jellyfish"] = new[] { "jelly", "fish" }, ["goldfish"] = new[] { "fish" },
		["catfish"] = new[] { "fish" }, ["dogfish"] = new[] { "fish" }, ["swordfish"] = new[] { "fish" },
		["starfish"] = new[] { "fish" }, ["shellfish"] = new[] { "shell", "fish" },
		["crayfish"] = new[] { "fish" }, ["crawfish"] = new[] { "fish" }, ["anglerfish"] = new[] { "fish" },
		["blowfish"] = new[] { "fish" }, ["lanternfish"] = new[] { "fish" }, ["flyingfish"] = new[] { "fish" },
		["bonefish"] = new[] { "bone", "fish" }
	};

	internal static bool ContainsAny(string value, params string[] words)
	{
		if (string.IsNullOrEmpty(value)) return false;
		if (!current.Entries.TryGetValue(value, out string[] tokens) &&
			!previous.Entries.TryGetValue(value, out tokens))
		{
			tokens = ExpandTokens(Tokenize(value));
			if (value.Length <= 1024) Remember(value, tokens);
		}
		foreach (string token in tokens)
			foreach (string word in words)
				if (string.Equals(token, word, StringComparison.OrdinalIgnoreCase)) return true;
		return false;
	}

	private static void Remember(string value, string[] tokens)
	{
		lock (CacheGate)
		{
			TokenGeneration generation = current;
			if (generation.Entries.ContainsKey(value)) return;
			if (generation.Count >= CacheLimit)
			{
				previous = generation;
				current = generation = new TokenGeneration();
			}
			if (generation.Entries.TryAdd(value, tokens)) generation.Count++;
		}
	}

	internal static void Reset()
	{
		lock (CacheGate)
		{
			previous = new TokenGeneration();
			current = new TokenGeneration();
		}
	}

	// Materialize exactly the former Matches alternatives once on a cache miss.
	// The hot comparison must not allocate a closure or a plural substring for
	// every token/word pair. Families/compounds deliberately expand one level.
	private static string[] ExpandTokens(string[] tokens)
	{
		var alternatives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string token in tokens)
		{
			AddAlternatives(token, alternatives);
			if (token.EndsWith("s", StringComparison.Ordinal)) AddAlternatives(token[..^1], alternatives);
		}
		string[] result = new string[alternatives.Count];
		alternatives.CopyTo(result);
		return result;
	}

	private static void AddAlternatives(string token, HashSet<string> alternatives)
	{
		alternatives.Add(token);
		if (Families.TryGetValue(token, out string family)) alternatives.Add(family);
		if (Compounds.TryGetValue(token, out string[] parts))
			foreach (string part in parts) alternatives.Add(part);
	}

	private static string[] Tokenize(string value)
	{
		List<string> tokens = new();
		int chunk = 0;
		while (chunk < value.Length)
		{
			while (chunk < value.Length && char.IsWhiteSpace(value[chunk])) chunk++;
			int end = chunk;
			while (end < value.Length && !char.IsWhiteSpace(value[end])) end++;
			int content = chunk;
			for (int i = chunk; i < end; i++)
				if (value[i] is '/' or ':' or '\\') content = i + 1;
			int start = content;
			for (int i = content; i <= end; i++)
			{
				bool separator = i == end || !char.IsLetterOrDigit(value[i]);
				bool boundary = !separator && i > start &&
					(char.IsDigit(value[i]) != char.IsDigit(value[i - 1]) ||
					 char.IsUpper(value[i]) && (char.IsLower(value[i - 1]) ||
						char.IsUpper(value[i - 1]) && i + 1 < end && char.IsLower(value[i + 1])));
				if (!separator && !boundary) continue;
				if (i > start)
				{
					string token = value[start..i].ToLowerInvariant();
					tokens.Add(token);
					// Terraria IDs sometimes join lowercase connectors to the prior
					// word: BlizzardinaBottle, BandofStarpower, NightforAlchemy.
					if (boundary && char.IsUpper(value[i]))
						foreach (string connector in new[] { "inthe", "ina", "for", "of", "in" })
							if (token.Length > connector.Length && token.EndsWith(connector, StringComparison.Ordinal))
							{
								tokens.Add(token[..^connector.Length]);
								break;
							}
				}
				start = separator ? i + 1 : i;
			}
			chunk = end;
		}
		return tokens.ToArray();
	}
}
