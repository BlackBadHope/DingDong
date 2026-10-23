using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

internal static class FargoMoneyTroughCompatibility
{
	private const string ProviderName = "Fargowiltas";
	private const string ProviderType = "Fargowiltas.Projectiles.FargoGlobalProjectile";
	private const string ConfigType = "Fargowiltas.Common.Configs.FargoServerConfig";
	private const string ExpectedMvid = "c9c57245-e2b1-4ad9-a0a3-d34e25bccf1c";
	private const string ExpectedOriginalIl = "E8685701CFDCB383030070E6AC478738570D39EEF17EDA736115BDBECF7C155D";

	// StartupData/Mods/Fargowiltas.tmod 3.3.6.7, DLL SHA256:
	// BD76654AF5DAEFAFBEC0730D69B0DD9BAD5CF846B4C2AFC0D39105EAFE5A5995.
	// Killing one following pig must not expire a replacement or another copy.
	// Keep the provider's guards/enumeration/finally; suppress only its zero store.
	internal static void Install(List<ILHook> hooks)
	{
		Type type = R19CompatibilitySystem.Provider(ProviderName, "3.3.6.7", ProviderType);
		if (type == null) return;
		MethodInfo method = CheckedMethod(type);
		hooks.Add(new ILHook(method, PatchOnKill));
	}

