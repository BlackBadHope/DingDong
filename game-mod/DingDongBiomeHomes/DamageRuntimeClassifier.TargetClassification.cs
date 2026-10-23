using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Data-driven runtime classifier. Optional-mod content is inspected only via
/// public Terraria/tModLoader metadata, so no compatibility assembly is a hard
/// dependency.
/// </summary>
public static partial class DamageRuntimeClassifier
{
	public static DamageTargetClassification Classify(NPC npc)
	{
		ArgumentNullException.ThrowIfNull(npc);
		DamageSegmentRole segment = InferSegmentRole(npc);
		string identity = StableNpcIdentity(npc);
		DamageTargetProfile profile;
		DamageCoverageKind coverage;
		if (IsSlime(npc, identity))
		{
			profile = DamageProfileRegistry.VanillaSlime;
			coverage = DamageCoverageKind.Explicit;
		}
		else if (ContainsAny(identity, "machine", "mechan", "robot", "golem", "probe", "drone", "turret", "cyborg", "construct"))
		{
			profile = MechanicalTarget;
			coverage = npc.ModNPC != null ? DamageCoverageKind.Adapter : DamageCoverageKind.Fallback;
		}
		else if (ContainsAny(identity, "ghost", "wraith", "spirit", "phantom", "spectr", "polter", "soul"))
		{
			profile = SpectralTarget;
			coverage = npc.ModNPC != null ? DamageCoverageKind.Adapter : DamageCoverageKind.Fallback;
		}
		else if (ContainsAny(identity, "skeleton", "zombie", "undead", "mummy", "vampire", "ghoul", "bone"))
		{
			profile = UndeadTarget;
			coverage = npc.ModNPC != null ? DamageCoverageKind.Adapter : DamageCoverageKind.Fallback;
		}
		else if (ContainsAny(identity, "plant", "flower", "vine", "spore", "mushroom", "fung", "cactus", "tree"))
		{
			profile = PlantTarget;
			coverage = npc.ModNPC != null ? DamageCoverageKind.Adapter : DamageCoverageKind.Fallback;
		}
		else if (ContainsAny(identity, "fish", "shark", "jelly", "crab", "squid", "ocean", "aqua", "eel", "whale"))
		{
			profile = AquaticTarget;
			coverage = npc.ModNPC != null ? DamageCoverageKind.Adapter : DamageCoverageKind.Fallback;
		}
		else if (npc.defense >= Math.Max(20, npc.lifeMax / 35) || ContainsAny(identity, "armor", "knight", "shell", "paladin", "guardian"))
		{
			profile = ArmoredTarget;
			coverage = npc.ModNPC != null ? DamageCoverageKind.Adapter : DamageCoverageKind.Fallback;
		}
		else
		{
			profile = OrganicTarget;
			coverage = npc.ModNPC != null ? DamageCoverageKind.Adapter : DamageCoverageKind.Fallback;
		}

		int encounterRoot = npc.realLife >= 0 && npc.realLife < Main.maxNPCs ? npc.realLife : npc.whoAmI;
		DamagePhaseRole phase = InferPhaseRole(npc);
		return new DamageTargetClassification(profile, coverage, segment, phase, encounterRoot);
	}
}
