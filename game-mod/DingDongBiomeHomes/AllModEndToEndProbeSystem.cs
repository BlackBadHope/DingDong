using System;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Fail-closed disposable-server acceptance for the exact enabled mod set.
/// The explicit environment gate and world prefix keep normal worlds out.
/// </summary>
public sealed partial class AllModEndToEndProbeSystem : ModSystem
{
	private const string EnabledEnvironment = "DINGDONG_ALLMOD_E2E_AUTO";
	private const string PhaseEnvironment = "DINGDONG_ALLMOD_E2E_PHASE";
	private const string ExpectedModsEnvironment = "DINGDONG_ALLMOD_E2E_EXPECTED_MODS";
	private const string ExpectedServerModsEnvironment = "DINGDONG_ALLMOD_E2E_EXPECTED_SERVER_MODS";
	private const string WorldPrefix = "DingDongAllModE2E-";
	private static SetupSnapshot setup;
	private bool ranWorldPhase;

	public override void PostSetupContent()
	{
		if (!Enabled())
			return;
		string phase = Phase();
		if (string.Equals(phase, "verify", StringComparison.Ordinal))
		{
			setup = AuditLoadedMods(registerCalls: false, runDamage: false);
			return;
		}
		if (!string.Equals(phase, "prepare", StringComparison.Ordinal))
		{
			setup = SetupSnapshot.Failure($"phase:{OneLine(phase)}");
			Mod.Logger.Error(setup.Marker);
			return;
		}
		setup = AuditLoadedMods(registerCalls: true, runDamage: true);
	}

	public override void PostWorldLoad() => RunWorldPhase();
	public override void PostUpdateWorld() => RunWorldPhase();
	public override void OnWorldUnload() => ranWorldPhase = false;

	public override void Unload()
	{
		setup = default;
		ranWorldPhase = false;
	}

	internal string RunFromConsole(string requestedPhase)
	{
		string activePhase = Phase();
		if (!Enabled() || !string.Equals(requestedPhase, activePhase, StringComparison.OrdinalIgnoreCase) || activePhase is not ("prepare" or "verify"))
		{
			string rejected = $"DINGDONG_ALLMOD_E2E_COMMAND|verdict=FAIL|requestedPhase={OneLine(requestedPhase)}|activePhase={OneLine(activePhase)}|dedicated={Main.dedServ}|enabled={Enabled()}|reason=guard-rejected";
			Mod.Logger.Error(rejected);
			return rejected;
		}

		bool alreadyRan = ranWorldPhase;
		RunWorldPhase();
		string marker = $"DINGDONG_ALLMOD_E2E_COMMAND|verdict=PASS|phase={OneLine(activePhase)}|worldPhaseInvoked={!alreadyRan}|alreadyRan={alreadyRan}";
		Mod.Logger.Info(marker);
		return marker;
	}
}
