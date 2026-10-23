using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
    private static void InstallSanctuaryPlacement(Mod provider, List<IDisposable> hooks)
    {
        InstallSanctuaryPlacementCaller(provider, hooks, "PrepareUnderworldArea", typeof(void), 0, 1,
            typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int));
        InstallSanctuaryPlacementCaller(provider, hooks, "SetRect", typeof(Rectangle), 2, 0);
        InstallSanctuaryPlacementCaller(provider, hooks, "GenerateSanctuary", typeof(void), 22, 9);
        InstallSanctuaryPlacementCaller(provider, hooks, "GeneratePillar", typeof(void), 1, 0, typeof(int), typeof(int));
        InstallSanctuaryPlacementCaller(provider, hooks, "GenerateRectangle", typeof(void), 1, 0,
            typeof(int), typeof(int), typeof(int), typeof(int), typeof(int));
        InstallSanctuaryPlacementCaller(provider, hooks, "TryErodingBlocks", typeof(void), 2, 0, typeof(int), typeof(int));
    }

    private static void InstallSanctuaryPlacementCaller(Mod provider, List<IDisposable> hooks,
        string name, Type result, int ceilingCalls, int underworldCalls, params Type[] parameters)
    {
        var method = Exact66ProviderGate.Method(provider, SanctuaryType, name, true, result, parameters);
        hooks.Add(new ILHook(method, il => PatchSanctuaryPlacementCalls(il, ceilingCalls, underworldCalls)));
    }

    private static void PatchSanctuaryPlacementCalls(ILContext il, int expectedCeiling, int expectedUnderworld)
    {
        Instruction[] ceiling = il.Body.Instructions.Where(i => SanctuaryHeightCall(i, "get_Ceiling")).ToArray();
        Instruction[] underworld = il.Body.Instructions.Where(i => SanctuaryHeightCall(i, "get_UnderworldHeight")).ToArray();
        if (ceiling.Length != expectedCeiling || underworld.Length != expectedUnderworld)
            throw new InvalidOperationException($"SOTS Sanctuary placement callsites changed: {il.Method.Name} " +
                $"C={ceiling.Length}/{expectedCeiling},UH={underworld.Length}/{expectedUnderworld}.");
        // Keep the native call and its result for standard/unmarked worlds.
        // The consumer-side delegate remains even if that tiny getter is
        // inlined to its original Main.UnderworldLayer expression by the JIT.
        foreach (Instruction call in ceiling)
        {
            var cursor = new ILCursor(il); cursor.Goto(call, MoveType.After);
            cursor.EmitDelegate<Func<int, int>>(NativeSanctuaryPlacementSystem.AdjustCeiling);
        }
        foreach (Instruction call in underworld)
        {
            var cursor = new ILCursor(il); cursor.Goto(call, MoveType.After);
            cursor.EmitDelegate<Func<int, int>>(NativeSanctuaryPlacementSystem.AdjustUnderworldHeight);
        }
    }

    private static bool SanctuaryHeightCall(Instruction instruction, string name) =>
        (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
        instruction.Operand is MethodReference method &&
        method.DeclaringType?.FullName == SanctuaryType && method.Name == name;
}
