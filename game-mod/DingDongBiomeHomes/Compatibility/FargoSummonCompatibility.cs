using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

internal static class FargoSummonCompatibility
{
	internal static void Install(List<ILHook> hooks)
	{
		Type type = R19CompatibilitySystem.Provider("Fargowiltas", "3.3.6.7", "Fargowiltas.Projectiles.SpawnProj");
		if (type == null) return;
		if (type.Module.ModuleVersionId != new Guid("c9c57245-e2b1-4ad9-a0a3-d34e25bccf1c"))
			throw new InvalidOperationException("Fargo summon provider identity changed.");
		hooks.Add(new ILHook(R19CompatibilitySystem.Method(type, "OnKill"), il => {
			var cursor = new ILCursor(il);
			var start = cursor.Next;
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate<Func<ModProjectile, bool>>(HasValidRequest);
			cursor.Emit(OpCodes.Brtrue, start);
			cursor.Emit(OpCodes.Ret);
		}));
	}

	// SpawnProj is a control message, not ammunition. A zero-filled duplicate
	// must never reach NPC.NewNPC(0): that creates an active NPC without globals,
	// which makes downstream projectile hit hooks throw for the entire frame.
	internal static bool HasValidRequest(ModProjectile summon)
	{
		Projectile projectile = summon?.Projectile;
		if (projectile == null || projectile.owner < 0 || projectile.owner >= Main.player.Length) return false;
		float mode = projectile.ai[1], type = projectile.ai[0];
		if (mode == 2f || mode == 3f) return true; // Native combined pre-HM/all-boss summons.
		return float.IsFinite(mode) && float.IsFinite(type) && type == MathF.Truncate(type) && type > 0 && type < NPCLoader.NPCCount;
	}
}
