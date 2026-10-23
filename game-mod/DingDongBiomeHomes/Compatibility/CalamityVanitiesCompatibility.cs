using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

internal static class CalamityVanitiesCompatibility
{
	internal static void Install(List<ILHook> hooks)
	{
		if (R19CompatibilitySystem.Provider("CalamityMod", "2.2.2", "CalamityMod.ModCalls") == null) return;
		foreach (string name in new[] { "Meldosaurus", "MeldWyrmBody", "MeldWyrmHead", "MeldWyrmTail" })
		{
			Type type = R19CompatibilitySystem.Provider("CalValEX", "11.4.2", "CalValEX.AprilFools.Meldosaurus." + name);
			if (type != null) hooks.Add(new ILHook(R19CompatibilitySystem.Method(type, "SetDefaults"), PatchDefaults));
		}
	}

	private static object CallWithNpc(Mod provider, object[] args, ModNPC owner)
	{
		if (args.Length == 2 && args[0] is string name)
		{
			if (name == "SetDamageReductionSpecific" && args[1] is float dr)
				return provider.Call("SetDRSpecific", owner.NPC, dr);
			if (name == "SetDefenseDamageNPC" && args[1] is bool enabled)
				return provider.Call(name, owner.NPC, enabled);
		}
		return provider.Call(args);
	}

	private static void PatchDefaults(ILContext il)
	{
		var cursor = new ILCursor(il);
		int replaced = 0;
		while (cursor.TryGotoNext(MoveType.Before, i => i.Operand is MethodReference m &&
			m.DeclaringType.FullName == "Terraria.ModLoader.Mod" && m.Name == "Call"))
		{
			cursor.Remove();
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate<Func<Mod, object[], ModNPC, object>>(CallWithNpc);
			replaced++;
		}
		if (replaced < 1) throw new InvalidOperationException("CalValEX NPC call contract changed.");
	}
}
