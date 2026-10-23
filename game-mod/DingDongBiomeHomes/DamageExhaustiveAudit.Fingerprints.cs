using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Dedicated-only exhaustive compatibility gate. Every loaded damage-capable
/// item, projectile and debuff source is evaluated against every loaded
/// hostile NPC target through the production rule engine. The audit never
/// mutates world, player, item or NPC persistence state.
/// </summary>
internal static partial class DamageExhaustiveAudit
{
	private static ulong Fingerprint(DamageSourceClassification classification, int contentId)
	{
		ulong result = Mix(DigestSeed, unchecked((ulong)(uint)contentId));
		result = Mix(result, (ulong)classification.Coverage);
		result = Mix(result, classification.TrueMultistrike ? 1UL : 0UL);
		result = MixString(result, classification.Profile.Key);
		result = FingerprintAxis(result, classification.Profile.Delivery);
		result = FingerprintAxis(result, classification.Profile.Element);
		result = FingerprintAxis(result, classification.Profile.Behavior);
		return FingerprintAxis(result, classification.Profile.Acquisition);
	}

	private static ulong Fingerprint(DamageTargetClassification classification, int contentId)
	{
		ulong result = Mix(DigestSeed, unchecked((ulong)(uint)contentId));
		result = Mix(result, (ulong)classification.Coverage);
		result = Mix(result, (ulong)classification.SegmentRole);
		result = Mix(result, (ulong)classification.PhaseRole);
		result = Mix(result, unchecked((ulong)(uint)classification.EncounterRootId));
		result = MixString(result, classification.Profile.Key);
		foreach (string morphology in classification.Profile.Morphology)
			result = MixString(result, morphology);
		return result;
	}

	private static ulong FingerprintAxis(ulong digest, WeightedDamageAxis axis)
	{
		digest = MixString(digest, axis.AxisName);
		foreach (WeightedDamageTag tag in axis.Tags)
		{
			digest = MixString(digest, tag.Id);
			digest = Mix(digest, unchecked((ulong)BitConverter.DoubleToInt64Bits(tag.Weight)));
		}

		return digest;
	}

	private static ulong MixString(ulong digest, string value)
	{
		digest = Mix(digest, (ulong)value.Length);
		foreach (char character in value)
			digest = Mix(digest, character);
		return digest;
	}

	private static ulong Mix(ulong digest, ulong value)
	{
		unchecked
		{
			digest ^= value + 0x9E3779B97F4A7C15UL + (digest << 6) + (digest >> 2);
			return digest * DigestPrime;
		}
	}

	private static bool IsFinitePositive(double value) => double.IsFinite(value) && value > 0d;
	private static bool Nearly(double left, double right) => Math.Abs(left - right) <= Epsilon;
	private static string ExceptionName(Exception exception) => exception.GetType().Name;
}
