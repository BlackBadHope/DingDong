using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DingDongBiomeHomes.Worldgen;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Diagnostics;

public sealed class ServerMemoryRuntimeProbe : ModSystem
{
	public override void PostSetupRecipes()
	{
		string output = Environment.GetEnvironmentVariable("DINGDONG_SERVER_MEMORY_PROBE_OUTPUT");
		if (!Main.dedServ || string.IsNullOrEmpty(output)) return;
		var checks = new List<string>(); string error = null;
		var originalWorld = Main.ActiveWorldFileData;
		var originalError = WorldFile.LastThrownLoadException;
		bool failed = WorldGen.loadFailed, success = WorldGen.loadSuccess, backup = WorldGen.worldBackup;
		string fixture = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)), "server-memory-fixture");
		Directory.CreateDirectory(fixture);
		string path = Path.Combine(fixture, "synthetic.wld");
		int calls = 0; bool memory = true;
		void Require(bool value, string name)
		{
			if (!value) throw new InvalidOperationException(name);
			checks.Add(name);
		}
		void FakeLoad(On_WorldFile.orig_LoadWorld orig, bool cloud)
		{
			calls++;
			WorldGen.loadFailed = true; WorldGen.loadSuccess = false;
			WorldFile.LastThrownLoadException = memory
				? new ContinentalMemoryAdmissionException("synthetic admission denial")
				: new InvalidDataException("synthetic corrupt-file failure");
		}
		try
		{
			Main.ActiveWorldFileData = new WorldFileData(path, false) { Name = "Synthetic admission fixture" };
			On_WorldFile.LoadWorld += FakeLoad;
			File.WriteAllText(path, "primary-marker"); File.WriteAllText(path + ".bak", "backup-marker");
			WorldGen.serverLoadWorldCallBack();
			Require(calls == 1, "Memory denial stops before native retry");
			Require(File.ReadAllText(path) == "primary-marker", "Memory denial preserves primary bytes");
			Require(File.ReadAllText(path + ".bak") == "backup-marker", "Memory denial preserves backup bytes");
			memory = false; calls = 0;
			WorldGen.serverLoadWorldCallBack();
			Require(calls == 4, "Ordinary corrupt-file failure retains native retry and recovery");
			Require(File.ReadAllText(path) == "backup-marker" && !File.Exists(path + ".bak"),
				"Native backup recovery remains active only in the synthetic corrupt-file case");
		}
		catch (Exception ex) { error = ex.ToString(); }
		finally
		{
			On_WorldFile.LoadWorld -= FakeLoad;
			Main.ActiveWorldFileData = originalWorld; WorldFile.LastThrownLoadException = originalError;
			WorldGen.loadFailed = failed; WorldGen.loadSuccess = success; WorldGen.worldBackup = backup;
		}
		File.WriteAllText(output, JsonSerializer.Serialize(new { status = error == null ? "PASS" : "FAIL",
			checks, error, realWorldLoaded = false, userSavesTouched = false }));
		Environment.Exit(error == null ? 0 : 1);
	}
}
