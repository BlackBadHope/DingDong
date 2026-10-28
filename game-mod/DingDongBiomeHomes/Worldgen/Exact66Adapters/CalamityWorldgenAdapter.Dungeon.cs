using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static void PatchDungeonWorkBudget(ILContext il)
	{
		ILCursor primaryHall = new(il);
		if (!primaryHall.TryGotoNext(MoveType.After,
			i => IsMainField(i, "maxTilesX"), i => i.MatchLdcI4(60),
			i => i.OpCode == OpCodes.Div, i => i.OpCode == OpCodes.Conv_R8))
			throw new InvalidOperationException("Vanilla dungeon primary-hall budget IL pattern is missing.");
		primaryHall.EmitDelegate<Func<double, double>>(BoundPrimaryHallBase);
		EmitDungeonStage(primaryHall, "primary-hall");

		ILCursor pitTraps = new(il);
		if (!pitTraps.TryGotoNext(MoveType.After,
			i => IsMainField(i, "maxTilesX"), i => i.MatchLdcI4(2),
			i => i.OpCode == OpCodes.Mul, i => i.MatchStloc(8)))
			throw new InvalidOperationException("Vanilla dungeon pit-trap budget IL pattern is missing.");
		pitTraps.Index--;
		pitTraps.EmitDelegate<Func<int, int>>(BoundPitTrapAttempts);
		EmitDungeonStage(pitTraps, "pit-traps");

		ILCursor wallFeatures = new(il);
		if (!wallFeatures.TryGotoNext(MoveType.After,
			i => IsMainField(i, "maxTilesX"), i => i.MatchLdcI4(100),
			i => i.OpCode == OpCodes.Div, i => i.MatchStloc(13)))
			throw new InvalidOperationException("Vanilla dungeon wall-feature budget IL pattern is missing.");
		wallFeatures.Index--;
		wallFeatures.EmitDelegate<Func<int, int>>(BoundWallFeatureTarget);
		EmitDungeonStage(wallFeatures, "wall-features");

		ILCursor platforms = new(il);
		if (!platforms.TryGotoNext(MoveType.After,
			i => IsMainField(i, "maxTilesX"), i => i.MatchLdcI4(20),
			i => i.OpCode == OpCodes.Div))
			throw new InvalidOperationException("Vanilla dungeon platform-target IL pattern is missing.");
		platforms.EmitDelegate<Func<int, int>>(BoundPlatformTarget);
		EmitDungeonStage(platforms, "platforms");
		PatchDungeonStairBudget(il);
	}

	private static double BoundPrimaryHallBase(double original) =>
		ContinentalAdapterMath.Active()
			? Math.Min(original, WorkBudget().PrimaryHallBase) : original;

	private static int BoundPitTrapAttempts(int original) =>
		ContinentalAdapterMath.Active()
			? Math.Min(original, WorkBudget().PitTrapAttempts) : original;

	private static int BoundWallFeatureTarget(int original) =>
		ContinentalAdapterMath.Active()
			? Math.Min(original, WorkBudget().WallFeatureTarget) : original;

	private static int BoundPlatformTarget(int original) =>
		ContinentalAdapterMath.Active()
			? Math.Min(original, WorkBudget().PlatformTarget) : original;

	private static ContinentalDungeonWorkBudget WorkBudget() =>
		!Compiler.Runtime.WorldCompilerActivation.IsActive
			? ContinentalDungeonWorkBudget.CreateNative(Main.maxTilesX, Main.maxTilesY)
			:
		ContinentalDungeonWorkBudget.Create(Main.maxTilesX,
			ContinentalWorldDimensions.VanillaLargeWidth,
			ContinentalWorldDimensions.VanillaLargeHeight);

	private static bool IsMainField(Instruction instruction, string name) =>
		instruction.OpCode == OpCodes.Ldsfld && instruction.Operand is FieldReference field &&
		field.DeclaringType.FullName == "Terraria.Main" && field.Name == name;

	private static void MakeDungeon(On_WorldGen.orig_MakeDungeon orig, int x, int y)
	{
		if (!ContinentalAdapterMath.Active())
		{
			orig(x, y);
			return;
		}
		// Presets 28.10: tiles of the current preset (x10: 28000*7200, unchanged).
		ContinentalDungeonBufferPlan plan = ContinentalDungeonBufferPlan.Create(
			(long)Main.maxTilesX * Main.maxTilesY,
			(long)ContinentalWorldDimensions.VanillaLargeWidth *
			ContinentalWorldDimensions.VanillaLargeHeight);
		EnsureDungeonBuffers(plan);
		BeginDungeonStageProfile();
		try { orig(x, y); }
		finally { FinishDungeonStageProfile(); }
		CalamityWorldgenReceipt.MarkDungeonCapacity(plan, WorkBudget());
	}

	private static void EnsureDungeonBuffers(ContinentalDungeonBufferPlan plan)
	{
		ContinentalArrayCapacity.Grow(ref GenVars.dRoomX, plan.RoomCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.dRoomY, plan.RoomCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.dRoomSize, plan.RoomCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.dRoomTreasure, plan.RoomCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.dRoomL, plan.RoomCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.dRoomR, plan.RoomCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.dRoomT, plan.RoomCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.dRoomB, plan.RoomCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.DDoorX, plan.JunctionCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.DDoorY, plan.JunctionCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.DDoorPos, plan.JunctionCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.dungeonPlatformX, plan.JunctionCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.dungeonPlatformY, plan.JunctionCapacity);
	}
}