	private static MethodInfo CheckedMethod(Type type)
	{
		if (type.Module.ModuleVersionId != Guid.Parse(ExpectedMvid))
			throw new InvalidOperationException("Fargowiltas 3.3.6.7 Money Trough contract changed: module MVID.");
		MethodInfo method = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
			null, new[] { typeof(Projectile), typeof(int) }, null);
		if (method == null || method.ReturnType != typeof(void) ||
			Hash(method.GetMethodBody()?.GetILAsByteArray() ?? Array.Empty<byte>()) != ExpectedOriginalIl)
			throw new InvalidOperationException("Fargowiltas 3.3.6.7 Money Trough contract changed: original OnKill IL.");
		return method;
	}

	private static void PatchOnKill(ILContext il)
	{
		Instruction[] stores = il.Body.Instructions.Where(i => i.OpCode == OpCodes.Stfld &&
			i.Operand is FieldReference field && field.DeclaringType.FullName == "Terraria.Projectile" &&
			field.Name == nameof(Projectile.timeLeft) && field.FieldType.FullName == "System.Int32").ToArray();
		Contract(stores.Length == 1, "timeLeft store count");
		ValidateZeroStore(il, stores[0]);
		// Before stfld the stack is [enumeratedProjectile, 0]. Consume both without
		// writing either object; leave all original branch targets and EH intact.
		stores[0].OpCode = OpCodes.Pop;
		stores[0].Operand = null;
		il.Body.GetILProcessor().InsertAfter(stores[0], Instruction.Create(OpCodes.Pop));
	}

	private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

	// MonoMod represents branches with ILLabel and may expand short forms and
	// locals. Validate the actual stack producer and loop edges, not a serialized
	// body hash. The provider's original bytes are independently pinned above.
	private static void ValidateZeroStore(ILContext il, Instruction store)
	{
		var instructions = il.Body.Instructions;
		Instruction zero = store.Previous, current = zero?.Previous, load = current?.Previous;
		Contract(zero != null && zero.MatchLdcI4(0) &&
			Call(current, "System.Collections.Generic.IEnumerator`1<Terraria.Projectile>", "get_Current") &&
			load != null && load.MatchLdloc(out _), "enumerator.Current / zero / store");
		load.MatchLdloc(out int local);
		Contract(local >= 0 && local < il.Body.Variables.Count &&
			il.Body.Variables[local].VariableType.FullName == "System.Collections.Generic.IEnumerator`1<Terraria.Projectile>", "enumerator local type");
		Instruction[] assignments = instructions.Where(i => i.MatchStloc(out int index) && index == local).ToArray();
		Contract(assignments.Length == 1, "enumerator assignment count");
		Instruction assignment = assignments[0], getEnumerator = assignment.Previous, where = getEnumerator?.Previous;
		Contract(Call(getEnumerator, "System.Collections.Generic.IEnumerable`1<Terraria.Projectile>", "GetEnumerator") &&
			Call(where, "System.Linq.Enumerable", "Where") && where.Operand is GenericInstanceMethod generic &&
			generic.GenericArguments.Count == 1 && generic.GenericArguments[0].FullName == "Terraria.Projectile", "projectile Where / GetEnumerator");
		Instruction nextLoad = store.Next, moveNext = nextLoad?.Next, repeat = moveNext?.Next;
		Contract(nextLoad != null && nextLoad.MatchLdloc(out int nextLocal) && nextLocal == local &&
			Call(moveNext, "System.Collections.IEnumerator", "MoveNext") &&
			Branch(repeat, Code.Brtrue, Code.Brtrue_S) && Target(repeat) == load &&
			Branch(assignment.Next, Code.Br, Code.Br_S) && Target(assignment.Next) == nextLoad, "enumerator loop edges");
		Instruction[] typeGuards = instructions.Where(i => i.MatchLdcI4(ProjectileID.FlyingPiggyBank) &&
			Field(i.Previous, Code.Ldfld, "Terraria.Projectile", "type") &&
			Field(i.Previous.Previous, Code.Ldfld, ProviderType + "/<>c__DisplayClass9_0", "projectile") &&
			Branch(i.Next, Code.Bne_Un, Code.Bne_Un_S)).ToArray();
		Contract(typeGuards.Length == 1, "FlyingPiggyBank guard");
		Instruction exit = Target(typeGuards[0].Next);
		Contract(exit?.OpCode == OpCodes.Ret && instructions.Count(i =>
			Field(i, Code.Ldsfld, "Fargowiltas.Common.Configs.FargoServerConfig", "Instance") &&
			Field(i.Next, Code.Ldfld, "Fargowiltas.Common.Configs.FargoServerConfig", "StalkerMoneyTrough") &&
			Branch(i.Next.Next, Code.Brfalse, Code.Brfalse_S) && Target(i.Next.Next) == exit) == 1, "StalkerMoneyTrough guard");
		Contract(instructions.Count(i => Field(i, Code.Ldsfld, "Terraria.Main", "projectile") &&
			instructions.IndexOf(i) < instructions.IndexOf(where)) == 1 && instructions.Count(i =>
			i.OpCode == OpCodes.Ldftn && i.Operand is MethodReference predicate &&
			predicate.DeclaringType.FullName == ProviderType + "/<>c__DisplayClass9_0" &&
			predicate.Name == "<OnKill>b__0" && instructions.IndexOf(i) < instructions.IndexOf(where)) == 1,
			"Main.projectile and provider predicate");
		ExceptionHandler[] handlers = il.Body.ExceptionHandlers.Where(h => h.HandlerType == ExceptionHandlerType.Finally &&
			h.TryStart == assignment.Next && instructions.IndexOf(h.TryStart) <= instructions.IndexOf(store) &&
			instructions.IndexOf(store) < instructions.IndexOf(h.TryEnd)).ToArray();
		Contract(handlers.Length == 1, "enumerator finally");
		ExceptionHandler handler = handlers[0];
		Contract(instructions.Where(i => instructions.IndexOf(i) >= instructions.IndexOf(handler.HandlerStart) &&
			instructions.IndexOf(i) < instructions.IndexOf(handler.HandlerEnd)).Count(i =>
			Call(i, "System.IDisposable", "Dispose") && i.Previous != null &&
			i.Previous.MatchLdloc(out int disposedLocal) && disposedLocal == local) == 1, "enumerator disposal");
	}

	private static bool Call(Instruction instruction, string type, string name) => instruction != null &&
		(instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
		instruction.Operand is MethodReference method && method.DeclaringType.FullName == type && method.Name == name;

	private static bool Field(Instruction instruction, Code code, string type, string name) => instruction != null &&
		instruction.OpCode.Code == code && instruction.Operand is FieldReference field && field.DeclaringType.FullName == type && field.Name == name;

	private static bool Branch(Instruction instruction, Code code, Code shortCode) => instruction != null &&
		(instruction.OpCode.Code == code || instruction.OpCode.Code == shortCode);

	private static Instruction Target(Instruction instruction) => instruction?.Operand switch
	{
		Instruction target => target,
		ILLabel label => label.Target,
		_ => null
	};

	private static void Contract(bool valid, string condition)
	{
		if (!valid) throw new InvalidOperationException("Fargowiltas 3.3.6.7 Money Trough contract changed: " + condition + ".");
	}

	// Explicit diagnostic only. The caller's existing disposable combat probe
	// owns output/launch/exit. This never runs from Install or ordinary gameplay.
	// Invokes the real hooked Fargo OnKill via reflection, with manual inactive
	// dying objects. It does not exercise Projectile.Kill, AI, rendering or input.
	internal static int RunRuntimeChecks()
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_COMBAT_PROBE") != "1")
			throw new InvalidOperationException("Money Trough checks require the explicit disposable combat probe.");
		Mod mod = ModLoader.GetMod(ProviderName);
		if (mod.Version != Version.Parse("3.3.6.7"))
			throw new InvalidOperationException("Money Trough checks require Fargowiltas 3.3.6.7.");
		Type type = mod.Code.GetType(ProviderType, throwOnError: true);
		MethodInfo onKill = CheckedMethod(type);
		GlobalProjectile global = mod.GetContent<GlobalProjectile>().Single(g => g.GetType() == type);
		Type configType = mod.Code.GetType(ConfigType, throwOnError: true);
		object config = configType.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
		FieldInfo stalker = configType.GetField("StalkerMoneyTrough", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		bool previousStalker = (bool)stalker.GetValue(config);
		Projectile[] previousProjectiles = Main.projectile;
		try
		{
			stalker.SetValue(config, true);
			Main.projectile = Enumerable.Range(0, Main.maxProjectiles + 1).Select(i => new Projectile { whoAmI = i }).ToArray();
			Projectile oldPig = Pig(0, 0, 0, false);
			Projectile newPig = Pig(1, 0, 600, true);
			Projectile foreignPig = Pig(2, 1, 700, true);
			Projectile singlePig = Pig(3, 2, 0, false);
			onKill.Invoke(global, new object[] { oldPig, 300 });
			Require(!oldPig.active && oldPig.timeLeft == 0 && newPig.active && newPig.timeLeft == 600,
				"Old pig OnKill changed its replacement.");
			Require(foreignPig.active && foreignPig.timeLeft == 700, "Old pig OnKill changed a different owner's pig.");
			onKill.Invoke(global, new object[] { singlePig, 300 });
			Require(!singlePig.active && singlePig.timeLeft == 0 && newPig.active && newPig.timeLeft == 600 &&
				foreignPig.active && foreignPig.timeLeft == 700, "Single pig OnKill resurrected or changed another pig.");
			return 3;
		}
		finally
		{
			Main.projectile = previousProjectiles;
			stalker.SetValue(config, previousStalker);
		}
	}

	private static Projectile Pig(int slot, int owner, int timeLeft, bool active)
	{
		Projectile projectile = Main.projectile[slot];
		projectile.type = ProjectileID.FlyingPiggyBank;
		projectile.owner = owner;
		projectile.timeLeft = timeLeft;
		projectile.active = active;
		return projectile;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition) throw new InvalidOperationException("Money Trough OnKill check failed: " + message);
	}
}
