using System;
using System.Diagnostics;
using Terraria.IO;
using Terraria.WorldBuilding;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class NativeTerrariaWorldgenPass : GenPass
{
	private readonly GenPass original;
	private readonly WorldgenStageJournal journal;
	private readonly Action<string> log;

	internal NativeTerrariaWorldgenPass(GenPass original, WorldgenStageJournal journal,
		Action<string> log) : base(original.Name, original.Weight)
	{
		this.original = original;
		this.journal = journal;
		this.log = log;
		if (!original.Enabled) Disable();
	}

	protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
	{
		log($"DINGDONG_NATIVE_PASS|state=START|name={Name}");
		long start = Stopwatch.GetTimestamp();
		string previousPass = NativeCoastPlacement.CurrentPass;
		NativeCoastPlacement.CurrentPass = Name;
		try
		{
			NativeCoastPlacement.BeforePass(Name);
			journal.Run(Name, () => original.Apply(progress, configuration));
		}
		finally { NativeCoastPlacement.CurrentPass = previousPass; }
		NativeCoastPlacement.AfterPass(Name);
		NativeSanctuaryNativeAdmission.AfterPass(Name);
		NativeReservationProbe.AfterPass(Name);
		if (Name == "Underworld") NativeUnderworldSystem.Generated();
		NativeTerrariaWorldgenReceipt.Record(Name);
		using Process process = Process.GetCurrentProcess();
		log($"DINGDONG_NATIVE_PASS|state=END|name={Name}" +
			$"|ms={Stopwatch.GetElapsedTime(start).TotalMilliseconds:F0}" +
			$"|workingSetMiB={process.WorkingSet64 / 1048576}" +
			$"|privateMiB={process.PrivateMemorySize64 / 1048576}");
	}
}
