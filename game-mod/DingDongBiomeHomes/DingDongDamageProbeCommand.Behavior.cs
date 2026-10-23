using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Non-mutating dedicated-server contract probe for the deterministic damage
/// core, persistent affinity metadata and multiplayer authority protocol.
/// </summary>
public sealed partial class DingDongDamageProbeCommand
{
	public override void Action(CommandCaller caller, string input, string[] args)
	{
		string worldPath = Main.ActiveWorldFileData?.Path ?? string.Empty;
		string worldFile = Path.GetFileNameWithoutExtension(worldPath);
		if (!Main.dedServ || !string.Equals(Environment.GetEnvironmentVariable(ProbeEnvironmentVariable), "1", StringComparison.Ordinal) || args.Length != 1 || !string.Equals(args[0], "slime", StringComparison.OrdinalIgnoreCase) || !worldFile.StartsWith(ProbeWorldPrefix, StringComparison.Ordinal))
		{
			caller.Reply($"Usage: {Usage}. Requires dedicated server, {ProbeEnvironmentVariable}=1, and a world named {ProbeWorldPrefix}*.", Color.OrangeRed);
			return;
		}

		string summary = RunProbe(Mod, worldFile);
		caller.Reply(summary, summary.Contains("|verdict=PASS|", StringComparison.Ordinal) ? Color.LightGreen : Color.OrangeRed);
	}

	private static DamageEvaluationTrace EvaluateOne(DamageRuleEngine engine, DamageSourceProfile source, DamageTargetProfile target, ulong lineage) => Evaluate(engine, source, target, new DamageHitLedger(), 1, lineage, 1);
	private static DamageEvaluationTrace Evaluate(DamageRuleEngine engine, DamageSourceProfile source, DamageTargetProfile target, DamageHitLedger ledger, long tick, ulong lineage, uint eventId, bool trueMultistrike = false, int attackerId = 7) => engine.EvaluateAcceptedHit(source, target, new DamageHitObservation(tick, attackerId, 11, lineage, eventId, trueMultistrike), ledger);
	private static DamageHitIntent RoundTripIntent(DamageHitIntent intent)
	{
		using MemoryStream stream = new();
		using BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		DamageNetworkCodec.WriteIntent(writer, intent);
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		if ((DamagePacketKind)reader.ReadByte() != DamagePacketKind.HitIntent)
			return default;
		return DamageNetworkCodec.ReadIntent(reader);
	}

	private static DamageHitReceipt RoundTripReceipt(DamageHitReceipt receipt)
	{
		using MemoryStream stream = new();
		using BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		DamageNetworkCodec.WriteReceipt(writer, receipt);
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		if ((DamagePacketKind)reader.ReadByte() != DamagePacketKind.HitReceipt)
			return default;
		return DamageNetworkCodec.ReadReceipt(reader);
	}

	private static DamageHitCommit RoundTripCommit(DamageHitCommit commit)
	{
		using MemoryStream stream = new();
		using BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		DamageNetworkCodec.WriteCommit(writer, commit);
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		if ((DamagePacketKind)reader.ReadByte() != DamagePacketKind.HitCommit)
			return default;
		return DamageNetworkCodec.ReadCommit(reader);
	}

	private static string RoundTripTag(string tag)
	{
		using MemoryStream stream = new();
		using BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		DamageTagWire.Write(writer, tag);
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		return DamageTagWire.Read(reader);
	}

	private static bool RejectsUnknownProtocolVersion() => !DamageNetworkCodec.IsSupportedVersion(99);
	private static bool SameMultiplier(DamageEvaluationTrace left, DamageEvaluationTrace right) => Nearly(left.FinalMultiplier, right.FinalMultiplier) && left.ConditionalRule == right.ConditionalRule;
}
