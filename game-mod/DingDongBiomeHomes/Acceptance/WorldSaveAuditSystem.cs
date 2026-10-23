using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Acceptance;

// Opt-in diagnostic for a disposable world copy. Never runs in normal gameplay.
public sealed class WorldSaveAuditSystem : ModSystem
{
	private bool pending;
	public override void OnWorldLoad() => pending = Main.dedServ &&
		Environment.GetEnvironmentVariable("DINGDONG_SAVE_AUDIT_AUTO") == "1";
	public override void PostUpdateWorld()
	{
		if (!pending) return;
		Run();
	}
	internal void Run()
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_SAVE_AUDIT_AUTO") != "1")
			throw new InvalidOperationException("Save audit is disabled outside an opt-in dedicated process.");
		pending = false;
		string root = Environment.GetEnvironmentVariable("DINGDONG_SAVE_AUDIT_ROOT");
		string path = Path.GetFullPath(Main.ActiveWorldFileData.Path);
		if (string.IsNullOrWhiteSpace(root) || !path.StartsWith(
			Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
			StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("Save audit requires a disposable world under its explicit root.");
		try
		{
			Type tileIO = typeof(Mod).Assembly.GetType("Terraria.ModLoader.IO.TileIO", true);
			object tiles = tileIO.GetField("Tiles", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
			object walls = tileIO.GetField("Walls", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
			var result = new { width = Main.maxTilesX, height = Main.maxTilesY,
				tileCount = TileLoader.TileCount, wallCount = WallLoader.WallCount,
				tiles = Inspect(tiles, false), walls = Inspect(walls, true) };
			Console.WriteLine("DINGDONG_SAVE_AUDIT_SCAN|" + JsonSerializer.Serialize(result));
			tileIO.GetMethod("SaveBasics", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, null);
			Console.WriteLine("DINGDONG_SAVE_AUDIT_NATIVE|PASS|inMemorySaveBasics=True|worldCopyOnly=True");
			Environment.Exit(0);
		}
		catch (Exception error)
		{
			Console.WriteLine("DINGDONG_SAVE_AUDIT_NATIVE|FAIL|" + error);
			Environment.Exit(1);
		}
	}

	private static object Inspect(object implementation, bool wall)
	{
		Type type = implementation.GetType().BaseType;
		Array entries = (Array)type.GetField("entries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(implementation);
		object lookup = type.GetField("unloadedEntryLookup", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(implementation);
		Type readerType = typeof(Mod).Assembly.GetType("Terraria.ModLoader.PosData`1+OrderedSparseLookupReader", true).MakeGenericType(typeof(ushort));
		object reader = Activator.CreateInstance(readerType, new[] { lookup, (object)true });
		MethodInfo get = readerType.GetMethod("Get", new[] { typeof(int), typeof(int) });
		long nonVanilla = 0, invalid = 0, placeholders = 0;
		List<object> examples = new();
		for (int x = 0; x < Main.maxTilesX; x++)
			for (int y = 0; y < Main.maxTilesY; y++)
			{
				Tile cell = Main.tile[x, y];
				int id = wall ? cell.WallType : cell.HasTile ? cell.TileType : 0;
				if (id < (wall ? WallID.Count : TileID.Count)) continue;
				nonVanilla++;
				int savedId = id;
				if (entries != null && id < entries.Length && entries.GetValue(id) == null)
				{
					placeholders++;
					savedId = (ushort)get.Invoke(reader, new object[] { x, y });
				}
				if (entries == null || savedId >= entries.Length || entries.GetValue(savedId) == null)
				{
					invalid++;
					if (examples.Count < 16) examples.Add(new { x, y, id, savedId });
				}
			}
		return new { entries = entries?.Length ?? 0, nonVanilla, placeholders, invalid, examples };
	}
}

// Empty dedicated servers pause world updates; this triggers the same audit
// explicitly after loading, without adding a player or changing world state.
public sealed class WorldSaveAuditCommand : ModCommand
{
	public override string Command => "ddsaveaudit";
	public override CommandType Type => CommandType.Console;
	public override string Usage => "ddsaveaudit";
	public override string Description => "Audits native saving on an explicitly opted-in disposable world.";
	public override void Action(CommandCaller caller, string input, string[] args) =>
		ModContent.GetInstance<WorldSaveAuditSystem>().Run();
}
